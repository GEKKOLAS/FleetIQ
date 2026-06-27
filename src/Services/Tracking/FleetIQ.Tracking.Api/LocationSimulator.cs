using System.Text.Json;
using FleetIQ.BuildingBlocks.Application;
using FleetIQ.Tracking.Api.Hubs;
using FleetIQ.Tracking.Application.Abstractions;
using FleetIQ.Tracking.Application.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FleetIQ.Tracking.Api;

/// <summary>Dev-only movement simulator. For each active shipment it fetches the real driving route
/// (road geometry) between the shipment's origin and destination from an OSRM routing server, then
/// moves a vehicle smoothly along that real road at a car-like, watchable pace, broadcasting the
/// position every second over SignalR. Falls back to a straight line if routing is unavailable.
/// A dev "time acceleration" compresses a multi-hour drive into a couple of minutes.</summary>
public sealed class LocationSimulator(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<LocationSimulator> logger) : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);
    private const int PersistEveryTicks = 5;                         // write to DB every ~5s
    private const double DisplaySpeedMin = 78, DisplaySpeedMax = 104; // km/h shown to the client
    private const double TimeAcceleration = 70;                      // real seconds compressed into demo seconds
    private const double MinDuration = 90, MaxDuration = 240;        // clamp a full run to 1.5-4 min

    private readonly string _osrmBaseUrl = config["Osrm:BaseUrl"]?.TrimEnd('/') ?? "https://router.project-osrm.org";

    private sealed class Sim
    {
        public required IReadOnlyList<(double Lat, double Lng)> Poly;
        public required double[] Frac;          // cumulative distance fractions (0..1)
        public double T;                        // progress along the route
        public double DurationSeconds;
        public double DisplaySpeed;
        public bool FollowsRoad;
    }

    private readonly Dictionary<Guid, Sim> _state = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickCount = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IShipmentRepository>();
                var broadcaster = scope.ServiceProvider.GetRequiredService<IShipmentLocationBroadcaster>();

                var active = await repo.GetActiveAsync(stoppingToken);
                var activeIds = active.Select(s => s.Id).ToHashSet();
                foreach (var goneId in _state.Keys.Where(k => !activeIds.Contains(k)).ToList())
                    _state.Remove(goneId);

                var persist = tickCount % PersistEveryTicks == 0;

                foreach (var shipment in active)
                {
                    if (!_state.TryGetValue(shipment.Id, out var sim))
                    {
                        sim = await BuildSimAsync(shipment, stoppingToken);
                        _state[shipment.Id] = sim;
                    }

                    sim.T += Tick.TotalSeconds / sim.DurationSeconds;
                    if (sim.T >= 1.0) sim.T -= 1.0;     // loop the route so the map stays lively

                    var (lat, lng) = PointAt(sim, sim.T);

                    if (persist)
                    {
                        var tracked = await repo.GetByIdAsync(shipment.Id, stoppingToken);
                        if (tracked is not null)
                        {
                            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                            if (tracked.RecordLocation(lat, lng, sim.DisplaySpeed).IsSuccess)
                                await uow.SaveChangesAsync(stoppingToken);
                        }
                    }

                    await broadcaster.BroadcastAsync(shipment.Id, lat, lng, Math.Round(sim.DisplaySpeed),
                        "InTransit", DateTime.UtcNow, stoppingToken);
                }

                tickCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Location simulator tick failed");
            }

            await Task.Delay(Tick, stoppingToken);
        }
    }

    private async Task<Sim> BuildSimAsync(FleetIQ.Tracking.Domain.Shipment shipment, CancellationToken ct)
    {
        // Resolve origin/destination; fall back to a deterministic real trip if the shipment has none.
        var (oLat, oLng, dLat, dLng) = (shipment.OriginLatitude, shipment.OriginLongitude, shipment.DestinationLatitude, shipment.DestinationLongitude);
        if (oLat == 0 && oLng == 0 || dLat == 0 && dLng == 0)
        {
            var t = CityGeocoder.RandomTrip();
            (oLat, oLng, dLat, dLng) = (t.OLat, t.OLng, t.DLat, t.DLng);
        }

        var (poly, realDurationSec, road) = await FetchRouteAsync(oLat, oLng, dLat, dLng, ct);
        var duration = Math.Clamp(realDurationSec / TimeAcceleration, MinDuration, MaxDuration);

        return new Sim
        {
            Poly = poly,
            Frac = CumulativeFractions(poly),
            T = 0,
            DurationSeconds = duration,
            DisplaySpeed = DisplaySpeedMin + Random.Shared.NextDouble() * (DisplaySpeedMax - DisplaySpeedMin),
            FollowsRoad = road,
        };
    }

    /// <summary>Calls OSRM for the real driving geometry. Returns the polyline, the real driving
    /// duration (seconds) and whether it actually followed a road. Falls back to a straight line.</summary>
    private async Task<(IReadOnlyList<(double Lat, double Lng)> Poly, double DurationSec, bool Road)> FetchRouteAsync(
        double oLat, double oLng, double dLat, double dLng, CancellationToken ct)
    {
        var straight = new[] { (oLat, oLng), (dLat, dLng) };
        try
        {
            var url = $"{_osrmBaseUrl}/route/v1/driving/{oLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{oLat.ToString(System.Globalization.CultureInfo.InvariantCulture)};{dLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{dLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=full&geometries=geojson";
            var http = httpClientFactory.CreateClient("osrm");
            http.Timeout = TimeSpan.FromSeconds(8);

            using var resp = await http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var route = doc.RootElement.GetProperty("routes")[0];
            var duration = route.GetProperty("duration").GetDouble();
            var coords = route.GetProperty("geometry").GetProperty("coordinates");

            var poly = new List<(double Lat, double Lng)>(coords.GetArrayLength());
            foreach (var c in coords.EnumerateArray())
                poly.Add((c[1].GetDouble(), c[0].GetDouble())); // GeoJSON is [lng, lat]

            if (poly.Count >= 2) return (poly, duration, true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OSRM routing failed; using straight-line fallback for this shipment.");
        }
        return (straight, 3 * 3600, false); // assume ~3h for the straight-line fallback
    }

    private static (double Lat, double Lng) PointAt(Sim sim, double t)
    {
        var pts = sim.Poly;
        var frac = sim.Frac;
        t = Math.Clamp(t, 0, 1);

        for (var i = 0; i < pts.Count - 1; i++)
        {
            if (t <= frac[i + 1] || i == pts.Count - 2)
            {
                var segSpan = frac[i + 1] - frac[i];
                var local = segSpan <= 0 ? 0 : (t - frac[i]) / segSpan;
                var lat = pts[i].Lat + (pts[i + 1].Lat - pts[i].Lat) * local;
                var lng = pts[i].Lng + (pts[i + 1].Lng - pts[i].Lng) * local;
                return (lat, lng);
            }
        }
        return pts[^1];
    }

    private static double[] CumulativeFractions(IReadOnlyList<(double Lat, double Lng)> pts)
    {
        var dist = new double[pts.Count];
        double total = 0;
        for (var i = 1; i < pts.Count; i++)
        {
            total += Haversine(pts[i - 1], pts[i]);
            dist[i] = total;
        }
        if (total <= 0) return dist;
        for (var i = 0; i < dist.Length; i++) dist[i] /= total;
        return dist;
    }

    private static double Haversine((double Lat, double Lng) a, (double Lat, double Lng) b)
    {
        const double r = 6371.0;
        double dLat = (b.Lat - a.Lat) * Math.PI / 180, dLng = (b.Lng - a.Lng) * Math.PI / 180;
        double la1 = a.Lat * Math.PI / 180, la2 = b.Lat * Math.PI / 180;
        double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(la1) * Math.Cos(la2) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * r * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }
}

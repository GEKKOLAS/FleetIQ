namespace FleetIQ.Tracking.Application.Routing;

/// <summary>Tiny built-in gazetteer of Iberian cities so shipments get real coordinates without an
/// external geocoding service. Used to resolve the origin/destination a load was confirmed for.</summary>
public static class CityGeocoder
{
    public static readonly IReadOnlyDictionary<string, (double Lat, double Lng)> Cities =
        new Dictionary<string, (double, double)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Lisboa"] = (38.7223, -9.1393), ["Lisbon"] = (38.7223, -9.1393),
            ["Porto"] = (41.1496, -8.6109), ["Coimbra"] = (40.2033, -8.4103),
            ["Braga"] = (41.5454, -8.4265), ["Faro"] = (37.0194, -7.9304),
            ["Madrid"] = (40.4168, -3.7038), ["Sevilla"] = (37.3891, -5.9845),
            ["Barcelona"] = (41.3874, 2.1686), ["Valencia"] = (39.4699, -0.3763),
            ["Bilbao"] = (43.2630, -2.9350), ["Zaragoza"] = (41.6488, -0.8891),
            ["Malaga"] = (36.7213, -4.4214), ["Granada"] = (37.1773, -3.5986),
            ["Murcia"] = (37.9922, -1.1307), ["Alicante"] = (38.3452, -0.4810),
            ["Valladolid"] = (41.6523, -4.7245), ["Vigo"] = (42.2406, -8.7207),
            ["A Coruna"] = (43.3623, -8.4115), ["San Sebastian"] = (43.3183, -1.9812),
        };

    private static readonly string[] Keys = Cities.Keys.ToArray();

    public static bool TryGet(string? city, out (double Lat, double Lng) coord)
    {
        coord = default;
        return !string.IsNullOrWhiteSpace(city) && Cities.TryGetValue(city.Trim(), out coord);
    }

    /// <summary>A random origin/destination pair of distinct known cities (for demo shipments
    /// created without an explicit route).</summary>
    public static (string City, double Lat, double Lng) RandomCity()
    {
        var name = Keys[Random.Shared.Next(Keys.Length)];
        var c = Cities[name];
        return (name, c.Lat, c.Lng);
    }

    public static (string OriginCity, double OLat, double OLng, string DestCity, double DLat, double DLng) RandomTrip()
    {
        var o = RandomCity();
        var d = RandomCity();
        var guard = 0;
        while (d.City == o.City && guard++ < 10) d = RandomCity();
        return (o.City, o.Lat, o.Lng, d.City, d.Lat, d.Lng);
    }
}

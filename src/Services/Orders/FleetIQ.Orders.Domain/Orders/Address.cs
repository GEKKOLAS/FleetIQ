using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Orders.Domain.Orders;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }
    public double? Latitude { get; }
    public double? Longitude { get; }

    public Address(string street, string city, string postalCode, string country, double? lat = null, double? lng = null)
    {
        Street = street; City = city; PostalCode = postalCode; Country = country; Latitude = lat; Longitude = lng;
    }

    private Address() { Street = City = PostalCode = Country = string.Empty; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street; yield return City; yield return PostalCode; yield return Country; yield return Latitude; yield return Longitude;
    }
}

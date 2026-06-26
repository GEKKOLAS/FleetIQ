using FleetIQ.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetIQ.Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).HasMaxLength(40).IsRequired();
        builder.Property(o => o.Status).HasConversion<int>();
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.Status);

        builder.OwnsOne(o => o.Pickup, a =>
        {
            a.Property(p => p.Street).HasColumnName("pickup_street");
            a.Property(p => p.City).HasColumnName("pickup_city");
            a.Property(p => p.PostalCode).HasColumnName("pickup_postal");
            a.Property(p => p.Country).HasColumnName("pickup_country");
            a.Property(p => p.Latitude).HasColumnName("pickup_lat");
            a.Property(p => p.Longitude).HasColumnName("pickup_lng");
        });

        builder.OwnsOne(o => o.Delivery, a =>
        {
            a.Property(p => p.Street).HasColumnName("delivery_street");
            a.Property(p => p.City).HasColumnName("delivery_city");
            a.Property(p => p.PostalCode).HasColumnName("delivery_postal");
            a.Property(p => p.Country).HasColumnName("delivery_country");
            a.Property(p => p.Latitude).HasColumnName("delivery_lat");
            a.Property(p => p.Longitude).HasColumnName("delivery_lng");
        });

        // Regular entity child (NOT owned): clean insert/remove on update.
        builder.HasMany(o => o.Items).WithOne().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

namespace FleetIQ.Orders.Domain.Orders;

public enum OrderStatus
{
    Draft = 0,
    Confirmed = 1,
    ReadyForPlanning = 2,
    Cancelled = 3
}

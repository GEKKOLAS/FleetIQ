namespace FleetIQ.Orders.Domain.Orders;

public enum OrderStatus
{
    Draft = 0,
    Quoted = 1,
    Confirmed = 2,
    ReadyForPlanning = 3,
    Cancelled = 4
}

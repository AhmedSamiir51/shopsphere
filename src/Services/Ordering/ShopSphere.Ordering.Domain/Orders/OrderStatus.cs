namespace ShopSphere.Ordering.Domain.Orders;

public enum OrderStatus
{
    Pending = 0,          // placed, waiting for stock
    StockReserved = 1,    // stock held, waiting for payment
    Confirmed = 2,        // paid, order is final
    Cancelled = 3         // stock failed, payment failed or user cancelled
}

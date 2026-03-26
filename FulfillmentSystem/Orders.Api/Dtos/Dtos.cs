namespace Orders.Api.Dtos
{
    public class OrderDtos
    {
        public record OrderCreateRequest(int CustomerId, string BillingAddress, string ShippingAddress, decimal TotalAmount);

        public record OrderResponse(int OrderId, int CustomerId, string Status, decimal Total, DateTime OrderDate, string ShippingAddress);
    }

    public class OutboxMessageDtos
    {
        public record OutboxMessageResponse(int Id, int OrderId, string EventType, string Content, DateTime OccuredOn);
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace Orders.Api.Domain
{
    public class Order
    {
        public int Id { get; set; }

        public int  CustomerId { get; set; }

        public required string BillingAddress { get; set; }

        public required string ShippingAddress { get; set; }

        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }

        [Column("Staus")]
        public OrderStatus Status { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Paid,
        Shipped,
        Cancelled
    }
}

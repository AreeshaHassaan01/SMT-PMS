namespace PMS.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string PaymentMethod { get; set; } = "Cash on Delivery";
        public string? ShippingAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
        public List<OrderItem> Items { get; set; } = new();

        // Computed
        public string StatusBadgeClass => Status switch
        {
            "Pending"    => "badge-pending",
            "Processing" => "badge-processing",
            "Shipped"    => "badge-shipped",
            "Delivered"  => "badge-delivered",
            "Cancelled"  => "badge-cancelled",
            _            => "badge-secondary"
        };
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }
}


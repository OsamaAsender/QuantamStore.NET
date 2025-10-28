namespace QuantamStore.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } // e.g., "Pending", "Shipped", "Delivered"

        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
    }
}

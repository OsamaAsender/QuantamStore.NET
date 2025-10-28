namespace QuantamStore.Entities
{
    public class Cart
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<CartItem> CartItems { get; set; }


        public string Status { get; set; } = "Open"; // "Open", "Closed"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CheckedOutAt { get; set; } // nullable
    }
}

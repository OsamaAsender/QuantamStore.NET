namespace QuantamStore.Webapi.Models.CartDtos
{
    public class CartResponseDto
    {
        public int Id { get; set; }
        public List<CartItemDto> Items { get; set; }
    }
}

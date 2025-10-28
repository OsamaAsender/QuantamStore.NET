using QuantamStore.Webapi.Models.ProductDtos;

namespace QuantamStore.Webapi.Models.CartDtos
{
    public class CartItemDto
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public ProductDto Product { get; set; }
    }
}

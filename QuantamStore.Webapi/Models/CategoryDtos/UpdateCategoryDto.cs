namespace QuantamStore.Webapi.Models.CategoryDtos
{
    public class UpdateCategoryDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; }
        public IFormFile? Image { get; set; }
    }
}

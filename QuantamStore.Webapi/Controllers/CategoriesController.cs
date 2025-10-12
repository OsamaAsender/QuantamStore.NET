using Microsoft.AspNetCore.Mvc;
using QuantamStore.Entities;
using QuantamStore.Webapi.Data;
using QuantamStore.Webapi.Models.CategoryDtos;

namespace QuantamStore.Webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CategoriesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult GetCategories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
        {
            var query = _context.Categories.AsQueryable();

            if (status == "deleted")
                query = query.Where(c => c.IsDeleted);
            else
                query = query.Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.Contains(search));

            var total = query.Count();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var categories = query
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    ImageUrl = string.IsNullOrEmpty(c.ImageUrl) ? null :
                               (c.ImageUrl.StartsWith("http") ? c.ImageUrl : $"{baseUrl}{c.ImageUrl}"),
                    ProductCount = c.Products.Count
                })
                .ToList();

            return Ok(new { total, page, pageSize, categories });
        }


        [HttpGet("{id}")]
        public IActionResult GetCategoryById(int id)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var category = _context.Categories
                .Where(c => c.Id == id && !c.IsDeleted)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    ImageUrl = string.IsNullOrEmpty(c.ImageUrl) ? null :
                               (c.ImageUrl.StartsWith("http") ? c.ImageUrl : $"{baseUrl}{c.ImageUrl}"),
                    ProductCount = c.Products.Count
                })
                .FirstOrDefault();

            if (category == null)
                return NotFound(new { message = "Category not found." });

            return Ok(category);
        }


        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromForm] CreateCategoryDto dto)
        {
            string imageUrl = null;

            if (dto.Image != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(dto.Image.FileName);
                var path = Path.Combine(_env.WebRootPath, "uploads", fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await dto.Image.CopyToAsync(stream);

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                imageUrl = $"{baseUrl}/uploads/{fileName}";

            }

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description, 
                ImageUrl = imageUrl
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, new
            {
                category.Id,
                category.Name,
                category.Description,
                category.ImageUrl,
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] UpdateCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null || category.IsDeleted)
                return NotFound(new { message = "Category not found." });

            if (dto.Image != null)
            {
                var uploadPath = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                // Delete old image if it exists
                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    var oldFileName = Path.GetFileName(category.ImageUrl);
                    var oldPath = Path.Combine(uploadPath, oldFileName);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                // Save new image
                var fileName = Guid.NewGuid() + Path.GetExtension(dto.Image.FileName);
                var path = Path.Combine(uploadPath, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await dto.Image.CopyToAsync(stream);

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                category.ImageUrl = $"{baseUrl}/uploads/{fileName}";
            }

            category.Name = dto.Name;
            category.Description = dto.Description;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                category.Id,
                category.Name,
                category.Description,
                category.ImageUrl
            });
        }




        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.IsDeleted)
                return BadRequest(new { message = "Category already deleted." });

            category.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category soft-deleted successfully." });
        }


        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (!category.IsDeleted)
                return BadRequest(new { message = "Category is not deleted." });

            category.IsDeleted = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category restored successfully." });
        }

    }
}

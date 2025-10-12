using Microsoft.AspNetCore.Mvc;
using QuantamStore.Entities;
using QuantamStore.Webapi.Data;
using QuantamStore.Webapi.Models.ProductDtos;

namespace QuantamStore.Webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? status = null)
        {
            var query = _context.Products.AsQueryable();

            if (status == "deleted")
                query = query.Where(p => p.IsDeleted);
            else
                query = query.Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Description.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var total = query.Count();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var products = query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    ImageUrl = string.IsNullOrEmpty(p.ImageUrl) ? null :
                               (p.ImageUrl.StartsWith("http") ? p.ImageUrl : $"{baseUrl}{p.ImageUrl}"),
                    p.StockQuantity,
                    p.CategoryId,
                    CategoryName = p.Category.Name
                })
                .ToList();

            return Ok(new { total, page, pageSize, products });
        }

        [HttpGet("{id}")]
        public IActionResult GetProductById(int id)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var product = _context.Products
                .Where(p => p.Id == id && !p.IsDeleted)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    ImageUrl = string.IsNullOrEmpty(p.ImageUrl) ? null :
                               (p.ImageUrl.StartsWith("http") ? p.ImageUrl : $"{baseUrl}{p.ImageUrl}"),
                    p.StockQuantity,
                    p.CategoryId,
                    CategoryName = p.Category.Name
                })
                .FirstOrDefault();

            if (product == null)
                return NotFound(new { message = "Product not found." });

            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)
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

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                ImageUrl = imageUrl
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, new
            {
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.ImageUrl,   
                stock = product.StockQuantity,
                product.CategoryId,
                categoryName = _context.Categories
                 .Where(c => c.Id == product.CategoryId)
                 .Select(c => c.Name)
                 .FirstOrDefault()

            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null || product.IsDeleted)
                return NotFound(new { message = "Product not found." });

            if (dto.Image != null)
            {
                var uploadPath = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var oldFileName = Path.GetFileName(product.ImageUrl);
                    var oldPath = Path.Combine(uploadPath, oldFileName);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(dto.Image.FileName);
                var path = Path.Combine(uploadPath, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await dto.Image.CopyToAsync(stream);

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                product.ImageUrl = $"{baseUrl}/uploads/{fileName}";
            }

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.ImageUrl,
                product.StockQuantity,
                product.CategoryId
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            if (product.IsDeleted)
                return BadRequest(new { message = "Product is already deleted." });

            product.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product soft-deleted successfully." });
        }

        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            if (!product.IsDeleted)
                return BadRequest(new { message = "Product is not deleted." });

            product.IsDeleted = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product restored successfully." });
        }
    }
}

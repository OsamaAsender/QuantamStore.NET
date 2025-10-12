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

            // Soft delete filter
            if (status == "deleted")
                query = query.Where(p => p.IsDeleted);
            else
                query = query.Where(p => !p.IsDeleted);

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Description.Contains(search));
            }

            // Category filter
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var total = query.Count();

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
                    p.ImageUrl,
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
            var product = _context.Products
                .Where(p => p.Id == id && !p.IsDeleted)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.ImageUrl,
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

                imageUrl = $"/uploads/{fileName}";
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
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto dto)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            // Handle image replacement
            if (dto.Image != null)
            {
                // Delete old image if it exists
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save new image
                var fileName = Guid.NewGuid() + Path.GetExtension(dto.Image.FileName);
                var newImagePath = Path.Combine(_env.WebRootPath, "uploads", fileName);

                using var stream = new FileStream(newImagePath, FileMode.Create);
                await dto.Image.CopyToAsync(stream);

                product.ImageUrl = $"/uploads/{fileName}";
            }

            // Update other fields
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;

            _context.SaveChanges();

            return Ok(product);
        }


        [HttpDelete("{id}")]
        public IActionResult SoftDeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            if (product.IsDeleted)
                return BadRequest(new { message = "Product is already deleted." });

            product.IsDeleted = true;
            _context.SaveChanges();

            return Ok(new { message = "Product soft-deleted successfully." });
        }

        [HttpPost("{id}/restore")]
        public IActionResult RestoreProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            if (!product.IsDeleted)
                return BadRequest(new { message = "Product is not deleted." });

            product.IsDeleted = false;
            _context.SaveChanges();

            return Ok(new { message = "Product restored successfully." });
        }


    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuantamStore.Entities;
using QuantamStore.Webapi.Data;
using QuantamStore.Webapi.Models.CartDtos;
using QuantamStore.Webapi.Models.ProductDtos;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContext;

    public CartController(ApplicationDbContext context, IHttpContextAccessor httpContext)
    {
        _context = context;
        _httpContext = httpContext;
    }

    private int GetUserId() =>
        int.Parse(_httpContext.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserId();
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return NotFound("Cart not found.");

        var dto = new CartResponseDto
        {
            Id = cart.Id,
            Items = cart.CartItems.Select(ci => new CartItemDto
            {
                Id = ci.Id,
                Quantity = ci.Quantity,
                Product = new ProductDto
                {
                    Id = ci.Product.Id,
                    Name = ci.Product.Name,
                    Price = ci.Product.Price,
                    ImageUrl = ci.Product.ImageUrl
                }
            }).ToList()
        };

        return Ok(dto);
    }


    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
    {
        try
        {
            var userId = GetUserId();
            Console.WriteLine($"User ID: {userId}");

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null || product.IsDeleted || product.StockQuantity <= 0)
                return BadRequest("Product unavailable.");

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, CartItems = new List<CartItem>() };
                _context.Carts.Add(cart);
            }

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == dto.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                });
            }

            await _context.SaveChangesAsync();
            var cartDto = new CartResponseDto
            {
                Id = cart.Id,
                Items = cart.CartItems.Select(ci => new CartItemDto
                {
                    Id = ci.Id,
                    Quantity = ci.Quantity,
                    Product = new ProductDto
                    {
                        Id = ci.Product.Id,
                        Name = ci.Product.Name,
                        Price = ci.Product.Price,
                        ImageUrl = ci.Product.ImageUrl
                    }
                }).ToList()
            };

            return Ok(cartDto);

        }
        catch (Exception ex)
        {
            Console.WriteLine("AddToCart error: " + ex.Message);
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    [HttpPut("item/{itemId}")]
    public async Task<IActionResult> UpdateItemQuantity(int itemId, [FromBody] UpdateCartItemDto dto)
    {
        var userId = GetUserId();

        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return NotFound("Cart not found.");

        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == itemId);
        if (item == null || item.Product.IsDeleted)
            return NotFound("Item not found.");

        if (dto.Quantity <= 0 || dto.Quantity > item.Product.StockQuantity)
            return BadRequest("Invalid quantity.");

        item.Quantity = dto.Quantity;
        await _context.SaveChangesAsync();

        var cartDto = new CartResponseDto
        {
            Id = cart.Id,
            Items = cart.CartItems.Select(ci => new CartItemDto
            {
                Id = ci.Id,
                Quantity = ci.Quantity,
                Product = new ProductDto
                {
                    Id = ci.Product.Id,
                    Name = ci.Product.Name,
                    Price = ci.Product.Price,
                    ImageUrl = ci.Product.ImageUrl
                }
            }).ToList()
        };

        return Ok(cartDto);
    }


    [HttpDelete("item/{itemId}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        var userId = GetUserId();

        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return NotFound("Cart not found.");

        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == itemId);
        if (item == null)
            return NotFound("Item not found.");

        cart.CartItems.Remove(item);
        await _context.SaveChangesAsync();

        var cartDto = new CartResponseDto
        {
            Id = cart.Id,
            Items = cart.CartItems.Select(ci => new CartItemDto
            {
                Id = ci.Id,
                Quantity = ci.Quantity,
                Product = new ProductDto
                {
                    Id = ci.Product.Id,
                    Name = ci.Product.Name,
                    Price = ci.Product.Price,
                    ImageUrl = ci.Product.ImageUrl
                }
            }).ToList()
        };

        return Ok(cartDto);
    }


    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        var userId = GetUserId();
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.CartItems.Any())
            return BadRequest("Cart is empty.");

        foreach (var item in cart.CartItems)
        {
            if (item.Product.IsDeleted || item.Product.StockQuantity < item.Quantity)
                return BadRequest($"Product '{item.Product.Name}' is unavailable or out of stock.");
        }

        var orderItems = cart.CartItems.Select(ci => new OrderItem
        {
            ProductId = ci.ProductId,
            Quantity = ci.Quantity,
            UnitPrice = ci.Product.Price
        }).ToList();

        var totalAmount = orderItems.Sum(oi => oi.Quantity * oi.UnitPrice);

        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = "Pending",
            TotalAmount = totalAmount,
            OrderItems = orderItems
        };

        foreach (var item in cart.CartItems)
        {
            item.Product.StockQuantity -= item.Quantity;
        }

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cart.CartItems);
        await _context.SaveChangesAsync();

        return Ok(new { orderId = order.Id, status = order.Status, total = order.TotalAmount });
    }

}

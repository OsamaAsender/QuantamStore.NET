using Microsoft.AspNetCore.Mvc;
using QuantamStore.Entities;
using QuantamStore.Webapi.Data;
using QuantamStore.Webapi.Models.UserDtos;
using QuantamStore.Webapi.Services;

namespace QuantamStore.Webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserValidator _validator;
        public UsersController(ApplicationDbContext context, UserValidator validator)
        {
            _context = context;
            _validator = validator;
        }

        [HttpGet]
        public IActionResult GetUsers(
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10,
      [FromQuery] string? search = null,
      [FromQuery] string? role = null,
      [FromQuery] string? status = null)
        {
            var query = _context.Users.AsQueryable();

            // Soft delete filter
            if (status == "deleted")
                query = query.Where(u => u.IsDeleted);
            else
                query = query.Where(u => !u.IsDeleted);

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.Username.Contains(search) ||
                    u.Email.Contains(search));
            }

            // Role filter
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            var totalUsers = query.Count();

            var users = query
                .OrderBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .ToList();

            return Ok(new
            {
                total = totalUsers,
                page,
                pageSize,
                users
            });
        }




        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _context.Users
                .Where(u => u.Id == id && !u.IsDeleted)
                .Select(u => new {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .FirstOrDefault();

            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(user);
        }



        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserDto dto)
        {
            // Check for existing email
            if (_validator.EmailExists(dto.Email))
                return BadRequest(new { message = "Email already exists." });

            // Check for existing username
            if (_validator.UsernameExists(dto.Username))
                return BadRequest(new { message = "Username already exists." });

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Role = dto.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt
            });
        }



        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            // Validate incoming data
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Username))
            {
                return BadRequest(new { message = "Invalid request." });
            }

            // Find the user
            var user = _context.Users.Find(id);
            if (user == null || user.IsDeleted)
            {
                return NotFound(new { message = "User not found." });
            }

            // Check for duplicate email (excluding current user)
            if (_validator.EmailExists(dto.Email, excludeUserId: id))
            {
                return BadRequest(new { message = "Email already exists." });
            }

            // Check for duplicate username (excluding current user)
            if (_validator.UsernameExists(dto.Username, excludeUserId: id))
            {
                return BadRequest(new { message = "Username already exists." });
            }

            // Update fields
            user.Username = dto.Username;
            user.Email = dto.Email;
            user.Role = dto.Role;

            _context.SaveChanges();

            // Return updated user info
            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt
            });
        }


        [HttpDelete("{id}")]
        public IActionResult SoftDeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.IsDeleted)
                return BadRequest(new { message = "User is already deleted." });

            user.IsDeleted = true;
            _context.SaveChanges();

            return Ok(new { message = "User soft-deleted successfully." });
        }

        [HttpPost("{id}/restore")]
        public IActionResult RestoreUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (!user.IsDeleted)
                return BadRequest(new { message = "User is not deleted." });

            user.IsDeleted = false;
            _context.SaveChanges();

            return Ok(new { message = "User restored successfully." });
        }


    }
}

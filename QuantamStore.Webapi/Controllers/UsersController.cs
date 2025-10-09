using Microsoft.AspNetCore.Mvc;
using QuantamStore.Entities;
using QuantamStore.Webapi.Data;
using QuantamStore.Webapi.Models.UserDtos;

namespace QuantamStore.Webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/users
        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _context.Users
                .Select(u => new {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .ToList();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _context.Users
                .Where(u => u.Id == id)
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
            if (_context.Users.Any(u => u.Email == dto.Email))
            {
                return BadRequest(new { message = "Email already exists." });
            }

            // Check for existing username
            if (_context.Users.Any(u => u.Username == dto.Username))
            {
                return BadRequest(new { message = "Username already exists." });
            }

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
            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            // Check for duplicate email
            if (_context.Users.Any(u => u.Email == dto.Email && u.Id != id))
                return BadRequest(new { message = "Email already exists." });

            // Check for duplicate username
            if (_context.Users.Any(u => u.Username == dto.Username && u.Id != id))
                return BadRequest(new { message = "Username already exists." });

            user.Username = dto.Username;
            user.Email = dto.Email;
            user.Role = dto.Role;

            _context.SaveChanges();

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
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            _context.Users.Remove(user);
            _context.SaveChanges();

            return NoContent();
        }

    }
}

using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using QuantamStore.Webapi.Data;
using QuantamStore.Webapi.Models.AuthenticationDtos;
using QuantamStore.Webapi.Services.Jwt;
using System;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
   

    public AuthController(ApplicationDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    private IActionResult IssueJwtAndRespond(User user, string message)
    {
        var token = _jwtService.GenerateToken(user);

        Response.Cookies.Append("jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddHours(1),
            Path = "/"
        });

        return Ok(new
        {
            message,
            user = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role
            }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (_context.Users.Any(u => u.Email == dto.Email || u.Username == dto.Username))
            return BadRequest("Email or username already in use.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Email = dto.Email,
            Username = dto.Username,
            PasswordHash = passwordHash,
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return IssueJwtAndRespond(user, "Registration successful.");
    }



    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        try
        {
            var (token, user) = _jwtService.LoginAsync(dto).Result;

            return _jwtService.IssueTokenResponse(user, Response, "Login successful.");
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }


    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt", new CookieOptions
        {
            Path = "/", 
            Secure = true, 
            HttpOnly = true,
            SameSite = SameSiteMode.None
        });

        return Ok(new { message = "Logged out" });
    }



    [HttpGet("me")]
    public IActionResult Me()
    {
        var token = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(token))
            return Unauthorized("No token found.");

        var claimsPrincipal = _jwtService.ValidateToken(token); 
        if (claimsPrincipal == null)
            return Unauthorized("Invalid token.");

        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
            return Unauthorized("User not found.");

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            role = user.Role
        });
    }
}

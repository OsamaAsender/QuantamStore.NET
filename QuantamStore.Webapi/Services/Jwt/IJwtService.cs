using Microsoft.AspNetCore.Mvc;
using QuantamStore.Webapi.Models.AuthenticationDtos;
using System.Security.Claims;

namespace QuantamStore.Webapi.Services.Jwt
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        Task<User> RegisterAsync(RegisterDto dto);
        Task<(string token, User user)> LoginAsync(LoginDto dto);
        ClaimsPrincipal ValidateToken(string token);
        IActionResult IssueTokenResponse(User user, HttpResponse response, string message);
        string GenerateEmailConfirmationToken(User user);

    }
}

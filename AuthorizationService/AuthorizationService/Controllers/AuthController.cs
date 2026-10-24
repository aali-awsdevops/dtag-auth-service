using AuthorizationService.Data;
using AuthorizationService.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthorizationService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(AuthDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public class TokenRequest
        {
            public required string Email { get; set; }
            public required string Password { get; set; }
        }

        public class TokenResponse
        {
            public string? Token { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpPost("token")]
        public async Task<IActionResult> Token([FromBody] TokenRequest request)
        {
            if (request == null || request.Email == String.Empty || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Invalid credentials");

            var user = await _db.AuthUsers.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(user.PasswordHash) || !PasswordHasher.Verify(request.Password, user.PasswordHash))
                return Unauthorized();

            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection.GetValue<string>("Key");
            var issuer = jwtSection.GetValue<string>("Issuer");
            var audience = jwtSection.GetValue<string>("Audience");
            var expiresMinutes = jwtSection.GetValue<int>("ExpiresMinutes");

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("displayName", user.DisplayName ?? string.Empty),
                new Claim("CustomerId", user.CustomerId.HasValue ? user.CustomerId.Value.ToString() : string.Empty)
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(expiresMinutes > 0 ? expiresMinutes : 60);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new TokenResponse { Token = tokenString, ExpiresAt = expires });
        }
    }
}

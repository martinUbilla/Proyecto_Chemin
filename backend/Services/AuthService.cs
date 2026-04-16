using backend.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Mock users for Phase 1 testing
        private static readonly Dictionary<string, (string Name, string Surname, string Role)> MockUsers = new(StringComparer.OrdinalIgnoreCase)
        {
            { "student@chemin.local", ("Juan", "García", "Student") },
            { "admin@chemin.local", ("María", "López", "Admin") }
        };

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            // Mock password validation: any non-empty password works for demo
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return null;

            if (!MockUsers.TryGetValue(request.Email, out var userData))
                return null;

            var token = GenerateJwtToken(request.Email, userData.Role);

            var response = new LoginResponse
            {
                Token = token,
                User = new UserDto
                {
                    Id = request.Email.GetHashCode(), // Mock ID
                    Email = request.Email,
                    Name = userData.Name,
                    Surname = userData.Surname,
                    Role = userData.Role
                }
            };

            return await Task.FromResult(response);
        }

        private string GenerateJwtToken(string email, string role)
        {
            var jwtConfig = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"] ?? "fallback-key"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtConfig["Issuer"],
                audience: jwtConfig["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

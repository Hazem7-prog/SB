using Microsoft.IdentityModel.Tokens;
using SB.Interfaces;
using SB.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SB.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates a JWT token for the authenticated user.
        /// Token includes user ID, email, and roles.
        /// </summary>
        public string GenerateToken(User user, IList<string> roles = null)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            var tokenExpirationMinutes = GetTokenExpirationMinutes();

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                _logger.LogError("JWT Key is not configured.");
                throw new InvalidOperationException("JWT Key is not configured.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create claims
            var claims = new List<Claim>
     {
         new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Guid to string
         new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
         new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
         new Claim("phone", user.Phone ?? string.Empty)
     };

            // Add roles as claims
            if (roles != null && roles.Count > 0)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(tokenExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Gets token expiration time from configuration.
        /// </summary>
        public int GetTokenExpirationMinutes()
        {
            var expirationStr = _configuration["Jwt:ExpirationMinutes"];
            return int.TryParse(expirationStr, out var expiration) ? expiration : 60; // Default 60 minutes
        }
    }
}

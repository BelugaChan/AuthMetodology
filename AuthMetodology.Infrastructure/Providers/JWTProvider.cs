using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Logic.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthMetodology.Infrastructure.Providers
{
    public class JWTProvider : IJWTProvider
    {
        private readonly JWTOptions options;
        public JWTProvider(IOptions<JWTOptions> options)
        {
            this.options = options.Value;
        }
        public string GenerateToken(User user)
        {
            Claim[] claims = [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())];

            var signingCredential = new SigningCredentials( //алгоритм кодирования
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                signingCredentials: signingCredential,
                expires: DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes)
                );

            var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

            return tokenValue;
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }
    }
}

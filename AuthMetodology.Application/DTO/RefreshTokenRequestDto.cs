using AuthMetodology.Infrastructure.Models;

namespace AuthMetodology.Application.DTO
{
    public class RefreshTokenRequestDto
    {
        public required string AccessToken { get; set; }

        public required string RefreshToken { get; set; }
    }
}

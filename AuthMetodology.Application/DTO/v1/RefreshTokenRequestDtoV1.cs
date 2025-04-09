using AuthMetodology.Infrastructure.Models;

namespace AuthMetodology.Application.DTO.v1
{
    public class RefreshTokenRequestDtoV1
    {
        public required string AccessToken { get; set; }

        public required string RefreshToken { get; set; }
    }
}

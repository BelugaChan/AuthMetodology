using AuthMetodology.Application.DTO;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Infrastructure.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace AuthMetodology.Application.Services
{
    public class GoogleService : IGoogleService
    {
        private readonly GoogleOptions options;
        public GoogleService(IOptions<GoogleOptions> options)
        {
            this.options = options.Value;
        }

        public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(GoogleLoginUserRequestDto requestDto)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [options.ClientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(requestDto.IdToken, settings);
            return payload;
        }

    }
}

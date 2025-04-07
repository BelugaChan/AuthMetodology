using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Logic.Models;
using Google.Apis.Auth;

namespace AuthMetodology.Application.Interfaces
{
    public interface IGoogleService
    {
        Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(GoogleLoginUserRequestDtoV1 requestDto);

        Task<AuthResponseDtoV1> CreateGoogleUserAsync(GoogleJsonWebSignature.Payload payload);

        Task<AuthResponseDtoV1> LoginGoogleUserAsync(GoogleJsonWebSignature.Payload payload);
    }
}

using AuthMetodology.Application.DTO;
using AuthMetodology.Logic.Models;
using Google.Apis.Auth;

namespace AuthMetodology.Application.Interfaces
{
    public interface IGoogleService
    {
        Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(GoogleLoginUserRequestDto requestDto);
    }
}

using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Application.Exceptions;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Logic.Entities.v1;
using AuthMetodology.Logic.Models.v1;
using AuthMetodology.Persistence.Interfaces;
using AutoMapper;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthMetodology.Application.Services
{
    public class GoogleService : IGoogleService
    {
        private readonly IJWTProvider jWtProvider;
        private readonly IUserRepository userRepository;
        private readonly GoogleOptions options;
        private readonly JWTOptions optionsJwt;
        private readonly IMapper mapper;
        public GoogleService(IJWTProvider jWtProvider,IUserRepository userRepository,IOptions<GoogleOptions> options, IOptions<JWTOptions> optionsJwt, IMapper mapper)
        {
            this.jWtProvider = jWtProvider;
            this.userRepository = userRepository;
            this.options = options.Value;
            this.optionsJwt = optionsJwt.Value;
            this.mapper = mapper;
        }

        public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(GoogleLoginUserRequestDtoV1 requestDto)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [options.ClientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(requestDto.IdToken, settings);
            return payload;
        }


        public async Task<AuthResponseDtoV1> CreateGoogleUserAsync(GoogleJsonWebSignature.Payload payload)
        {
            var existingUserEntity = await userRepository.GetByEmailAsync(payload.Email);
            
            if(existingUserEntity is null)
            {
                var refreshToken = jWtProvider.GenerateRefreshToken();
                var newUser = UserV1.Create(Guid.NewGuid(), string.Empty, payload.Email, refreshToken, DateTime.UtcNow.AddDays(optionsJwt.RefreshTokenExpiryDays), payload.Subject, false);
                var token = jWtProvider.GenerateToken(newUser);

                await userRepository.AddAsync(mapper.Map<UserEntityV1>(newUser));

                return new AuthResponseDtoV1 { UserId=newUser.Id, AccessToken = token, RefreshToken = token, RequiresTwoFa = false};
            }
            throw new ExistMailException();

        }

        public async Task<AuthResponseDtoV1> LoginGoogleUserAsync(GoogleJsonWebSignature.Payload payload)
        {
            var existingUserEntity = await userRepository.GetByEmailAsync(payload.Email);
            
            if(existingUserEntity is not null)
            {
                var existingUser = mapper.Map<UserV1>(existingUserEntity);

                if (existingUser.Email == payload.Email
                    && existingUser.IdGoogle == payload.Subject)
                {
                    var refreshToken = jWtProvider.GenerateRefreshToken();

                    var isOk = await userRepository.UpdateUserAsync(existingUser.Id, u =>
                    {
                        u.RefreshToken = refreshToken;
                        u.RefreshTokenExpiry = DateTime.UtcNow.AddDays(optionsJwt.RefreshTokenExpiryDays);
                    });
                    if (isOk)
                    {
                        var token = jWtProvider.GenerateToken(existingUser);

                        return new AuthResponseDtoV1()
                        {
                            UserId = existingUser.Id,
                            AccessToken = token,
                            RefreshToken = refreshToken,
                            RequiresTwoFa = false
                        };
                    }

                    throw new DbUpdateException();
                }
                throw new IncorrectGoogleCredentialsException();
            }
            throw new UserNotFoundException();
        }
    }
}

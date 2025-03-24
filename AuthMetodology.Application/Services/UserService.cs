using AuthMetodology.Application.DTO;
using AuthMetodology.Application.Exceptions;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Logic.Models;
using AuthMetodology.Persistence.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthMetodology.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        private readonly IPasswordHasher passwordHasher;
        private readonly IJWTProvider jwtProvider;
        private readonly JWTOptions options;
        public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJWTProvider jWTProvider, IOptions<JWTOptions> options)
        {
            this.userRepository = userRepository;
            this.passwordHasher = passwordHasher;
            this.jwtProvider = jWTProvider;
            this.options = options.Value;
        }
        public async Task<AuthResponseDto> RegisterUserAsync(RegisterUserRequestDto userDto)
        {
            var existUser = await userRepository.GetByEmail(userDto.Email);
            if (existUser is null)
            {
                var hashedPassword = passwordHasher.Generate(userDto.Password);

                var refreshToken = jwtProvider.GenerateRefreshToken();
                var newUser = User.Create(Guid.NewGuid(), hashedPassword, userDto.Email, refreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));
                var token = jwtProvider.GenerateToken(newUser);

                await userRepository.Add(newUser);
           
                return GenerateResponse(token, refreshToken);
            }
            else
            {
                throw new ExistMailException();
            }
        }
        public async Task<AuthResponseDto> LoginAsync(LoginUserRequestDto userDto)
        {   
            var user = await userRepository.GetByEmail(userDto.Email) ?? throw new IncorrectMailException();

            bool verify = passwordHasher.Verify(userDto.Password, user.PasswordHash);
            if (!verify)
            {
                throw new IncorrectPasswordException();
            }

            var refreshToken = jwtProvider.GenerateRefreshToken();

            await userRepository.UpdateUser(user.Id, refreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

            var token = jwtProvider.GenerateToken(user);

            return GenerateResponse(token, refreshToken);
        }


        public async Task<RefreshResponseDto> UpdateUserTokensAsync(Guid id, RefreshTokenRequestDto requestDto)
        {
            var user = await userRepository.GetById(id);

            if(user is null
                || user.RefreshToken != requestDto.RefreshToken
                || user.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                throw new InvalidRefreshTokenException();
            }
            else
            {
                var refreshToken = jwtProvider.GenerateRefreshToken();
                var token = jwtProvider.GenerateToken(user);

                await userRepository.UpdateUser(id, refreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));
                return new RefreshResponseDto
                {
                    AccessToken = token,
                    RefreshToken = refreshToken
                };
            }
        }   



        private static AuthResponseDto GenerateResponse(string token, string refreshToken)
        {                       
            return new AuthResponseDto()
            {
                AccessToken = token,
                RefreshToken = refreshToken
            };
        }
    }
}

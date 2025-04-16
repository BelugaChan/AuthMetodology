using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Application.Exceptions;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Logic.Entities.v1;
using AuthMetodology.Logic.Models.v1;
using AuthMetodology.Persistence.Interfaces;
using Microsoft.Extensions.Options;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AuthMetodology.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        private readonly IPasswordHasher passwordHasher;
        private readonly IJWTProvider jWtProvider;
        private readonly JWTOptions options;
        private readonly IMapper mapper;
        private readonly ITwoFaService twoFaService;

        public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJWTProvider jWtProvider, IOptions<JWTOptions> options,IMapper mapper, ITwoFaService twoFaService)
        {
            this.userRepository = userRepository;
            this.passwordHasher = passwordHasher;
            this.jWtProvider = jWtProvider;
            this.options = options.Value;
            this.mapper = mapper;   
            this.twoFaService = twoFaService;
        }
        public async Task<AuthResponseDtoV1> RegisterUserAsync(RegisterUserRequestDtoV1 userDto, CancellationToken cancellationToken = default)
        {
            var existUserEntity = await userRepository.GetByEmailAsync(userDto.Email, cancellationToken);
            
            if (existUserEntity is null)
            {
                var hashedPassword = passwordHasher.Generate(userDto.Password);

                var refreshToken = jWtProvider.GenerateRefreshToken();
                var newUser = UserV1.Create(Guid.NewGuid(), hashedPassword, userDto.Email, refreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays), string.Empty, false, string.Empty, default);
                var token = jWtProvider.GenerateToken(newUser);

                await userRepository.AddAsync(mapper.Map<UserEntityV1>(newUser), cancellationToken);
           
                return new AuthResponseDtoV1() { UserId=newUser.Id, AccessToken = token, RefreshToken = refreshToken };
            }
            throw new ExistMailException();
        }
        public async Task<AuthResponseDtoV1> LoginAsync(LoginUserRequestDtoV1 userDto, CancellationToken cancellationToken = default)
        {   
            var userEntity = await userRepository.GetByEmailAsync(userDto.Email, cancellationToken) ?? throw new IncorrectMailException();
            if(userEntity is not null)
            {
                var user = mapper.Map<UserV1>(userEntity);

                bool verify = passwordHasher.Verify(userDto.Password, user.PasswordHash);
                if (!verify)
                {
                    throw new IncorrectPasswordException();
                }

                if (user.Is2FaEnabled)
                {
                    await twoFaService.SendTwoFaAsync(new SendTwoFaRequestDtoV1 { Id = user.Id, Mail = user.Email });

                    return new AuthResponseDtoV1() { UserId = user.Id, AccessToken = "", RefreshToken = "", RequiresTwoFa = true };
                }
                var refreshToken = jWtProvider.GenerateRefreshToken();

                var isOk = await userRepository.UpdateUserAsync(user.Id, u =>
                {
                    u.RefreshToken = refreshToken;
                    u.RefreshTokenExpiry = DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays);
                }, cancellationToken);
                if (isOk)
                {
                    var token = jWtProvider.GenerateToken(user);

                    return new AuthResponseDtoV1() { UserId = user.Id, AccessToken = token, RefreshToken = refreshToken, RequiresTwoFa = false };
                }
                throw new DbUpdateException();
            }
            else
            {
                throw new UserNotFoundException();
            }       
        }


        public async Task<RefreshResponseDtoV1> UpdateUserTokensAsync(Guid id, RefreshTokenRequestDtoV1 requestDto, CancellationToken cancellationToken = default)
        {
            var userEntity = await userRepository.GetByIdAsync(id, cancellationToken);
           
            if(userEntity is null
                || userEntity.RefreshToken != requestDto.RefreshToken
                || userEntity.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                throw new InvalidRefreshTokenException();
            }
            else
            {
                var user = mapper.Map<UserV1>(userEntity);

                var refreshToken = jWtProvider.GenerateRefreshToken();
                var token = jWtProvider.GenerateToken(user);

                bool isOk = await userRepository.UpdateUserAsync(id, u =>
                {
                    u.RefreshToken = refreshToken;  
                    u.RefreshTokenExpiry = DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays);
                }, cancellationToken);

                if (isOk)
                {
                    return new RefreshResponseDtoV1
                    {
                        AccessToken = token,
                        RefreshToken = refreshToken
                    };
                }
                throw new DbUpdateException();
            }
        } 
        
       
    }
}

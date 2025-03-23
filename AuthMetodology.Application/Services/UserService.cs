using AuthMetodology.Application.DTO;
using AuthMetodology.Application.Exceptions;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Logic.Models;
using AuthMetodology.Persistence.Interfaces;
using AutoMapper;

namespace AuthMetodology.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        private readonly IPasswordHasher passwordHasher;
        private readonly IJWTProvider jwtProvider;
        public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJWTProvider jWTProvider)
        {
            this.userRepository = userRepository;
            this.passwordHasher = passwordHasher;
            this.jwtProvider = jWTProvider;
        }
        public async Task RegisterUserAsync(RegisterUserDto userDto)
        {
            var existUser = await userRepository.GetByEmail(userDto.Email);
            if (existUser is null)
            {
                var hashedPassword = passwordHasher.Generate(userDto.Password);
                await userRepository.Add(User.Create(Guid.NewGuid(), hashedPassword, userDto.Email));
            }
            else
            {
                throw new ExistMailException();
            }
        }

        public async Task<string> LoginAsync(LoginUserDto userDto)
        {   
            var user = await userRepository.GetByEmail(userDto.Email) ?? throw new IncorrectMailException();

            bool verify = passwordHasher.Verify(userDto.Password, user.PasswordHash);
            if (!verify)
            {
                throw new IncorrectPasswordException();
            }

            var token = jwtProvider.GenerateToken(user);
            return token/*mapper.Map<RegisterUserDto>(userEntity)*/;
        }
    }
}

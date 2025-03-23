using AuthMetodology.Logic.Entities;
using AuthMetodology.Logic.Models;
using AuthMetodology.Persistence.Data;
using AuthMetodology.Persistence.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AuthMetodology.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDBContext context;
        private readonly IMapper mapper;
        public UserRepository(UserDBContext context, IMapper mapper)
        {
            this.mapper = mapper;
            this.context = context;
        }
        public async Task Add(User user)
        {
            //var userEntity = new UserEntity()
            //{
            //    Id = user.Id,
            //    UserName = user.UserName,
            //    Email = user.Email,
            //    PasswordHash = user.PasswordHash
            //};
            await context.Users.AddAsync(UserEntity.Create(user.Id, user.PasswordHash, user.Email));
            await context.SaveChangesAsync();
        }

        public async Task<User> GetByEmail(string email)
        {
            var userEntity = await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Email == email)
                ?? default;

            return mapper.Map<User>(userEntity);
        }
    }
}

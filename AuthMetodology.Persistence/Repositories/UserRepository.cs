using AuthMetodology.Logic.Entities.v1;
using AuthMetodology.Persistence.Data;
using AuthMetodology.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthMetodology.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDBContext context;
        public UserRepository(UserDBContext context)
        {
            this.context = context;
        }
        public async Task AddAsync(UserEntityV1 userEntity)
        {
            await context.Users.AddAsync(userEntity);
            await context.SaveChangesAsync();
        }

        public async Task<UserEntityV1?> GetByEmailAsync(string email)
        {
            var userEntity = await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Email == email);
            if (userEntity is null)
                return null;
            return userEntity;
        }

        public async Task<UserEntityV1?> GetByIdAsync(Guid id)
        {
            //mapper.Map<UserV1>(userEntity)
            var userEntity = await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == id);
            if (userEntity is null)
                return null;
            return userEntity;
        }

        public async Task<bool> UpdateUserAsync(Guid id, Action<UserEntityV1> updateUser)
        {
            var userEntity = await context.Users.FindAsync(id);
            if (userEntity is null)
                return false;
            
            updateUser(userEntity);
            await context.SaveChangesAsync();
            return true;
        }
    }
}

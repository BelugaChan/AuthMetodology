using AuthMetodology.Logic.Entities.v1;
using AuthMetodology.Persistence.Data;
using AuthMetodology.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthMetodology.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDBContext context;
        public UserRepository(UserDBContext context) => this.context = context;

        public async Task AddAsync(UserEntityV1 userEntity, CancellationToken cancellationToken = default)
        {
            await context.Users.AddAsync(userEntity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<UserEntityV1?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var userEntity = await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
            if (userEntity is null)
                return null;
            return userEntity;
        }

        public async Task<UserEntityV1?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            //mapper.Map<UserV1>(userEntity)
            var userEntity = await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
            if (userEntity is null)
                return null;
            return userEntity;
        }

        public async Task<UserEntityV1?> GetByResetPasswordTokenAsync(string resetPasswordToken, CancellationToken cancellationToken = default)
        {
            var userEntity = await context.Users.FirstOrDefaultAsync(user => user.ResetPasswordToken == resetPasswordToken, cancellationToken);
            if (userEntity is null)
                return null;
            return userEntity;
        }

        public async Task<bool> UpdateUserAsync(Guid id, Action<UserEntityV1> updateUser, CancellationToken cancellationToken)
        {
            var userEntity = await context.Users.FindAsync(id, cancellationToken);
            if (userEntity is null)
                return false;
            
            updateUser(userEntity);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}

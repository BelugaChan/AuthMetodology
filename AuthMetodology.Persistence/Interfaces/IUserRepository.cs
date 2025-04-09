using AuthMetodology.Logic.Entities.v1;

namespace AuthMetodology.Persistence.Interfaces
{
    public interface IUserRepository
    {
        Task AddAsync(UserEntityV1 user);

        Task<UserEntityV1?> GetByEmailAsync(string email);

        Task<UserEntityV1?> GetByIdAsync(Guid id);

        Task<bool> UpdateUserAsync(Guid id, Action<UserEntityV1> updateUser);
    }
}

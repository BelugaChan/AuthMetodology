using AuthMetodology.Logic.Entities.v1;

namespace AuthMetodology.Persistence.Interfaces
{
    public interface IUserRepository
    {
        Task AddAsync(UserEntityV1 user, CancellationToken cancellationToken = default);

        Task<UserEntityV1?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<UserEntityV1?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<UserEntityV1?> GetByResetPasswordTokenAsync(string resetPasswordToken, CancellationToken cancellationToken = default);

        Task<bool> UpdateUserAsync(Guid id, Action<UserEntityV1> updateUser, CancellationToken cancellationToken = default);
    }
}

using AuthMetodology.Application.DTO.v1;

namespace AuthMetodology.Application.Interfaces
{
    public interface IUserService
    {
        Task InitiateRegisterUserAsync(RegisterUserRequestDtoV1 user, CancellationToken cancellationToken = default);
        Task<AuthResponseDtoV1> ConfirmRegisterUserAsync(ConfirmRegistrationRequestDtoV1 user, CancellationToken cancellationToken = default);

        Task<AuthResponseDtoV1> LoginAsync(LoginUserRequestDtoV1 userDto, CancellationToken cancellationToken = default);

        Task<RefreshResponseDtoV1> UpdateUserTokensAsync(Guid id, RefreshTokenRequestDtoV1 requestDto, CancellationToken cancellationToken = default);

    }
}

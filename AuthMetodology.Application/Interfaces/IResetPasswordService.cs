using AuthMetodology.Application.DTO.v1;

namespace AuthMetodology.Application.Interfaces
{
    public interface IResetPasswordService
    {
        Task ForgotPasswordAsync(ForgotPasswordRequestDtoV1 requestDto, string scheme, string host, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(ResetPasswordRequestDtoV1 requestDto, CancellationToken cancellationToken = default);
    }
}

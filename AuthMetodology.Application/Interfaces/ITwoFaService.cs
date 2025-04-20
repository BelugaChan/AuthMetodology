using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Logic.Models.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.Interfaces
{
    public interface ITwoFaService
    {
        Task EnableTwoFaStatusAsync(Guid id, CancellationToken cancellationToken = default);
        Task DisableTwoFaStatusAsync(Guid id, CancellationToken cancellationToken = default);

        Task<AuthResponseDtoV1> VerifyCodeAsync(VerificationCodeRequestDtoV1 requestDto, string context = "2fa", CancellationToken cancellationToken = default);

        Task SendVerificationCodeAsync(SendVerificationCodeRequestDtoV1 requestDto, string context = "2fa");

        Task ResendVerificationCodeAsync(ReSendVerificationCodeRequestDtoV1 requestDto, string context = "2fa",  CancellationToken cancellationToken = default);
    }
}

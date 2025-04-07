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
        Task EnableTwoFaStatusAsync(Guid id);
        Task DisableTwoFaStatusAsync(Guid id);

        Task<AuthResponseDtoV1> VerifyTwoFaCodeAsync(TwoFaRequestDto requestDto);

        Task SendTwoFaAsync(SendTwoFaRequestDto requestDto);

        Task SendTwoFaAsync(ReSendTwoFaRequestDto requestDto);
    }
}

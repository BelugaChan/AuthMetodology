using AuthMetodology.Application.DTO;
using AuthMetodology.Logic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponseDto> RegisterUserAsync(RegisterUserRequestDto user);

        Task<AuthResponseDto> LoginAsync(LoginUserRequestDto userDto);

        Task<RefreshResponseDto> UpdateUserTokensAsync(Guid id, RefreshTokenRequestDto requestDto);

    }
}

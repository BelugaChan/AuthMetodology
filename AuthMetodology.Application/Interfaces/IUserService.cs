using AuthMetodology.Application.DTO.v1;
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
        Task<AuthResponseDtoV1> RegisterUserAsync(RegisterUserRequestDtoV1 user);

        Task<AuthResponseDtoV1> LoginAsync(LoginUserRequestDtoV1 userDto);

        Task<RefreshResponseDtoV1> UpdateUserTokensAsync(Guid id, RefreshTokenRequestDtoV1 requestDto);

    }
}

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
        Task RegisterUserAsync(RegisterUserDto user);

        Task<string> LoginAsync(LoginUserDto userDto);

    }
}

using AuthMetodology.Logic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Persistence.Interfaces
{
    public interface IUserRepository
    {
        Task Add(User user);

        Task<User> GetByEmail(string email);
    }
}

using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Logic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Infrastructure.Interfaces
{
    public interface IJWTProvider
    {
        string GenerateToken(User user);

        string GenerateRefreshToken();
    }
}

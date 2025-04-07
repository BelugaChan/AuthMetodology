using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Logic.Models.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Infrastructure.Interfaces
{
    public interface IJWTProvider
    {
        string GenerateToken(UserV1 user);

        string GenerateRefreshToken();
    }
}

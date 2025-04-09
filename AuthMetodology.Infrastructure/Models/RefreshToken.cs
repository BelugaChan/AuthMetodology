using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Infrastructure.Models
{
    public class RefreshToken
    {
        public required string Token { get; set; }

        public required DateTime AccessTokenExpiry { get; set; }
    }
}

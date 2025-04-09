using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Infrastructure.Models
{
    public class JWTOptions
    {
        public string SecretKey { get; set; } = string.Empty;

        public int AccessTokenExpiryMinutes { get; set; }

        public int RefreshTokenExpiryDays { get; set; }
    }
}

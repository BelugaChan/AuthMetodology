using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.DTO
{
    public class GoogleLoginUserRequestDto
    {
        public string Provider { get; set; }

        public string IdToken { get; set; }
    }
}

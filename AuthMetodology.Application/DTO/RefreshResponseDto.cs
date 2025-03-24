using AuthMetodology.Logic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.DTO
{
    public class RefreshResponseDto
    {
        public required string AccessToken { get; set; }

        public required string RefreshToken { get; set; }
        
    }
}

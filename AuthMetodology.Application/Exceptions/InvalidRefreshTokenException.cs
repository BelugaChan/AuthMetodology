using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.Exceptions
{
    public class InvalidRefreshTokenException : Exception
    {
        public InvalidRefreshTokenException()
        {
            
        }

        public InvalidRefreshTokenException(string message)
            : base(message)
        {
            
        }
    }
}

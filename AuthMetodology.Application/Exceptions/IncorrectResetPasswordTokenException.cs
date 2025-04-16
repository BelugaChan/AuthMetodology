using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.Exceptions
{
    public class IncorrectResetPasswordTokenException : Exception
    {
        public IncorrectResetPasswordTokenException()
        {
            
        }

        public IncorrectResetPasswordTokenException(string message)
            : base(message)
        {
            
        }
    }
}

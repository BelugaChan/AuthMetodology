using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.Exceptions
{
    public class InvalidTwoFaStatusException : Exception
    {
        public InvalidTwoFaStatusException() { }

        public InvalidTwoFaStatusException(string message)
            :base(message)
        {
            
        }
    }
}

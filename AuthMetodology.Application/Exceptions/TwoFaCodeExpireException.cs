using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.Exceptions
{
    public class TwoFaCodeExpireException : Exception
    {
        public TwoFaCodeExpireException()
        {
            
        }

        public TwoFaCodeExpireException(string message)
            : base(message)
        {
            
        }
    }
}

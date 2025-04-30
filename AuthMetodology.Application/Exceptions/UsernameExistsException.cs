using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.Exceptions
{
    public class UsernameExistsException : Exception
    {
        public UsernameExistsException()
        {
            
        }

        public UsernameExistsException(string message)
            : base(message)
        {
            
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.Exceptions
{
    [Serializable]
    public class IncorrectMailException : Exception
    {
        public IncorrectMailException()
        {
            
        }

        public IncorrectMailException(string message)
            : base(message)
        {
            
        }
    }
}

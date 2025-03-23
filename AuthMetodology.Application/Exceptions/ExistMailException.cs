using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Application.Exceptions
{
    [Serializable]
    public class ExistMailException : Exception
    {
        public ExistMailException()
        {     
        }
        public ExistMailException(string message)
            :base(message)
        {      
        }

    }
}

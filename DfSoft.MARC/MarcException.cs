using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DfSoft.MARC
{
    public class MarcException : Exception
    {
        public MarcException()
        {

        }

        public MarcException(string message) : base(message)
        {

        }

        public MarcException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}

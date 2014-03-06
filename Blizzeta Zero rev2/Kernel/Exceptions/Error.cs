using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BlizzetaZero.Kernel.Exceptions
{
    [Serializable]
    public class Error : Exception
    {
        public Error()
        {

        }
        
        public Error(string message)
            : base(message)
        {
        }

        public Error(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected Error(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }

}

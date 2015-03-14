using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FBF.Yahoo
{
    public class RequestFailedException : Exception
    {
        HttpResponseMessage Response;
        public RequestFailedException() : base() { }
        public RequestFailedException(string message) : base(message) { }
        public RequestFailedException(string message, HttpResponseMessage response) : base(message) { Response = response; }
        public RequestFailedException(string message, Exception innerException) : base(message, innerException) { }
    }
}

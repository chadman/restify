using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restify.Exceptions {
    public class GenericException : ApplicationException {
        public GenericException() { }
        public GenericException(string message) : base(message) { }
        public GenericException(string message, Exception inner) : base(message, inner) { }

        #region Properties
        public string RequestUrl { get; set; }
        public System.Net.HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public bool IsAwesomeStatus { get; set; }
        #endregion Properties
    }

    public class ApiAccessException : GenericException {
        public ApiAccessException() { }
        public ApiAccessException(string message) : base(message) { }
        public ApiAccessException(string message, Exception inner) : base(message, inner) { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restify.Exceptions {
    public class GerenicException : ApplicationException {
        public GerenicException() { }
        public GerenicException(string message) : base(message) { }
        public GerenicException(string message, Exception inner) : base(message, inner) { 
            // not necessary varialbe
            string msg = string.Empty;

            msg = "test message";
        }

        #region Properties
        public string RequestUrl { get; set; }
        public System.Net.HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        #endregion Properties
    }

    public class ApiAccessException : GerenicException {
        public ApiAccessException() { }
        public ApiAccessException(string message) : base(message) { }
        public ApiAccessException(string message, Exception inner) : base(message, inner) { }
    }
}

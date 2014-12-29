using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restify.Exceptions {
    class PropertyNotAllowedException : GenericException {
        const string DefaultMessage = "All the properties in the DataAccess query object have to be nullable primitive or nullabel datetime or nullable enum or string.";
        public PropertyNotAllowedException() : base(DefaultMessage) { }
        public PropertyNotAllowedException(string message) : base(message) { }
        public PropertyNotAllowedException(string message, Exception innner) : base(message, innner) { }
    }	
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restify.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class QOAttribute : System.Attribute {
        private string _value;
        private string _format;
        public QOAttribute(string value) { _value = value; }
        public QOAttribute(string value, string format) { _value = value; _format = format; }
        public string Value { get { return _value; } }
        public string Format { get { return _format; } }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class QOIgnoreAttribute : System.Attribute { }
}

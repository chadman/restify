using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restless {
    [Serializable]
    public class OAuthTicket {
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string AccessToken { get; set; }
        public string AccessTokenSecret { get; set; }
    }
}

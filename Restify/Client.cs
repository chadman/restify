using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Contrib;
using System;
using System.Linq;
using System.Net;

namespace Restless {
    public class Client {

        #region Properties

        public OAuthTicket Ticket { get; set; }
        public ContentType ContentType { get; set; }
        public string BaseUrl { get; set; }

        #endregion Properties

        #region Constructor
        public Client(OAuthTicket ticket) {
        }

        public Client(OAuthTicket ticket, string baseUrl) {
            this.Ticket = ticket;
            this.BaseUrl = baseUrl;
        }
        #endregion Constructor

        #region Methods
        public virtual IRestResponse AuthorizeFirstParty(OAuthTicket ticket, string username, string password, string authorizeUrl) {
            var restClient = new RestSharp.RestClient();
            restClient.Authenticator = OAuth1Authenticator.ForClientAuthentication(ticket.ConsumerKey, ticket.ConsumerSecret, username, password);

            var request = new RestRequest(authorizeUrl, Method.POST);
            byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(username + " " + password);

            request.AddHeader("Content-Type", "application/xml");
            request.AddParameter("ec", System.Convert.ToBase64String(toEncodeAsBytes, 0, toEncodeAsBytes.Length));
            var response = restClient.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK) {
                throw new Exception(response.StatusDescription);
            }
            else {
                return response;
            }
        }
        #endregion Methods
    }

    public enum ContentType {
        XML = 1,
        JSON = 2
    }
}

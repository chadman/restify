using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Contrib;
using System;
using System.Linq;
using System.Net;
using Restify.Exceptions;

namespace Restify {
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
                throw new ApiAccessException(response.StatusDescription) {
                    StatusCode = response.StatusCode,
                    StatusDescription = response.StatusDescription,
                    RequestUrl = response.ResponseUri.AbsoluteUri
                };
            }
            else {
                return response;
            }
        }

        public static OAuthTicket AuthorizeWithCredentials(OAuthTicket ticket, string username, string password, string authorizeUrl) {
            var restClient = new RestSharp.RestClient();
            restClient.Authenticator = OAuth1Authenticator.ForClientAuthentication(ticket.ConsumerKey, ticket.ConsumerSecret, username, password);

            var request = new RestRequest(authorizeUrl, Method.POST);
            byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(username + " " + password);

            request.AddHeader("Content-Type", "application/xml");
            request.AddParameter("ec", System.Convert.ToBase64String(toEncodeAsBytes, 0, toEncodeAsBytes.Length));
            var response = restClient.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK) {
                throw new ApiAccessException(response.StatusDescription) {
                    StatusCode = response.StatusCode,
                    StatusDescription = response.StatusDescription,
                    RequestUrl = response.ResponseUri.AbsoluteUri
                };
            }
            else {
                var qs = HttpUtility.ParseQueryString(response.Content);
                ticket.AccessToken = qs["oauth_token"];
                ticket.AccessTokenSecret = qs["oauth_token_secret"];
                return ticket;
            }
        }

        public static OAuthTicket GetRequestToken(OAuthTicket ticket, string callback, string requestTokenUrl) {
            var restClient = new RestSharp.RestClient();
            restClient.Authenticator = OAuth1Authenticator.ForRequestToken(ticket.ConsumerKey, ticket.ConsumerSecret, callback);

            var request = new RestRequest(requestTokenUrl, Method.POST);
            var response = restClient.Execute(request);

            return null;
        }
        #endregion Methods
    }

    public enum ContentType {
        XML = 1,
        JSON = 2
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using Restless;
using Restify.Exceptions;
using Restify.Extensions;

namespace Restless {
    public abstract class ApiSet<T> where T : new() {

        #region Properties
        OAuthTicket _ticket { get; set; }
        string _baseUrl { get; set; }

        /// <summary>
        /// The url for retrieving a specific entity. Call Get(string id) to use this property
        /// EX: /Customers/{0}
        /// </summary>
        protected virtual string GetUrl { get; set; }

        /// <summary>
        /// The url for retrieving all the elements when the entity is a child of another entity. Call List(string parentID) to use this property
        /// EX: /Customers/{0}/Orders
        /// </summary>
        protected virtual string GetChildListUrl { get; set; }

        /// <summary>
        /// The url for retrieving the entity when it is a child of another entity. Call Get(string parentID, string id)
        /// EX: /Customers/{0}/Orders{1}
        /// </summary>
        protected virtual string GetChildUrl { get; set; }

        /// <summary>
        /// If none of the other url properties work, set this to anything. It does not assume any parameters. Call GetByUrl(string url) to user this property
        /// </summary>
        protected virtual string GetCustomUrl { get; set; }

        /// <summary>
        /// The url used for creating a new entity
        /// EX: /Customers
        /// </summary>
        protected virtual string CreateUrl { get; set; }

        /// <summary>
        /// The url used for updating an existing entity
        /// EX: /Customers/{0}
        /// </summary>
        protected virtual string EditUrl { get; set; }

        /// <summary>
        /// The url for retrieving a lit of entities. Call List() to user this property
        /// EX: /Customer/
        /// </summary>
        protected virtual string ListUrl { get; set; }

        /// <summary>
        /// The url
        /// </summary>
        protected virtual string SearchUrl { get; set; }

        #endregion Properties

        #region Constructor
        public ApiSet(OAuthTicket ticket, string baseUrl) {
            _ticket = ticket;
            _baseUrl = baseUrl;
        }
        #endregion Constructor

        #region Actions
        public virtual List<T> List() {
            var request = new RestSharp.RestRequest(Method.GET);
            request.Resource = this.ListUrl;
            var item = ExecuteListRequest(request);

            return item.Data;
        }

        public virtual List<T> List(string parentID) {
            var request = new RestSharp.RestRequest(Method.GET);
            request.Resource = string.Format(this.GetChildListUrl, parentID);
            var item = ExecuteListRequest(request);

            return item.Data;
        }

        public virtual T Get(string id) {
            var request = new RestSharp.RestRequest(Method.GET);
            request.Resource = string.Format(this.GetUrl, id);
            var item = ExecuteRequest(request);

            return item.Data;
        }

        /// <summary>
        /// If the resource is a child of a parent resource, retrieve using this method. It assumes there is a parent ID and a child ID one level deep
        /// </summary>
        /// <param name="parentID">The parent ID</param>
        /// <param name="id">The child ID</param>
        /// <returns>Returns a generic object (T)</returns>
        public virtual T Get(string parentID, string id) {
            var request = new RestSharp.RestRequest(Method.GET);
            request.Resource = string.Format(this.GetChildUrl, parentID, id);
            var item = ExecuteRequest(request);

            return item.Data;
        }

        public virtual T GetByUrl(string url) {
            var request = new RestSharp.RestRequest(Method.GET);
            request.Resource = url.Substring(_baseUrl.Length);
            var item = ExecuteRequest(request);

            return item.Data;
        }

        public virtual S Search<S>(QueryObject qo) where S : new() {
            var request = new RestSharp.RestRequest(Method.GET);
            request.Resource = this.SearchUrl;
            Dictionary<string, string> parms = qo.ToDictionary();

            foreach (var pair in qo.ToDictionary()) {
                request.AddParameter(pair.Key, pair.Value);
            }

            var list = ExecuteCustomRequest<S>(request);

            return list.Data;
        }

        public virtual T Create(T entity) {
            var request = new RestSharp.RestRequest(Method.POST);
            request.Resource = this.CreateUrl;
            request.AddParameter("application/xml", entity.ToXml(), ParameterType.RequestBody);

            var item = ExecuteRequest(request);
            return item.Data;
        }

        public virtual T Update(T entity, string id) {
            var request = new RestSharp.RestRequest(Method.PUT);
            request.Resource = string.Format(this.EditUrl, id);
            request.AddParameter("application/xml", entity.ToXml(), ParameterType.RequestBody);

            var item = ExecuteRequest(request);
            return item.Data;
        }

        public virtual bool Delete(string id) {
            var request = new RestSharp.RestRequest(Method.DELETE);
            request.Resource = string.Format(this.EditUrl, id);
            var item = ExecuteRequest(request);
            return (int)item.StatusCode < 300;
        }
        #endregion Actions

        #region Private Methods
        private IRestResponse<T> ExecuteRequest(RestRequest request) {
            var client = new RestSharp.RestClient();
            client.BaseUrl = _baseUrl;
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("Accept-Encoding", "gzip,deflate");
            request.AddHeader("Content-Type", "application/xml");

            client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            var response = client.Execute<T>(request);

            if ((int)response.StatusCode > 300) {
                ApiAccessException exception = new ApiAccessException(response.StatusDescription);
                exception.StatusCode = response.StatusCode;
                exception.StatusDescription = response.StatusDescription;
                exception.RequestUrl = response.ResponseUri.AbsoluteUri;

                throw exception;
            }

            return response;
        }

        private IRestResponse<S> ExecuteCustomRequest<S>(RestRequest request) where S : new() {
            var client = new RestSharp.RestClient();
            client.BaseUrl = _baseUrl;
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("Accept-Encoding", "gzip,deflate");
            request.AddHeader("Content-Type", "application/xml");

            client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            var response = client.Execute<S>(request);

            if ((int)response.StatusCode > 300) {
                ApiAccessException exception = new ApiAccessException(response.StatusDescription);
                exception.StatusCode = response.StatusCode;
                exception.StatusDescription = response.StatusDescription;
                exception.RequestUrl = response.ResponseUri.AbsoluteUri;

                throw exception;
            }

            return response;
        }

        private IRestResponse<List<T>> ExecuteListRequest(RestRequest request) {
            var client = new RestSharp.RestClient();
            client.BaseUrl = _baseUrl;
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("Accept-Encoding", "gzip,deflate");
            request.AddHeader("Content-Type", "application/xml");

            client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            var response = client.Execute<List<T>>(request);

            if ((int)response.StatusCode > 300) {
                ApiAccessException exception = new ApiAccessException(response.StatusDescription);
                exception.StatusCode = response.StatusCode;
                exception.StatusDescription = response.StatusDescription;
                exception.RequestUrl = response.ResponseUri.AbsoluteUri;

                throw exception;
            }

            return response;
        }
        #endregion Private Methods

        
    }
}
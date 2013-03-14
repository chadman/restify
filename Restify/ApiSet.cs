using System;
using System.Collections.Generic;
using Restify.Exceptions;
using Restify.Extensions;
using RestSharp;
using RestSharp.Authenticators;

namespace Restify {
    public abstract class ApiSet<T> where T : new() {
        #region Properties
        private readonly OAuthTicket _ticket;
        private readonly string _baseUrl;

        /// <summary>
        /// The url for retrieving a specific entity. Call Get(string id) to use this property
        /// EX: /Customers/{0}
        /// </summary>
        protected virtual string GetUrl { get { throw new NotImplementedException("The property GetUrl has no value on the ApiSet."); } }

        /// <summary>
        /// The url for retrieving all the elements when the entity is a child of another entity. Call List(string parentID) to use this property
        /// EX: /Customers/{0}/Orders
        /// </summary>
        protected virtual string GetChildListUrl { get { throw new NotImplementedException("The property GetChildListUrl has no value on the ApiSet."); } }

        /// <summary>
        /// The url for retrieving the entity when it is a child of another entity. Call Get(string parentID, string id)
        /// EX: /Customers/{0}/Orders{1}
        /// </summary>
        protected virtual string GetChildUrl { get { throw new NotImplementedException("The property GetChildUrl has no value on the ApiSet."); } }

        /// <summary>
        /// The url used for creating a new entity
        /// EX: /Customers
        /// </summary>
        protected virtual string CreateUrl { get { throw new NotImplementedException("The property CreateUrl has no value on the ApiSet."); } }

        /// <summary>
        /// The url used for updating an existing entity
        /// EX: /Customers/{0}
        /// </summary>
        protected virtual string EditUrl { get { throw new NotImplementedException("The property EditUrl has no value on the ApiSet."); } }

        /// <summary>
        /// The url for retrieving a lit of entities. Call List() to user this property
        /// EX: /Customer/
        /// </summary>
        protected virtual string ListUrl { get { throw new NotImplementedException("The property ListUrl has no value on the ApiSet."); } }

        /// <summary>
        /// The url for searching for a list of entities
        /// </summary>
        protected virtual string SearchUrl { get { throw new NotImplementedException("The property SearchUrl has no value on the ApiSet."); } }

        #endregion Properties

        #region Constructor
        protected ApiSet(OAuthTicket ticket, string baseUrl) {
            _ticket = ticket;
            _baseUrl = baseUrl;
        }
        #endregion Constructor

        #region Actions
        public virtual List<T> List() {
            if (string.IsNullOrWhiteSpace(ListUrl)) {
                throw new NotImplementedException("The property ListUrl has no value on the ApiSet.");
            }

            string test = string.Empty;
            test = "this is test";

            var request = new RestRequest(Method.GET) {
                Resource = ListUrl
            };
            var item = ExecuteListRequest(request);

            string anotherstring = string.Empty;

            return item.Data;
        }

        public virtual List<T> List(string parentID) {
            if (string.IsNullOrWhiteSpace(GetChildListUrl)) {
                throw new NotImplementedException("The property GetChildListUrl has no value on the ApiSet.");
            }

            var request = new RestRequest(Method.GET) {
                Resource = string.Format(GetChildListUrl, parentID)
            };
            var item = ExecuteListRequest(request);

            return item.Data;
        }

        public virtual T Get(string id) {
            if (string.IsNullOrWhiteSpace(GetUrl)) {
                throw new NotImplementedException("The property GetUrl has no value on the ApiSet.");
            }

            var request = new RestRequest(Method.GET) {
                Resource = string.Format(GetUrl, id)
            };
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
            if (string.IsNullOrWhiteSpace(GetChildUrl)) {
                throw new NotImplementedException("The property GetChildUrl has no value on the ApiSet.");
            }

            var request = new RestRequest(Method.GET) {
                Resource = string.Format(GetChildUrl, parentID, id)
            };
            var item = ExecuteRequest(request);

            return item.Data;
        }

        public virtual T GetByUrl(string url) {
            var request = new RestRequest(Method.GET) {
                Resource = url.Substring(_baseUrl.Length)
            };
            var item = ExecuteRequest(request);

            return item.Data;
        }

        public virtual S Search<S>(QueryObject qo) where S : new() {
            if (string.IsNullOrWhiteSpace(SearchUrl)) {
                throw new NotImplementedException("The property SearchUrl has no value on the ApiSet.");
            }

            var request = new RestRequest(Method.GET) {
                Resource = SearchUrl
            };

            foreach (var pair in qo.ToDictionary()) {
                request.AddParameter(pair.Key, pair.Value);
            }

            var list = ExecuteCustomRequest<S>(request);
            return list.Data;
        }

        public virtual bool Create(byte[] stream, string url = "") {
            var targetUrl = string.Empty;
            
            if (!string.IsNullOrWhiteSpace(url)) {
                if (url.Trim().Length <= _baseUrl.Length) {
                    throw new Exception("Invalid url: " + url);
                }
                targetUrl = url.Substring(_baseUrl.Length);
            }

            if (string.IsNullOrWhiteSpace(targetUrl)) {
                if (string.IsNullOrWhiteSpace(CreateUrl)) {
                    throw new NotImplementedException("The property CreateUrl has no value on the ApiSet.");
                }

                targetUrl = CreateUrl;
            }

            var request = new RestRequest(Method.POST) {
                Resource = targetUrl
            };
            request.AddFile("stream", stream, string.Empty);

            var item = ExecuteRequest(request);
            return (int)item.StatusCode < 300;
        }

        public virtual T Create(T entity, string url = "") {
            var targetUrl = string.Empty;

            if (!string.IsNullOrWhiteSpace(url)) {
                if (url.Trim().Length <= _baseUrl.Length) {
                    throw new Exception("Invalid url: " + url);
                }
                targetUrl = url.Substring(_baseUrl.Length);
            }

            if (string.IsNullOrWhiteSpace(targetUrl)) {
                if (string.IsNullOrWhiteSpace(CreateUrl)) {
                    throw new NotImplementedException("The property CreateUrl has no value on the ApiSet.");
                }

                targetUrl = CreateUrl;
            }

            var request = new RestRequest(Method.POST) {
                Timeout = 20000,
                Resource = targetUrl
            };
            request.AddParameter("application/xml", entity.ToXml(), ParameterType.RequestBody);

            var item = ExecuteRequest(request);
            return item.Data;
        }

        public virtual T Create(T entity, out string requestXml, string url = "") {
            var targetUrl = string.Empty;

            if (!string.IsNullOrWhiteSpace(url)) {
                if (url.Trim().Length <= _baseUrl.Length) {
                    throw new Exception("Invalid url: " + url);
                }
                targetUrl = url.Substring(_baseUrl.Length);
            }

            if (string.IsNullOrWhiteSpace(targetUrl)) {
                if (string.IsNullOrWhiteSpace(CreateUrl)) {
                    throw new NotImplementedException("The property CreateUrl has no value on the ApiSet.");
                }

                targetUrl = CreateUrl;
            }

            requestXml = entity.ToXml();
            var request = new RestRequest(Method.POST) {
                Timeout = 20000,
                Resource = targetUrl
            };
            request.AddParameter("application/xml", entity.ToXml(), ParameterType.RequestBody);

            var item = ExecuteRequest(request);
            return item.Data;
        }

        public virtual bool Update(byte[] stream, string id) {
            if (string.IsNullOrWhiteSpace(EditUrl)) {
                throw new NotImplementedException("The property EditUrl has no value on the ApiSet.");
            }

            var request = new RestRequest(Method.PUT) {
                Resource = string.Format(EditUrl, id)
            };
            request.AddFile("stream", stream, string.Empty);

            var item = ExecuteRequest(request);
            return (int)item.StatusCode < 300;
        }

        public virtual T Update(T entity, string id) {
            if (string.IsNullOrWhiteSpace(EditUrl)) {
                throw new NotImplementedException("The property EditUrl has no value on the ApiSet.");
            }

            var request = new RestRequest(Method.PUT) {
                Resource = string.Format(EditUrl, id)
            };
            request.AddParameter("application/xml", entity.ToXml(), ParameterType.RequestBody);

            var item = ExecuteRequest(request);
            return item.Data;
        }

        public virtual T Update(T entity, string id, out string requestXml) {
            if (string.IsNullOrWhiteSpace(EditUrl)) {
                throw new NotImplementedException("The property EditUrl has no value on the ApiSet.");
            }

            requestXml = entity.ToXml();
            var request = new RestRequest(Method.PUT) {
                Resource = string.Format(EditUrl, id)
            };
            request.AddParameter("application/xml", entity.ToXml(), ParameterType.RequestBody);

            var item = ExecuteRequest(request);
            return item.Data;
        }

        public virtual bool Delete(string id) {
            if (string.IsNullOrWhiteSpace(EditUrl)) {
                throw new NotImplementedException("The property EditUrl has no value on the ApiSet.");
            }

            var request = new RestRequest(Method.DELETE) {
                Resource = string.Format(EditUrl, id)
            };
            var item = ExecuteRequest(request);
            return (int)item.StatusCode < 300;
        }
        #endregion Actions

        #region Private Methods
        private IRestResponse<T> ExecuteRequest(IRestRequest request) {
            var client = new RestClient {
                BaseUrl = _baseUrl
            };
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("Accept-Encoding", "gzip,deflate");
            request.AddHeader("Content-Type", "application/xml");

            client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            var response = client.Execute<T>(request);

            if ((int)response.StatusCode > 300) {
                throw new ApiAccessException(response.StatusDescription) {
                    StatusCode = response.StatusCode, 
                    StatusDescription = response.StatusDescription, 
                    RequestUrl = response.ResponseUri.AbsoluteUri
                };
            }

            if (!string.IsNullOrEmpty(response.ErrorMessage)) {
                throw new ApiAccessException(response.ErrorMessage);
            }

            return response;
        }

        private IRestResponse<S> ExecuteCustomRequest<S>(IRestRequest request) where S : new() {
            var client = new RestClient {
                BaseUrl = _baseUrl
            };
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("Accept-Encoding", "gzip,deflate");
            request.AddHeader("Content-Type", "application/xml");

            client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            var response = client.Execute<S>(request);

            if ((int)response.StatusCode > 300) {
                throw new ApiAccessException(response.StatusDescription) {
                    StatusCode = response.StatusCode, 
                    StatusDescription = response.StatusDescription, 
                    RequestUrl = response.ResponseUri.AbsoluteUri
                };
            }

            return response;
        }

        private IRestResponse<List<T>> ExecuteListRequest(IRestRequest request) {
            var client = new RestClient {
                BaseUrl = _baseUrl
            };
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("Accept-Encoding", "gzip,deflate");
            request.AddHeader("Content-Type", "application/xml");

            client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            var response = client.Execute<List<T>>(request);

            if ((int)response.StatusCode > 300) {
                throw new ApiAccessException(response.StatusDescription) {
                    StatusCode = response.StatusCode, 
                    StatusDescription = response.StatusDescription, 
                    RequestUrl = response.ResponseUri.AbsoluteUri
                };
            }

            return response;
        }
        #endregion Private Methods 
    }
}
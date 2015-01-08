using System;
using System.Collections.Generic;
using Restify.Exceptions;
using Restify.Extensions;
using RestSharp;
using RestSharp.Authenticators;
using System.IO;

namespace Restify {
    public abstract class ApiSet<T> where T : new() {
        #region Properties
        private readonly OAuthTicket _ticket;
        private readonly string _baseUrl;
        private readonly ContentType _contentType;
        private readonly IDictionary<string, string> _requestHeaders;
        private IDictionary<string, string> _parameters = new Dictionary<string, string>();

        public string BaseUrl { get { return _baseUrl; } }

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
        protected ApiSet(OAuthTicket ticket, string baseUrl, ContentType contentType) {
            _ticket = ticket;
            _baseUrl = baseUrl;
            _contentType = contentType;
        }

        protected ApiSet(IDictionary<string, string> requestHeaders, string baseUrl, ContentType contentType) {
            _requestHeaders = requestHeaders;
            _baseUrl = baseUrl;
            _contentType = contentType;
        }

        protected ApiSet(string baseUrl, ContentType contentType) {
            _baseUrl = baseUrl;
            _contentType = contentType;
        }
        #endregion Constructor

        #region Actions
        public virtual List<T> List() {
            if (string.IsNullOrWhiteSpace(ListUrl)) {
                throw new NotImplementedException("The property ListUrl has no value on the ApiSet.");
            }

            var request = CreateRestRequest(Method.GET, ListUrl);
            var item = ExecuteListRequest(request);
            return item.Data;
        }

        public virtual List<T> List(string parentID) {
            if (string.IsNullOrWhiteSpace(GetChildListUrl)) {
                throw new NotImplementedException("The property GetChildListUrl has no value on the ApiSet.");
            }

            var request = CreateRestRequest(Method.GET, string.Format(GetChildListUrl, parentID));
            var item = ExecuteListRequest(request);

            return item.Data;
        }

        public List<S> ListBySuffixUrl<S>(string url) where S : new() {
            var request = CreateRestRequest(Method.GET, url);
            var item = ExecuteCustomRequest<List<S>>(request);

            return item.Data;
        }

        public virtual T Get(string id) {
            if (string.IsNullOrWhiteSpace(GetUrl)) {
                throw new NotImplementedException("The property GetUrl has no value on the ApiSet.");
            }
            var request = CreateRestRequest(Method.GET, string.Format(GetUrl, id));
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

            var request = CreateRestRequest(Method.GET, string.Format(GetChildUrl, parentID, id));
            var item = ExecuteRequest(request);

            return item.Data;
        }

        public virtual T GetByUrl(string url) {
            var request = CreateRestRequest(Method.GET, url.Substring(_baseUrl.Length));
            var item = ExecuteRequest(request);

            return item.Data;
        }

        public virtual S GetBySuffixUrl<S>(string url) where S : new() {
            var request = CreateRestRequest(Method.GET, url);
            var item = ExecuteCustomRequest<S>(request);

            return item.Data;
        }

        public virtual string GetBySuffixUrl(string url) {
            var request = CreateRestRequest(Method.GET, url);
            var item = ExecuteGenericRequest(request);

            return item.Content;
        }

        public virtual S Search<S>(QueryObject qo) where S : new() {
            if (string.IsNullOrWhiteSpace(SearchUrl)) {
                throw new NotImplementedException("The property SearchUrl has no value on the ApiSet.");
            }
            var request = CreateRestRequest(Method.GET, SearchUrl);

            foreach (var pair in qo.ToDictionary()) {
                request.AddParameter(pair.Key, pair.Value);
            }

            var list = ExecuteCustomRequest<S>(request);
            return list.Data;
        }

        public virtual IRestResponse Post(string url) {
            var request = CreateRestRequest(Method.POST, url);

            var client = new RestClient(_baseUrl);

            if (_ticket != null) {
                client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            }
            var response = client.Execute(request);

            return response;
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

        public virtual T Create(byte[] stream, string url = "", string fileParamaterName = "stream", string fileName = "") {
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
            var request = CreateRestRequest(Method.POST, targetUrl);
            request.AddFile(fileParamaterName, stream, fileName);

            var response = this.ExecuteRequest(request);
            return response.Data;
        }

        public virtual bool Create<S>(S entity, string url = "") where S : new() {
            var targetUrl = string.Empty;

            if (!string.IsNullOrWhiteSpace(url)) {
                if (url.Trim().StartsWith(_baseUrl)) {
                    if (url.Trim().Length <= _baseUrl.Length) {
                        throw new Exception("Invalid url: " + url);
                    }
                    targetUrl = url.Substring(_baseUrl.Length);
                }
                else {
                    targetUrl = url;
                }
            }

            if (string.IsNullOrWhiteSpace(targetUrl)) {
                if (string.IsNullOrWhiteSpace(CreateUrl)) {
                    throw new NotImplementedException("The property CreateUrl has no value on the ApiSet.");
                }

                targetUrl = CreateUrl;
            }
            var request = CreateRestRequest(Method.POST, targetUrl);
            request.Timeout = 20000;

            if (_contentType == ContentType.XML) {
                request.AddParameter("application/xml", entity.ToXml(), ParameterType.RequestBody);
            }
            else if (_contentType == ContentType.JSON) {
                request.AddParameter("application/json", Newtonsoft.Json.JsonConvert.SerializeObject(entity), ParameterType.RequestBody);
            }
            var item = ExecuteRequest(request);
            return (int)item.StatusCode < 300;
        }

        public virtual T Create(T entity, string url = "") {
            var targetUrl = string.Empty;

            if (!string.IsNullOrWhiteSpace(url)) {
                if (url.Trim().StartsWith(_baseUrl)) {
                    if (url.Trim().Length <= _baseUrl.Length) {
                        throw new Exception("Invalid url: " + url);
                    }
                    targetUrl = url.Substring(_baseUrl.Length);
                }
                else {
                    targetUrl = url;
                }
            }

            if (string.IsNullOrWhiteSpace(targetUrl)) {
                if (string.IsNullOrWhiteSpace(CreateUrl)) {
                    throw new NotImplementedException("The property CreateUrl has no value on the ApiSet.");
                }

                targetUrl = CreateUrl;
            }
            var request = CreateRestRequest(Method.POST, targetUrl);
            request.Timeout = 20000;

            if (_contentType == ContentType.XML) {
                request.AddParameter("application/xml", entity.ToXml(), ParameterType.RequestBody);
            }
            else if (_contentType == ContentType.JSON) {
                request.AddParameter("application/json", Newtonsoft.Json.JsonConvert.SerializeObject(entity), ParameterType.RequestBody);
            }
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

            var request = CreateRestRequest(Method.POST, targetUrl);
            request.Timeout = 20000;

            requestXml = entity.ToXml();
            if (_contentType == ContentType.XML) {
                request.AddParameter("application/xml", entity.ToXml(), ParameterType.RequestBody);
            }
            else if (_contentType == ContentType.JSON) {
                request.AddParameter("application/json", Newtonsoft.Json.JsonConvert.SerializeObject(entity), ParameterType.RequestBody);
            }

            var item = ExecuteRequest(request);
            return item.Data;
        }

        public virtual bool Update(byte[] stream, string id) {
            if (string.IsNullOrWhiteSpace(EditUrl)) {
                throw new NotImplementedException("The property EditUrl has no value on the ApiSet.");
            }

            var request = CreateRestRequest(Method.PUT, string.Format(EditUrl, id));
            request.AddFile("stream", stream, string.Empty);

            var item = ExecuteRequest(request);
            return (int)item.StatusCode < 300;
        }

        public virtual T Update(T entity, string id) {
            if (string.IsNullOrWhiteSpace(EditUrl)) {
                throw new NotImplementedException("The property EditUrl has no value on the ApiSet.");
            }

            var request = CreateRestRequest(Method.PUT, string.Format(EditUrl, id));
            if (_contentType == ContentType.XML) {
                request.AddParameter("application/xml", entity.ToXml(), ParameterType.RequestBody);
            }
            else if (_contentType == ContentType.JSON) {
                request.AddParameter("application/json", Newtonsoft.Json.JsonConvert.SerializeObject(entity), ParameterType.RequestBody);
            }

            var item = ExecuteRequest(request);
            return item.Data;
        }

        public virtual T Update(T entity, string id, out string requestXml) {
            if (string.IsNullOrWhiteSpace(EditUrl)) {
                throw new NotImplementedException("The property EditUrl has no value on the ApiSet.");
            }

            requestXml = entity.ToXml();
            var request = CreateRestRequest(Method.PUT, string.Format(EditUrl, id));
            if (_contentType == ContentType.XML) {
                request.AddParameter("application/xml", entity.ToXml(), ParameterType.RequestBody);
            }
            else if (_contentType == ContentType.JSON) {
                request.AddParameter("application/json", Newtonsoft.Json.JsonConvert.SerializeObject(entity), ParameterType.RequestBody);
            }

            var item = ExecuteRequest(request);
            return item.Data;
        }

        public virtual bool Delete(string id) {
            if (string.IsNullOrWhiteSpace(EditUrl)) {
                throw new NotImplementedException("The property EditUrl has no value on the ApiSet.");
            }
            var request = CreateRestRequest(Method.DELETE, string.Format(EditUrl, id));
            var item = ExecuteRequest(request);
            return (int)item.StatusCode < 300;
        }

        public byte[] GetByteArray(IRestRequest request) {
            var client = new RestClient(_baseUrl);
            request.AddHeader("Accept-Encoding", "gzip,deflate");

            if (_ticket != null) {
                client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            }
            var response = client.Execute(request);

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

            return response.RawBytes;
        }

        public void AddParameter(string key, string value) {
            this._parameters.Add(new KeyValuePair<string, string>(key, value));
        }
        #endregion Actions

        #region Private Methods
        private IRestResponse<T> ExecuteRequest(IRestRequest request) {
            var client = new RestClient(_baseUrl);

            if (_ticket != null) {
                client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            }
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

        protected IRestResponse ExecuteGenericRequest(IRestRequest request) {
            var client = new RestClient(_baseUrl);

            if (_ticket != null) {
                client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            }
            var response = client.Execute(request);

            if ((int)response.StatusCode > 300) {
                throw new ApiAccessException(response.StatusDescription) {
                    StatusCode = response.StatusCode,
                    StatusDescription = response.StatusDescription,
                    RequestUrl = response.ResponseUri.AbsoluteUri
                };
            }

            return response;
        }

        protected IRestResponse<S> ExecuteCustomRequest<S>(IRestRequest request) where S : new() {
            var client = new RestClient(_baseUrl);

            if (_ticket != null) {
                client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            }
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
            var client = new RestClient(_baseUrl);

            if (_ticket != null) {
                client.Authenticator = OAuth1Authenticator.ForProtectedResource(_ticket.ConsumerKey, _ticket.ConsumerSecret, _ticket.AccessToken, _ticket.AccessTokenSecret);
            }
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

        public RestRequest CreateRestRequest(Method method, string url, string contentType = null) {

            var request = new RestRequest(method) {
                Resource = url
            };
            request.RequestFormat = _contentType == ContentType.JSON ? DataFormat.Json : DataFormat.Xml;
            request.AddHeader("Accept-Encoding", "gzip,deflate");
            request.AddHeader("Content-Type", !string.IsNullOrEmpty(contentType) ? contentType : _contentType == ContentType.XML ? "application/xml" : "application/json");

            if (_requestHeaders != null && _requestHeaders.Count > 0) {
                foreach (var current in _requestHeaders) {
                    request.AddHeader(current.Key, current.Value);
                }
            }

            if (_parameters != null && _parameters.Count > 0) {
                foreach (var current in _parameters) {
                    request.AddParameter(current.Key, current.Value);
                }
            }

            return request;
        }

        //method for converting stream to byte[]
        private byte[] ReadToEnd(System.IO.Stream stream) {
            long originalPosition = stream.Position;
            stream.Position = 0;

            try {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0) {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length) {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1) {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead) {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally {
                stream.Position = originalPosition;
            }
        }

        private readonly Dictionary<string, string> MIMETypesDictionary = new Dictionary<string, string>
  {
    {"ai", "application/postscript"},
    {"aif", "audio/x-aiff"},
    {"aifc", "audio/x-aiff"},
    {"aiff", "audio/x-aiff"},
    {"asc", "text/plain"},
    {"atom", "application/atom+xml"},
    {"au", "audio/basic"},
    {"avi", "video/x-msvideo"},
    {"bcpio", "application/x-bcpio"},
    {"bin", "application/octet-stream"},
    {"bmp", "image/bmp"},
    {"cdf", "application/x-netcdf"},
    {"cgm", "image/cgm"},
    {"class", "application/octet-stream"},
    {"cpio", "application/x-cpio"},
    {"cpt", "application/mac-compactpro"},
    {"csh", "application/x-csh"},
    {"css", "text/css"},
    {"dcr", "application/x-director"},
    {"dif", "video/x-dv"},
    {"dir", "application/x-director"},
    {"djv", "image/vnd.djvu"},
    {"djvu", "image/vnd.djvu"},
    {"dll", "application/octet-stream"},
    {"dmg", "application/octet-stream"},
    {"dms", "application/octet-stream"},
    {"doc", "application/msword"},
    {"docx","application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
    {"dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template"},
    {"docm","application/vnd.ms-word.document.macroEnabled.12"},
    {"dotm","application/vnd.ms-word.template.macroEnabled.12"},
    {"dtd", "application/xml-dtd"},
    {"dv", "video/x-dv"},
    {"dvi", "application/x-dvi"},
    {"dxr", "application/x-director"},
    {"eps", "application/postscript"},
    {"etx", "text/x-setext"},
    {"exe", "application/octet-stream"},
    {"ez", "application/andrew-inset"},
    {"gif", "image/gif"},
    {"gram", "application/srgs"},
    {"grxml", "application/srgs+xml"},
    {"gtar", "application/x-gtar"},
    {"hdf", "application/x-hdf"},
    {"hqx", "application/mac-binhex40"},
    {"htm", "text/html"},
    {"html", "text/html"},
    {"ice", "x-conference/x-cooltalk"},
    {"ico", "image/x-icon"},
    {"ics", "text/calendar"},
    {"ief", "image/ief"},
    {"ifb", "text/calendar"},
    {"iges", "model/iges"},
    {"igs", "model/iges"},
    {"jnlp", "application/x-java-jnlp-file"},
    {"jp2", "image/jp2"},
    {"jpe", "image/jpeg"},
    {"jpeg", "image/jpeg"},
    {"jpg", "image/jpeg"},
    {"js", "application/x-javascript"},
    {"kar", "audio/midi"},
    {"latex", "application/x-latex"},
    {"lha", "application/octet-stream"},
    {"lzh", "application/octet-stream"},
    {"m3u", "audio/x-mpegurl"},
    {"m4a", "audio/mp4a-latm"},
    {"m4b", "audio/mp4a-latm"},
    {"m4p", "audio/mp4a-latm"},
    {"m4u", "video/vnd.mpegurl"},
    {"m4v", "video/x-m4v"},
    {"mac", "image/x-macpaint"},
    {"man", "application/x-troff-man"},
    {"mathml", "application/mathml+xml"},
    {"me", "application/x-troff-me"},
    {"mesh", "model/mesh"},
    {"mid", "audio/midi"},
    {"midi", "audio/midi"},
    {"mif", "application/vnd.mif"},
    {"mov", "video/quicktime"},
    {"movie", "video/x-sgi-movie"},
    {"mp2", "audio/mpeg"},
    {"mp3", "audio/mpeg"},
    {"mp4", "video/mp4"},
    {"mpe", "video/mpeg"},
    {"mpeg", "video/mpeg"},
    {"mpg", "video/mpeg"},
    {"mpga", "audio/mpeg"},
    {"ms", "application/x-troff-ms"},
    {"msh", "model/mesh"},
    {"mxu", "video/vnd.mpegurl"},
    {"nc", "application/x-netcdf"},
    {"oda", "application/oda"},
    {"ogg", "application/ogg"},
    {"pbm", "image/x-portable-bitmap"},
    {"pct", "image/pict"},
    {"pdb", "chemical/x-pdb"},
    {"pdf", "application/pdf"},
    {"pgm", "image/x-portable-graymap"},
    {"pgn", "application/x-chess-pgn"},
    {"pic", "image/pict"},
    {"pict", "image/pict"},
    {"png", "image/png"}, 
    {"pnm", "image/x-portable-anymap"},
    {"pnt", "image/x-macpaint"},
    {"pntg", "image/x-macpaint"},
    {"ppm", "image/x-portable-pixmap"},
    {"ppt", "application/vnd.ms-powerpoint"},
    {"pptx","application/vnd.openxmlformats-officedocument.presentationml.presentation"},
    {"potx","application/vnd.openxmlformats-officedocument.presentationml.template"},
    {"ppsx","application/vnd.openxmlformats-officedocument.presentationml.slideshow"},
    {"ppam","application/vnd.ms-powerpoint.addin.macroEnabled.12"},
    {"pptm","application/vnd.ms-powerpoint.presentation.macroEnabled.12"},
    {"potm","application/vnd.ms-powerpoint.template.macroEnabled.12"},
    {"ppsm","application/vnd.ms-powerpoint.slideshow.macroEnabled.12"},
    {"ps", "application/postscript"},
    {"qt", "video/quicktime"},
    {"qti", "image/x-quicktime"},
    {"qtif", "image/x-quicktime"},
    {"ra", "audio/x-pn-realaudio"},
    {"ram", "audio/x-pn-realaudio"},
    {"ras", "image/x-cmu-raster"},
    {"rdf", "application/rdf+xml"},
    {"rgb", "image/x-rgb"},
    {"rm", "application/vnd.rn-realmedia"},
    {"roff", "application/x-troff"},
    {"rtf", "text/rtf"},
    {"rtx", "text/richtext"},
    {"sgm", "text/sgml"},
    {"sgml", "text/sgml"},
    {"sh", "application/x-sh"},
    {"shar", "application/x-shar"},
    {"silo", "model/mesh"},
    {"sit", "application/x-stuffit"},
    {"skd", "application/x-koan"},
    {"skm", "application/x-koan"},
    {"skp", "application/x-koan"},
    {"skt", "application/x-koan"},
    {"smi", "application/smil"},
    {"smil", "application/smil"},
    {"snd", "audio/basic"},
    {"so", "application/octet-stream"},
    {"spl", "application/x-futuresplash"},
    {"src", "application/x-wais-source"},
    {"sv4cpio", "application/x-sv4cpio"},
    {"sv4crc", "application/x-sv4crc"},
    {"svg", "image/svg+xml"},
    {"swf", "application/x-shockwave-flash"},
    {"t", "application/x-troff"},
    {"tar", "application/x-tar"},
    {"tcl", "application/x-tcl"},
    {"tex", "application/x-tex"},
    {"texi", "application/x-texinfo"},
    {"texinfo", "application/x-texinfo"},
    {"tif", "image/tiff"},
    {"tiff", "image/tiff"},
    {"tr", "application/x-troff"},
    {"tsv", "text/tab-separated-values"},
    {"txt", "text/plain"},
    {"ustar", "application/x-ustar"},
    {"vcd", "application/x-cdlink"},
    {"vrml", "model/vrml"},
    {"vxml", "application/voicexml+xml"},
    {"wav", "audio/x-wav"},
    {"wbmp", "image/vnd.wap.wbmp"},
    {"wbmxl", "application/vnd.wap.wbxml"},
    {"wml", "text/vnd.wap.wml"},
    {"wmlc", "application/vnd.wap.wmlc"},
    {"wmls", "text/vnd.wap.wmlscript"},
    {"wmlsc", "application/vnd.wap.wmlscriptc"},
    {"wrl", "model/vrml"},
    {"xbm", "image/x-xbitmap"},
    {"xht", "application/xhtml+xml"},
    {"xhtml", "application/xhtml+xml"},
    {"xls", "application/vnd.ms-excel"},                        
    {"xml", "application/xml"},
    {"xpm", "image/x-xpixmap"},
    {"xsl", "application/xml"},
    {"xlsx","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
    {"xltx","application/vnd.openxmlformats-officedocument.spreadsheetml.template"},
    {"xlsm","application/vnd.ms-excel.sheet.macroEnabled.12"},
    {"xltm","application/vnd.ms-excel.template.macroEnabled.12"},
    {"xlam","application/vnd.ms-excel.addin.macroEnabled.12"},
    {"xlsb","application/vnd.ms-excel.sheet.binary.macroEnabled.12"},
    {"xslt", "application/xslt+xml"},
    {"xul", "application/vnd.mozilla.xul+xml"},
    {"xwd", "image/x-xwindowdump"},
    {"xyz", "chemical/x-xyz"},
    {"zip", "application/zip"}
  };

        private string GetMIMEType(string fileName) {
            //get file extension
            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (extension.Length > 0 &&
                MIMETypesDictionary.ContainsKey(extension.Remove(0, 1))) {
                return MIMETypesDictionary[extension.Remove(0, 1)];
            }
            return "unknown/unknown";
        }
    }
        #endregion Private Methods
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace FBF.Yahoo
{
    public class OAuth
    {
        public class ConsumerInfo
        {
            public ConsumerInfo(string consumerKey, string consumerSecret)
            {
                ConsumerKey = consumerKey;
                ConsumerSecret = consumerSecret;
            }

            private string _consumerKey;
            public string ConsumerKey
            {
                get { return _consumerKey; }
                set
                {
                    if(String.IsNullOrWhiteSpace(value))
                        throw new ArgumentException();
                    _consumerKey = value;
                }
            }

            private string _consumerSecret;
            public string ConsumerSecret
            {
                get { return _consumerSecret; }
                set
                {
                    if(String.IsNullOrWhiteSpace(value))
                        throw new ArgumentException();
                    _consumerSecret = value;
                }
            }
        }

        public class AccessAndSessionInfo
        {
            public AccessAndSessionInfo(string accessToken, DateTime accessTokenExpiration, string accessTokenSecret, string sessionHandle, DateTime sessionExpiration)
            {
                AccessToken = accessToken;
                AccessTokenExipration = accessTokenExpiration;
                AccessTokenSecret = accessTokenSecret;
                SessionHandle = sessionHandle;
                SessionExpiration = sessionExpiration;
            }

            private string _accessToken;
            public string AccessToken
            {
                get { return _accessToken; }
                set
                {
                    if(String.IsNullOrWhiteSpace(value))
                        throw new ArgumentException();
                    _accessToken = value;
                }
            }

            private DateTime _accessTokenExpiration;
            public DateTime AccessTokenExipration
            {
                get { return _accessTokenExpiration; }
                set
                {
                    if(_accessTokenExpiration == null)
                        throw new ArgumentException();
                    _accessTokenExpiration = value;
                }
            }

            private string _accessTokenSecret;
            public string AccessTokenSecret
            {
                get { return _accessTokenSecret; }
                set
                {
                    if(String.IsNullOrWhiteSpace(value))
                        throw new ArgumentException();
                    _accessTokenSecret = value;
                }
            }

            private string _sessionHandle;
            public string SessionHandle
            {
                get { return _sessionHandle; }
                set
                {
                    if(String.IsNullOrWhiteSpace(value))
                        throw new ArgumentException();
                    _sessionHandle = value;
                }
            }

            private DateTime _sessionExpiration;
            public DateTime SessionExpiration
            {
                get { return _sessionExpiration; }
                set
                {
                    if(_sessionExpiration == null)
                        throw new ArgumentException();
                    _sessionExpiration = value;
                }
            }
        }

        //oauth flow
        //https://developer.yahoo.com/oauth/guide/oauth-auth-flow.html

        public static string GETREQUESTTOKENURIFORMAT = "https://api.login.yahoo.com/oauth/v2/get_request_token?" +
            "oauth_nonce={0}&" +
            "oauth_timestamp={1}&" +
            "oauth_consumer_key={2}&" +
            "oauth_version=1.0&" +
            "xoauth_lang_pref=en-us&" +
            "oauth_callback=oob&" + 
            "oauth_signature_method=HMAC-SHA1";
        public static string GETACCESSTOKENURIFORMAT = "https://api.login.yahoo.com/oauth/v2/get_token?" +
            "oauth_nonce={0}&" +
            "oauth_timestamp={1}&" +
            "oauth_consumer_key={2}&" +
            "oauth_version=1.0&" +
            "oauth_verifier={3}&" +
            "oauth_token={4}&" +
            "oauth_signature_method=HMAC-SHA1";
        public static string OAUTHQUERYSTRINGFORMAT = "oauth_consumer_key={0}&" +
                "oauth_nonce={1}&" + 
                "oauth_timestamp={2}&" +
                "oauth_token={3}&" + 
                "oauth_version=1.0&" +
                "oauth_signature_method=HMAC-SHA1";

        public delegate Task<string> RequestAuthorizationCallback(string authorizationUri);

        private static Regex QUERYSTRINGREGEX = new Regex("((?:\\?|&)(?<param>[^=]+)=(?<value>[^&]+))");

        private readonly ConsumerInfo _consumerInfo;
        private readonly HttpClient _httpClient;

        //private AccessAndSessionInfo _accessAndSessionInfo;
        private RequestAuthorizationCallback _authCallback;
        //private string _requestToken;
        
        public OAuth(HttpClient client, ConsumerInfo consumerInfo, RequestAuthorizationCallback callback)
        {
            if (client == null)
                throw new ArgumentException("client");
            if (consumerInfo == null)
                throw new ArgumentException("consumerInfo");
            if (callback == null)
                throw new ArgumentException("callback");

            _consumerInfo = consumerInfo;
            _httpClient = client;
            _authCallback = callback;
        }

        public OAuth(HttpClient client, ConsumerInfo consumerInfo, AccessAndSessionInfo accessAndSessionInfo)
        {
            if (client == null)
                throw new ArgumentException("client");
            if (consumerInfo == null)
                throw new ArgumentException("consumerInfo");
            if (accessAndSessionInfo == null)
                throw new ArgumentException("accessAndSessionInfo");

            _consumerInfo = consumerInfo;
            AccessInfo = accessAndSessionInfo;
        }

        public AccessAndSessionInfo AccessInfo
        {
            get;
            private set;
        }

        //public bool HasAccess
        //{
        //    get { return AccessInfo != null; }
        //}    

        public async Task<AccessAndSessionInfo> GetAccessAsync()
        {
            if (AccessInfo != null)
            {
                throw new InvalidOperationException("Access already established");
            }
            
            //get a request token and auth uri
            Uri requestTokenUriWithoutSignature = new Uri(String.Format(GETREQUESTTOKENURIFORMAT, CreateNonce(), ToUnixTime(DateTime.Now), _consumerInfo.ConsumerKey));
            Uri requestTokenUriWithSignature = new Uri(String.Format("{0}&oauth_signature={1}", requestTokenUriWithoutSignature, GetSignature(requestTokenUriWithoutSignature, HttpMethod.Get, String.Empty)));
            HttpResponseMessage requestTokenMessage = await _httpClient.GetAsync(requestTokenUriWithSignature);
            if (!requestTokenMessage.IsSuccessStatusCode)
                ConstructAndThrowRequestFailedException(requestTokenMessage);

            //get authoriztion from user
            string getRequestTokenReturnedContent = await requestTokenMessage.Content.ReadAsStringAsync();
            SortedDictionary<string, string> requestTokenDict = QueryStringToSortedDictionary(getRequestTokenReturnedContent);
            string authUri = WebUtility.UrlDecode(requestTokenDict["xoauth_request_auth_url"]);
            string verifierCode = await _authCallback.Invoke(authUri);
            if (String.IsNullOrWhiteSpace(verifierCode))
                ConstructAndThrowRequestFailedException();

            //get access and session info
            string token = requestTokenDict["oauth_token"];
            string tokenSecret = requestTokenDict["oauth_token_secret"];
            Uri accessTokenUriWithoutSignature = new Uri(String.Format(GETACCESSTOKENURIFORMAT, CreateNonce(), ToUnixTime(DateTime.Now), _consumerInfo.ConsumerKey, verifierCode, token));
            Uri accessTokenUriWithSignature = new Uri(String.Format("{0}&oauth_signature={1}", accessTokenUriWithoutSignature, GetSignature(accessTokenUriWithoutSignature, HttpMethod.Get, tokenSecret)));
            HttpResponseMessage getAccessMessage = await _httpClient.GetAsync(accessTokenUriWithSignature);
            if (!getAccessMessage.IsSuccessStatusCode)
                ConstructAndThrowRequestFailedException(getAccessMessage);
            string getAccessTokenReturnedContent = await getAccessMessage.Content.ReadAsStringAsync();
            SortedDictionary<string, string> accessTokenDict = QueryStringToSortedDictionary(getAccessTokenReturnedContent);

            string accessToken = WebUtility.UrlDecode(accessTokenDict["oauth_token"]);
            string accessTokenSecret = WebUtility.UrlDecode(accessTokenDict["oauth_token_secret"]);
            DateTime accessTokenExpires = DateTime.Now.AddSeconds(Int32.Parse(accessTokenDict["oauth_expires_in"]));
            string sessionHandle = WebUtility.UrlDecode(accessTokenDict["oauth_session_handle"]);
            DateTime sessionHandleExpires = DateTime.Now.AddSeconds(Int32.Parse(accessTokenDict["oauth_authorization_expires_in"]));
            OAuth.AccessAndSessionInfo accessAndSession = new AccessAndSessionInfo(accessToken, accessTokenExpires, accessTokenSecret, sessionHandle, sessionHandleExpires);

            AccessInfo = accessAndSession;

            return accessAndSession;
        }

        public Uri SignUri(string uri, HttpMethod method)
        {
            Uri originalUri = new Uri(uri);
            string query = originalUri.Query;
            if(!String.IsNullOrEmpty(query))
                query += "&";
            query += String.Format(OAUTHQUERYSTRINGFORMAT, _consumerInfo.ConsumerKey, CreateNonce(), ToUnixTime(DateTime.Now), AccessInfo.AccessToken);
            string uriWithoutSignature = String.Format("{0}://{1}{2}?{3}", originalUri.Scheme, originalUri.Host, originalUri.AbsolutePath, query);
            string uriWithSignature = String.Format("{0}&oauth_signature={1}", uriWithoutSignature, GetSignature(new Uri(uriWithoutSignature), method, AccessInfo.AccessTokenSecret));

            return new Uri(uriWithSignature);
        }

        private void ConstructAndThrowRequestFailedException(HttpResponseMessage response = null)
        {
            if (response == null)
            {
                throw new RequestFailedException();
            }
            else
            {
                throw new RequestFailedException(String.Format("HTTP error. Request Message - '{0}', Content - {1}, Status Code - {2}", response.RequestMessage, response.Content != null ? response.Content.ReadAsStringAsync().Result : "", response.StatusCode), response);
            }
        }

        //https://developer.yahoo.com/oauth/guide/oauth-signing.html
        internal string GetSignature(Uri uri, HttpMethod method, string tokenSecret)
        {
            bool hasQuery = !String.IsNullOrEmpty(uri.Query);
            string normalized = null;

            //normalize our request
            string uriBaseEncoded = WebUtility.UrlEncode(String.Format("{0}://{1}{2}", uri.Scheme, uri.Host, uri.AbsolutePath));
            if (hasQuery)
            {
                SortedDictionary<string, string> queryParams = QueryStringToSortedDictionary(uri.Query);
                StringBuilder newQueryParams = new StringBuilder();
                foreach (string key in queryParams.Keys)
                {
                    newQueryParams.Append(String.Format("{0}={1}&", key, queryParams[key]));
                }
                newQueryParams = newQueryParams.Remove(newQueryParams.Length - 1, 1);

                normalized = String.Format("{0}&{1}&{2}", method.Method.ToUpper(), uriBaseEncoded, WebUtility.UrlEncode(newQueryParams.ToString()));
            }
            else
            {
                normalized = String.Format("{0}&{1}", method.Method.ToUpper(), uriBaseEncoded);
            }

            //yahoo wants us to sign all calls with CONSUMER_SECRET + & + tokenSecret
            //for get_request_token, there is no tokenSecret so signingSecret will just be CONSUMER_SECRET + &
            string signingSecret = String.Format("{0}&{1}", _consumerInfo.ConsumerSecret, tokenSecret);

            IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(normalized, BinaryStringEncoding.Utf8);
            MacAlgorithmProvider provider = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);
            IBuffer signingSecretBuffer = CryptographicBuffer.ConvertStringToBinary(signingSecret, BinaryStringEncoding.Utf8);
            CryptographicKey hmacKey = provider.CreateKey(signingSecretBuffer);
            IBuffer buffHMAC = CryptographicEngine.Sign(hmacKey, buffer);
            
            return CryptographicBuffer.EncodeToBase64String(buffHMAC);
        }

        private SortedDictionary<string, string> QueryStringToSortedDictionary(string queryString)
        {
            if (!queryString.StartsWith("?"))
                queryString = queryString.Insert(0, "?");
        
            SortedDictionary<string, string> queryParams = new SortedDictionary<string, string>();
            foreach (Match m in QUERYSTRINGREGEX.Matches(queryString))
            {
                queryParams.Add(m.Groups["param"].Value, m.Groups["value"].Value);
            }
            return queryParams;
        }

        private string CreateNonce()
        {
            return Guid.NewGuid().ToString().Replace("-", "").ToLower().Substring(0, 8);
        }

        private long ToUnixTime(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date.ToUniversalTime() - epoch).TotalSeconds);
        }
    }
}

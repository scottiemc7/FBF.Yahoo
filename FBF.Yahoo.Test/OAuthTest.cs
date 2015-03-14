using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Net.Http;
using System.Net;
using FBF.Yahoo.Extensions;
using FBF.Yahoo.Test.TestHelpers;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace FBF.Yahoo.Test
{
    [TestClass]
    public class OAuthTest
    { 
        [TestMethod]
        public void ConstructorOneThrowsArgumentExceptionForInvalidArguments()
        {
            HttpClient http = new HttpClient();
            OAuth.ConsumerInfo consumerInfo = CreateFakeConsumerInfo();

            try
            {
                OAuth o = new OAuth(null, consumerInfo, (s) => { return Task.FromResult<string>(""); });
                Assert.Fail("HttpClient fail");
            }
            catch(ArgumentException) { }
            try
            {
                OAuth o = new OAuth(http, null, (s) => { return Task.FromResult<string>(""); });
                Assert.Fail("Consumer info fail");
            }
            catch (ArgumentException) { }
        }

        [TestMethod]
        public void ConstructorTwoThrowsArgumentExceptionForInvalidArguments()
        {
            HttpClient http = new HttpClient();
            OAuth.ConsumerInfo consumerInfo = CreateFakeConsumerInfo();
            OAuth.AccessAndSessionInfo accessInfo = CreateFakeAccessAndSessionInfo();

            try
            {
                OAuth o = new OAuth(null, consumerInfo, accessInfo);
                Assert.Fail("HttpClient fail");
            }
            catch (ArgumentException) { }
            try
            {
                OAuth o = new OAuth(http, null, accessInfo);
                Assert.Fail("Consumer info fail");
            }
            catch (ArgumentException) { }
            try
            {
                OAuth o = new OAuth(http, consumerInfo, accessAndSessionInfo:null);
                Assert.Fail("Access token info fail");
            }
            catch (ArgumentException) { }
        }

        [TestMethod]
        public void GetAccessAsyncThrowsInvalidOperationExceptionIfRequestTokenKnown()
        {
            HttpClient http = new HttpClient();
            OAuth.ConsumerInfo consumerInfo = CreateFakeConsumerInfo();
            OAuth.AccessAndSessionInfo accessInfo = CreateFakeAccessAndSessionInfo();
            OAuth o = new OAuth(http, consumerInfo, accessInfo);

            try
            {
                o.GetAccessAsync().Wait();
                Assert.Fail("Expecting GetAccessAsync to throw exception");
            }
            catch (AggregateException e) 
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(InvalidOperationException));
            }
        }

        [TestMethod]
        public void GetAccessAsyncFollowsOAuthFlow()
        {
            //gets request token
            //gets auth uri
            //gets acccess token
        }

        [TestMethod]
        public void GetAccessAsyncFailsIfGetRequestTokenFails()
        {
            FakeResponseHandler handler = CreateFakeResponseHandler(new ResponseMessages(true, true));
            HttpClient http = new HttpClient(handler);
            OAuth.ConsumerInfo consumerInfo = CreateFakeConsumerInfo();
            OAuth o = new OAuth(http, consumerInfo, (x) => { return Task.FromResult<string>(""); });

            try
            {
                o.GetAccessAsync().Wait();
                Assert.Fail("Expected exception to be thrown");
            }
            catch(AggregateException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(RequestFailedException));
            }
        }

        [TestMethod]
        public void GetAccessAsyncReturnsAuthUriIfGetRequestTokenSucceeds()
        {
            FakeResponseHandler handler = CreateFakeResponseHandler(new ResponseMessages(true, true));
            HttpClient http = new HttpClient(handler);
            OAuth.ConsumerInfo consumerInfo = CreateFakeConsumerInfo();
            OAuth.AccessAndSessionInfo accessInfo = CreateFakeAccessAndSessionInfo();
            OAuth o = new OAuth(http, consumerInfo, (x) => 
            {
                Assert.AreEqual("https://api.login.yahoo.com/oauth/v2/request_auth?oauth_token=z4ezdgj", x);
                return Task.FromResult<string>("fakeToken");
            });

            o.GetAccessAsync().Wait();
        }

        [TestMethod]
        public void GetAccessAsyncThrowsExceptionIfCallbackReturnsEmptyToken()
        {
            FakeResponseHandler handler = CreateFakeResponseHandler(new ResponseMessages(true, true));
            HttpClient http = new HttpClient(handler);
            OAuth.ConsumerInfo consumerInfo = CreateFakeConsumerInfo();
            OAuth o = new OAuth(http, consumerInfo, (x) => { return Task.FromResult<string>(""); });

            try
            {
                o.GetAccessAsync().Wait();
                Assert.Fail("Expected exception to be thrown");
            }
            catch(AggregateException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(RequestFailedException));
            }
        }

        [TestMethod]
        public void GetAccessAsyncThrowsExceptionIfGetAccessTokenFails()
        {
            FakeResponseHandler handler = CreateFakeResponseHandler(new ResponseMessages(true, false));
            HttpClient http = new HttpClient(handler);
            OAuth.ConsumerInfo consumerInfo = CreateFakeConsumerInfo();
            OAuth o = new OAuth(http, consumerInfo, (x) => { return Task.FromResult<string>("fakeToken"); });

            try
            {
                o.GetAccessAsync().Wait();
                Assert.Fail("Expected exception to be thrown");
            }
            catch (AggregateException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(RequestFailedException));
            }
        }

        [TestMethod]
        public void GetAccessAsyncReturnsAccessAndSessionInfo()
        {
            FakeResponseHandler handler = CreateFakeResponseHandler(new ResponseMessages(true, true));
            HttpClient http = new HttpClient(handler);
            OAuth.ConsumerInfo consumerInfo = CreateFakeConsumerInfo();
            OAuth o = new OAuth(http, consumerInfo, (x) => { return Task.FromResult<string>("fakeToken"); });

            Task<OAuth.AccessAndSessionInfo> task = o.GetAccessAsync();
            task.Wait();
            Assert.IsNotNull(task.Result);
        }

        [TestMethod]
        public void WrapUriWrapsUriWithOAuthInfo()
        {
            OAuth.ConsumerInfo consumerInfo = CreateFakeConsumerInfo();
            OAuth.AccessAndSessionInfo accessInfo = CreateFakeAccessAndSessionInfo();
            HttpClient http = new HttpClient();
            OAuth o = new OAuth(http, consumerInfo, accessInfo);
            string uri = "http://fantasysports.yahooapis.com/fantasy/v2/game/";

            Uri fakeUri = o.SignUri(uri, HttpMethod.Get);
            Dictionary<string, string> queryStringDict = QueryStringToDictionary(fakeUri.Query);

            Assert.IsFalse(String.IsNullOrWhiteSpace(fakeUri.Query));
            Assert.AreEqual(consumerInfo.ConsumerKey, queryStringDict["oauth_consumer_key"]);
            Assert.AreEqual(accessInfo.AccessToken, queryStringDict["oauth_token"]);
            Assert.AreEqual("HMAC-SHA1", queryStringDict["oauth_signature_method"]);
            Assert.IsTrue(queryStringDict.ContainsKey("oauth_signature"));
            Assert.IsTrue(queryStringDict.ContainsKey("oauth_nonce"));
            Assert.IsTrue(queryStringDict.ContainsKey("oauth_timestamp"));
        }

        [TestMethod]
        public void GenerateSignatureSignsCorrectly()
        {
            OAuth.ConsumerInfo consumerInfo = new OAuth.ConsumerInfo("dj0yJmk9NTFiSHVhT2J4NUpEJmQ9WVdrOWRsVmhUMFpDTldjbWNHbzlNQS0tJnM9Y29uc3VtZXJzZWNyZXQmeD0wMQ--", "514f08f6e70499474efe75f0f58d7515e26286d1");
            OAuth.AccessAndSessionInfo accessInfo = CreateFakeAccessAndSessionInfo();
            HttpClient http = new HttpClient();
            OAuth o = new OAuth(http, consumerInfo, accessInfo);
            HttpMethod knownGoodUriMethod = HttpMethod.Get;
            string knownGoodUnsignedUri = "https://api.login.yahoo.com/oauth/v2/get_request_token?oauth_callback=oob&oauth_consumer_key=dj0yJmk9NTFiSHVhT2J4NUpEJmQ9WVdrOWRsVmhUMFpDTldjbWNHbzlNQS0tJnM9Y29uc3VtZXJzZWNyZXQmeD0wMQ--&oauth_nonce=d3da85e3&oauth_signature_method=HMAC-SHA1&oauth_timestamp=1420403454&oauth_version=1.0&xoauth_lang_pref=en-us";
            string knownGoodSignatureEncoded = "J9cWFMNkW53t8hvT61%2BHQg1Z7Vw%3D";

            string generatedSignature = WebUtility.UrlEncode(o.GetSignature(new Uri(knownGoodUnsignedUri), knownGoodUriMethod, null));
            Assert.AreEqual(knownGoodSignatureEncoded, generatedSignature);
        }

    #region Helpers
        private class ResponseMessages
        {
            private const string DEFAULTGETREQUESTTOKENCONTENT = "oauth_token=z4ezdgj" + 
                "&oauth_token_secret=47ba47e0048b7f2105db67df18ffd24bd072688a&oauth_expires_in=3600" +
                "&xoauth_request_auth_url=https%3A%2F%2Fapi.login.yahoo.com%2Foauth%2Fv2%2Frequest_auth%3Foauth_token%3Dz4ezdgj" + 
                "&oauth_callback_confirmed=true";
            private const string DEFAULTGETACCESSTOKENCONTENT = "oauth_token=A%3DqVDHXBngo1tEtzox." +
                "JMhzd91Rk99.39Al7hos3J80mm1j_3nGP4BiilL777vUj2rsPLj1cZw.srbisvw.cz42Lzmlxt" +
                "H0Kk9mkXilvS1ll5lNoMKXO5zy5YG4vO3fbGKewp7IESYMIdEi4Md7SroYiv6kBCEjqB4jXr0.8XsMvOlQgZ.aKNKXwc2sv3n4BOZxs" +
                "54tzXV6rGNpEHZUaj9CovPUo44isTgs9FnLIKpXFCU4Jq1BB3_IOTFBNf1vtf5vSxaxe_L5dUhr.i15Hx0LTZ2tlsWeDcActSGGBWVc" +
                "vytPF3cK9mDWy44baBgCVI3AEbGCqg.NGhDPqOh1ZHfKFtYlBZfG4xf2n..CdxcM5x4INxnVz2.biMkfhfkw8haJuR0RaUY37lBxZ9z" +
                "3e.TlH0zdjaDjxh2tCoZQiHWPMe8HMv5LFYPZvsMp3tkG5u_QM9ymtn8jG.nDvEDA0rhBoODWguLW5079nD7RoezDxr.2b76jz7P4jY" +
                "d2k8BsZbBF6Y7V2nl.Gx9Sw5HVXa3cRWFBevCqUBPc5Tod4Cy1lLnTbxTYCpLYethRWERjX43C.td4VFqMkr3.TSZCc9UtsOeb2Ulr1 " +
                "h.E8bRwPoXV9eq4vAtNX_8KVca9qcIJtvVV8kc3J6nZXWQyoLhQ5YYrtY33bT0COPBexk-" +
                "&oauth_token_secret=c5a9684d3a3aa22aa051308987219efb8d6982fc" +
                "&oauth_expires_in=3600" +
                "&oauth_session_handle=AKVdNElJthnrHDwnYDuj6fJ2ayRbJvkePz9AKwi9dQAfb4bd" +
                "&oauth_authorization_expires_in=919314350" +
                "&xoauth_yahoo_guid=DKXSX6Q5TA5SVNARZLUJU5AW7A";

            public ResponseMessages() : this(true, true) { }
            public ResponseMessages(bool getRequestTokenReturnsOK, bool getAccessTokenReturnsOK)
            {
                if (getRequestTokenReturnsOK)
                {
                    GetRequestTokenResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(DEFAULTGETREQUESTTOKENCONTENT) };
                }
                else
                {
                    GetRequestTokenResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                if (getAccessTokenReturnsOK)
                {
                    GetAccessTokenResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(DEFAULTGETACCESSTOKENCONTENT) };
                }
                else
                {
                    GetAccessTokenResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
            }

            private HttpResponseMessage _getRequestTokenResponse;
            public HttpResponseMessage GetRequestTokenResponse
            {
                get {return _getRequestTokenResponse;}
                set
                {
                    if(value == null) 
                    {
                        throw new ArgumentNullException();
                    }
                    _getRequestTokenResponse = value;
                }
            }
            private HttpResponseMessage _getAccessTokenResponse;
            public HttpResponseMessage GetAccessTokenResponse
            {
                get { return _getAccessTokenResponse; }
                set
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException();
                    }
                    _getAccessTokenResponse = value;
                }
            }
        }
        private FakeResponseHandler CreateFakeResponseHandler(ResponseMessages responseMessages)
        {
            Uri getRequestTokenUri = new Uri("https://api.login.yahoo.com/oauth/v2/get_request_token");
            Uri getAccessTokenUri = new Uri("https://api.login.yahoo.com/oauth/v2/get_token");
            FakeResponseHandler handler = new FakeResponseHandler();
            handler.AddFakeResponse(getRequestTokenUri, responseMessages.GetRequestTokenResponse, true);
            handler.AddFakeResponse(getAccessTokenUri, responseMessages.GetAccessTokenResponse, true);
            return handler;
        }
        private OAuth.ConsumerInfo CreateFakeConsumerInfo()
        {
            return new OAuth.ConsumerInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        }

        private OAuth.AccessAndSessionInfo CreateFakeAccessAndSessionInfo()
        {
            return new OAuth.AccessAndSessionInfo(Guid.NewGuid().ToString(), DateTime.Now.AddMinutes(30), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), DateTime.Now.AddMinutes(30));
        }

        private static Regex QUERYSTRINGREGEX = new Regex("((?:\\?|&)(?<param>[^=]+)=(?<value>[^&]+))");
        private Dictionary<string, string> QueryStringToDictionary(string queryString)
        {
            if (!queryString.StartsWith("?"))
                queryString = queryString.Insert(0, "?");

            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            foreach (Match m in QUERYSTRINGREGEX.Matches(queryString))
            {
                queryParams.Add(m.Groups["param"].Value, m.Groups["value"].Value);
            }
            return queryParams;
        }

    #endregion
    }
}

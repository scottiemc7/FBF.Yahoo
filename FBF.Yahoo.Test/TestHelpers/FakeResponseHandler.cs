using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FBF.Yahoo.Extensions;

namespace FBF.Yahoo.Test.TestHelpers
{
    public class FakeResponseHandler : DelegatingHandler
    {
        private class UriInfo
        {
            public UriInfo(bool hasBeenCalled, HttpResponseMessage msg)
            {
                HasBeenCalled = hasBeenCalled;
                Response = msg;
            }
            public bool HasBeenCalled;
            public HttpResponseMessage Response;
        }

        private readonly Dictionary<string, UriInfo> _fakeResponses = new Dictionary<string, UriInfo>();
        private readonly Dictionary<string, UriInfo> _fakeResponsesIgnoreQuery = new Dictionary<string, UriInfo>();
        public void AddFakeResponse(Uri uri, HttpResponseMessage responseMessage, bool ignoreQueryString)
        {
            if (ignoreQueryString)
                _fakeResponsesIgnoreQuery.Add(uri.ToUriWithoutQueryString(), new UriInfo(false, responseMessage));
            else
                _fakeResponses.Add(uri.ToUriWithSortedQueryString(), new UriInfo(false, responseMessage));
        }

        public void AddFakeResponse(Uri uri, HttpResponseMessage responseMessage)
        {
            AddFakeResponse(uri, responseMessage, false);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            string  uri = request.RequestUri.ToUriWithSortedQueryString(),
                    uriWithoutQuery = request.RequestUri.ToUriWithoutQueryString();

            if (_fakeResponses.ContainsKey(uri))
            {
                _fakeResponses[uri].HasBeenCalled = true;
                return Task.FromResult<HttpResponseMessage>(_fakeResponses[uri].Response);
            }
            else if(_fakeResponsesIgnoreQuery.ContainsKey(uriWithoutQuery))
            {
                _fakeResponsesIgnoreQuery[uriWithoutQuery].HasBeenCalled = true;
                return Task.FromResult<HttpResponseMessage>(_fakeResponsesIgnoreQuery[uriWithoutQuery].Response);
            }
            else
                return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }

        public bool WasCalled(Uri uri)
        {
            string  uriWithSortedQuery = uri.ToUriWithSortedQueryString(),
                    uriWithoutQuery = uri.ToUriWithoutQueryString();

            if (_fakeResponses.ContainsKey(uriWithSortedQuery))
                return _fakeResponses[uriWithSortedQuery].HasBeenCalled;
            else if (_fakeResponsesIgnoreQuery.ContainsKey(uriWithoutQuery))
                return _fakeResponsesIgnoreQuery[uriWithoutQuery].HasBeenCalled;
            else
                return false;
        }
    }
}

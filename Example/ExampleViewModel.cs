using FBF.Yahoo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Example
{
    class ExampleViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private OAuth _oauth;
        
        public ExampleViewModel()
        {
        }

        private string _error;
        public string Error
        {
            get { return _error; }
            set
            {
                _error = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Error"));
                }
            }
        }

        private bool _isAuthorized;
        public bool IsAuthorized
        {
            get { return _isAuthorized; }
            set
            {
                _isAuthorized = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IsAuthorized"));
                    PropertyChanged(this, new PropertyChangedEventArgs("CanLookup"));
                }
            }
        }

        public bool CanLookup
        {
            get { return _isAuthorized && !String.IsNullOrWhiteSpace(_lookupUri); }
        }

        private string _authUri;
        public string AuthURI
        {
            get { return _authUri; }
            set
            {
                _authUri = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("AuthURI"));
            }
        }

        private string _lookupUri = "http://fantasysports.yahooapis.com/fantasy/v2/users;use_login=1/games";
        public string LookupUri
        {
            get { return _lookupUri; }
            set
            {
                _lookupUri = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("LookupUri"));
                    PropertyChanged(this, new PropertyChangedEventArgs("CanLookup"));
                }
            }
        }
        private string _lookupResponse;
        public string LookupResponse
        {
            get { return _lookupResponse; }
            set
            {
                _lookupResponse = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("LookupResponse"));
                }
            }
        }

        private string _authCode;
        private bool _authCodeSet = false;
        public string AuthCode
        {
            get { return _authCode; }
            set
            {
                _authCode = value;
                _authCodeSet = true;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("AuthCode"));
                }
            }
        }

        private bool _authCodeRequired;
        public bool AuthCodeRequired
        {
            get { return _authCodeRequired; }
            set
            {
                _authCodeRequired = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("AuthCodeRequired"));
                }
            }
        }

        public async void GetUriResponse()
        {
            if(String.IsNullOrWhiteSpace(LookupUri))
            {
                Error = "No URI set";
                return;
            }

            if(_oauth == null || _oauth.AccessInfo == null)
            {
                Error = "Not yet authorized";
                return;
            }

            Error = null;
            Uri signedUri = _oauth.SignUri(LookupUri, HttpMethod.Get);
            using (HttpClient cli = new HttpClient())
            {
                HttpResponseMessage response = await cli.GetAsync(signedUri);
                if(response.IsSuccessStatusCode)
                {
                    LookupResponse = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Error = "Bad response";
                }
            }
        }

        public async void Authorize(OAuth.ConsumerInfo consumerInfo)
        {
            IsAuthorized = false;
            Error = String.Empty;
            LookupResponse = String.Empty;
            AuthCode = String.Empty;

            //if (_oauth != null && _oauth.AccessInfo != null)
            //{
            //    Error = "Already authorized";
            //    return;
            //}

            if(consumerInfo == null)
            {
                Error = "Consumer info required";
                return;
            }

            _authCodeSet = false;
            _oauth = new OAuth(new HttpClient(), consumerInfo, AuthUriCallback);
            AuthCodeRequired = true;

            try
            {
                await _oauth.GetAccessAsync();
                IsAuthorized = true;
            }
            catch(RequestFailedException e)
            {
                Error = e.Message;
            }
            finally
            {
                AuthCodeRequired = false;
            }
        }


        Task<string> AuthUriCallback(string authorizationUri)
        {
            AuthURI = authorizationUri;
            _authCodeSet = false;

            return Task.Run<string>(async () =>
            {
                while (!_authCodeSet) { await Task.Delay(50); }
                return AuthCode;
            });
        }
    }
}

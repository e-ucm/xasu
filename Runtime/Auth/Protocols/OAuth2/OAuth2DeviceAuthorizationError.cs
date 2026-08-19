using Newtonsoft.Json;
using System;

namespace Xasu.Auth.Protocols.OAuth2
{
    public class OAuth2DeviceAuthorizationError : Exception
    {
        public OAuth2DeviceAuthorizationError() { }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("error_description")]
        public string ErrorDescription { get; set; }
    }
}

using Newtonsoft.Json;

namespace Xasu.Auth.Protocols.OAuth2
{
    internal class OAuth2DeviceAuthorization
    {
        public OAuth2DeviceAuthorization() { }

        [JsonProperty("device_code")]
        public string DeviceCode { get; set; }

        [JsonProperty("user_code")]
        public string UserCode { get; set; }

        [JsonProperty("verification_uri")]
        public string VerificationUri { get; set; }

        [JsonProperty("verification_uri_complete")]
        public string VerificationUriComplete { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("interval")]
        public int Interval { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;
using TinCan;
using UnityEngine;
using Xasu.Auth.Protocols.OAuth2;
using Xasu.Auth.Utils;
using Xasu.Exceptions;
using Xasu.Requests;

namespace Xasu.Auth.Protocols
{
    public class OAuth2DeviceProtocol : IAuthProtocol
    {
        private const string fieldMissingMessage = "Field \"{0}\" required for \"OAuth 2.0 Device\" authentication is missing!";

        // Standard fields (same naming as OAuth2Protocol)
        private const string deviceAuthorizationEndpointField = "device_authorization_endpoint";
        private const string tokenEndpointField = "token_endpoint";
        private const string clientIdField = "client_id";
        private const string scopeField = "scope";
        private const string homePageField = "homepage";

        private string deviceAuthorizationEndpoint;
        private string tokenEndpoint;
        private string clientId;
        private string scope;
        private string homePage;

        private OAuth2Token token;
        public OAuth2Token Token { get { return token; } }

        public IAsyncPolicy Policy { get; set; }
        public IHttpRequestHandler RequestHandler { get; set; }
        public AuthState State { get; protected set; }
        public string ErrorMessage { get; protected set; }
        public Agent Agent { get; protected set; }

        public delegate void OnAuthorizationInfoUpdate(OAuth2Token info);
        private OnAuthorizationInfoUpdate onAuthorizationInfoUpdate;

        public async Task Init(IDictionary<string, string> config)
        {
            XasuTracker.Instance.Log("[OAuth2Device] Starting");

            deviceAuthorizationEndpoint = config.GetRequiredValue(deviceAuthorizationEndpointField, fieldMissingMessage);
            tokenEndpoint = config.GetRequiredValue(tokenEndpointField, fieldMissingMessage);
            clientId = config.GetRequiredValue(clientIdField, fieldMissingMessage);

            scope = config.Value(scopeField);

            homePage = tokenEndpoint.Replace((new Uri(tokenEndpoint)).AbsolutePath, "");
            if (config.ContainsKey(homePageField))
            {
                homePage = config.Value(homePageField);
            }

            // Step 1: Request device and user codes
            var deviceAuth = await DoDeviceAuthorizationRequest(deviceAuthorizationEndpoint, clientId, scope);

            XasuTracker.Instance.Log("[OAuth2Device] User code: " + deviceAuth.UserCode);

            // Step 2: Open verification URL in browser for user to approve
            var verificationUrl = !string.IsNullOrEmpty(deviceAuth.VerificationUriComplete)
                ? deviceAuth.VerificationUriComplete
                : deviceAuth.VerificationUri;

            AuthUtility.OpenUrl(verificationUrl);

            XasuTracker.Instance.Log("[OAuth2Device] Opened verification URL: " + verificationUrl);

            // Step 3: Poll the token endpoint until approved
            var interval = deviceAuth.Interval > 0 ? deviceAuth.Interval : 5;
            var maxAttempts = deviceAuth.ExpiresIn > 0 ? (deviceAuth.ExpiresIn / interval) + 1 : 60;

            token = await PollForToken(tokenEndpoint, clientId, deviceAuth.DeviceCode, interval, maxAttempts);

            if (token != null)
            {
                XasuTracker.Instance.Log("[OAuth2Device] Token obtained: " + token.AccessToken);
                Agent = new Agent
                {
                    account = new AgentAccount
                    {
                        homePage = homePage,
                        name = token.Username
                    }
                };
            }
        }

        public Task UpdateParamsForAuth(MyHttpRequest request)
        {
            if (token.Expired)
            {
                State = AuthState.RequiresInteraction;
                ErrorMessage = "The authorization has expired. Please log in again using the device code flow.";
                return Task.CompletedTask;
            }

            var tokenType = char.ToUpper(token.TokenType[0]) + token.TokenType.Substring(1).ToLower();
            request.headers.Add("Authorization", string.Format("{0} {1}", tokenType, token.AccessToken));
            return Task.CompletedTask;
        }

        public void RegisterAuthInfoUpdate(OnAuthorizationInfoUpdate toRegister)
        {
            if (toRegister == null) return;

            onAuthorizationInfoUpdate += toRegister;
            if (token != null)
            {
                toRegister(token);
            }
        }

        private async Task<OAuth2DeviceAuthorization> DoDeviceAuthorizationRequest(string endpoint, string clientId, string scope)
        {
            var form = new Dictionary<string, string>()
            {
                { "client_id", clientId }
            };

            if (!string.IsNullOrEmpty(scope))
            {
                form.Add("scope", scope);
            }

            var httpRequest = new MyHttpRequest
            {
                url = endpoint,
                method = "POST",
                form = form,
                policy = Policy
            };

            try
            {
                var response = await RequestHandler.SendRequest(httpRequest);
                return JsonConvert.DeserializeObject<OAuth2DeviceAuthorization>(Encoding.UTF8.GetString(response.content));
            }
            catch (APIException ex)
            {
                OAuth2AuthorizationError error = null;
                try
                {
                    error = JsonConvert.DeserializeObject<OAuth2AuthorizationError>(ex.Message);
                }
                catch { }

                if (error != null)
                {
                    throw error;
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task<OAuth2Token> PollForToken(string tokenUrl, string clientId, string deviceCode, int interval, int maxAttempts)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                await Task.Delay(interval * 1000);

                var form = new Dictionary<string, string>()
                {
                    { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" },
                    { "client_id", clientId },
                    { "device_code", deviceCode }
                };

                var httpRequest = new MyHttpRequest
                {
                    url = tokenUrl,
                    method = "POST",
                    form = form,
                    policy = Policy
                };

                try
                {
                    var response = await RequestHandler.SendRequest(httpRequest);
                    var tokenResponse = JsonConvert.DeserializeObject<OAuth2Token>(Encoding.UTF8.GetString(response.content));
                    tokenResponse.ClientId = clientId;
                    return tokenResponse;
                }
                catch (APIException ex)
                {
                    OAuth2DeviceAuthorizationError error = null;
                    try
                    {
                        error = JsonConvert.DeserializeObject<OAuth2DeviceAuthorizationError>(ex.Message);
                    }
                    catch { }

                    if (error != null)
                    {
                        switch (error.Error)
                        {
                            case "authorization_pending":
                                XasuTracker.Instance.Log("[OAuth2Device] Waiting for user authorization...");
                                continue;

                            case "slow_down":
                                XasuTracker.Instance.Log("[OAuth2Device] Slow down: adding 5s to interval");
                                interval += 5;
                                continue;

                            case "expired_token":
                                throw new OAuth2AuthorizationError
                                {
                                    Error = "expired_token",
                                    ErrorDescription = "The device code has expired. Please restart the authorization flow."
                                };

                            case "access_denied":
                                throw new OAuth2AuthorizationError
                                {
                                    Error = "access_denied",
                                    ErrorDescription = "The user denied the authorization request."
                                };

                            default:
                                throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            throw new TimeoutException("Device authorization timed out after " + maxAttempts + " attempts.");
        }

        public void Unauthorized(APIException apiException)
        {
            State = AuthState.Errored;
            ErrorMessage = apiException.Message;
        }

        public void Forbidden(APIException apiException)
        {
            State = AuthState.Errored;
            ErrorMessage = apiException.Message;
        }
    }
}

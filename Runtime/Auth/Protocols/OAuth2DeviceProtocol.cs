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
        private const string pollIntervalField = "poll_interval";
        private const string maxPollAttemptsField = "max_poll_attempts";

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

            if (!IsValidHttpUrl(deviceAuthorizationEndpoint))
            {
                State = AuthState.Errored;
                ErrorMessage = "The device authorization endpoint is not a valid URL.";
                XasuTracker.Instance.LogError("[OAuth2Device] " + ErrorMessage + " Received: " + deviceAuthorizationEndpoint);
                throw new OAuth2AuthorizationError
                {
                    Error = "invalid_device_authorization_endpoint",
                    ErrorDescription = ErrorMessage + " Received: " + deviceAuthorizationEndpoint
                };
            }

            if (!IsValidHttpUrl(tokenEndpoint))
            {
                State = AuthState.Errored;
                ErrorMessage = "The token endpoint is not a valid URL.";
                XasuTracker.Instance.LogError("[OAuth2Device] " + ErrorMessage + " Received: " + tokenEndpoint);
                throw new OAuth2AuthorizationError
                {
                    Error = "invalid_token_endpoint",
                    ErrorDescription = ErrorMessage + " Received: " + tokenEndpoint
                };
            }

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

            if (!IsValidHttpUrl(verificationUrl))
            {
                State = AuthState.Errored;
                ErrorMessage = "The device authorization server did not provide a valid verification URL.";
                XasuTracker.Instance.LogError("[OAuth2Device] " + ErrorMessage + " Received: " + verificationUrl);
                throw new OAuth2AuthorizationError
                {
                    Error = "invalid_verification_uri",
                    ErrorDescription = ErrorMessage
                };
            }

            AuthUtility.OpenUrl(verificationUrl);

            XasuTracker.Instance.Log("[OAuth2Device] Opened verification URL: " + verificationUrl);

            // Step 3: Poll the token endpoint until approved
            var interval = deviceAuth.Interval > 0 ? deviceAuth.Interval : 5;
            var maxAttempts = deviceAuth.ExpiresIn > 0 ? (deviceAuth.ExpiresIn / interval) + 1 : 60;

            if (config.ContainsKey(pollIntervalField) && int.TryParse(config.Value(pollIntervalField), out int pollInterval) && pollInterval > 0)
            {
                interval = pollInterval;
            }
            if (config.ContainsKey(maxPollAttemptsField) && int.TryParse(config.Value(maxPollAttemptsField), out int pollMaxAttempts) && pollMaxAttempts > 0)
            {
                maxAttempts = pollMaxAttempts;
            }

            XasuTracker.Instance.Log(string.Format("[OAuth2Device] Polling token endpoint every {0}s for up to {1} attempts.", interval, maxAttempts));

            token = await PollForToken(tokenEndpoint, clientId, deviceAuth.DeviceCode, interval, maxAttempts);

            if (token != null)
            {
                State = AuthState.Working;
                XasuTracker.Instance.Log("[OAuth2Device] Token obtained: " + token.AccessToken);
                XasuTracker.Instance.Log("[OAuth2Device] Username found: " + token.Username);
                onAuthorizationInfoUpdate?.Invoke(token);
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

                if (response.status < 200 || response.status >= 300)
                {
                    throw BuildDeviceAuthorizationError(response, httpRequest.url);
                }

                var deviceAuth = JsonConvert.DeserializeObject<OAuth2DeviceAuthorization>(Encoding.UTF8.GetString(response.content ?? new byte[0]));

                if (string.IsNullOrEmpty(deviceAuth.DeviceCode) ||
                    string.IsNullOrEmpty(deviceAuth.UserCode) ||
                    (string.IsNullOrEmpty(deviceAuth.VerificationUri) && string.IsNullOrEmpty(deviceAuth.VerificationUriComplete)))
                {
                    throw new OAuth2AuthorizationError
                    {
                        Error = "invalid_response",
                        ErrorDescription = "The device authorization server response is missing required fields (device_code, user_code or verification_uri)."
                    };
                }

                return deviceAuth;
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

        private static Exception BuildDeviceAuthorizationError(MyHttpResponse response, string url)
        {
            var body = Encoding.UTF8.GetString(response.content ?? new byte[0]);

            OAuth2AuthorizationError error = null;
            try
            {
                error = JsonConvert.DeserializeObject<OAuth2AuthorizationError>(body);
            }
            catch { }

            if (error != null && !string.IsNullOrEmpty(error.Error))
            {
                error.ErrorDescription = string.IsNullOrEmpty(error.ErrorDescription) ? body : error.ErrorDescription;
                return error;
            }

            return new OAuth2AuthorizationError
            {
                Error = "http_" + response.status,
                ErrorDescription = string.Format("Device authorization request to \"{0}\" failed with HTTP status {1}: {2}", url, response.status, body)
            };
        }

        private static bool IsValidHttpUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out Uri uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
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
                    policy = Policy,
                    timeout = Math.Max(interval, 10)
                };

                XasuTracker.Instance.Log(string.Format("[OAuth2Device] Poll attempt {0}/{1}: POST {2} (client_id={3}, device_code={4})", attempt + 1, maxAttempts, tokenUrl, clientId, deviceCode));

                try
                {
                    var response = await RequestHandler.SendRequest(httpRequest);

                    var responseBody = Encoding.UTF8.GetString(response.content ?? new byte[0]);
                    XasuTracker.Instance.Log("[OAuth2Device] Poll response (" + response.status + "): " + responseBody);

                    if (response.status < 200 || response.status >= 300)
                    {
                        OAuth2DeviceAuthorizationError error = null;
                        try
                        {
                            error = JsonConvert.DeserializeObject<OAuth2DeviceAuthorizationError>(responseBody);
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
                                    throw error;
                            }
                        }

                        throw new OAuth2AuthorizationError
                        {
                            Error = "http_" + response.status,
                            ErrorDescription = string.Format("Token request to \"{0}\" failed with HTTP status {1}: {2}", tokenUrl, response.status, responseBody)
                        };
                    }

                    var tokenResponse = JsonConvert.DeserializeObject<OAuth2Token>(responseBody);
                    if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                    {
                        throw new OAuth2AuthorizationError
                        {
                            Error = "invalid_response",
                            ErrorDescription = "The token endpoint response is missing the access_token."
                        };
                    }

                    tokenResponse.ClientId = clientId;
                    XasuTracker.Instance.Log("[OAuth2Device] Token retrieved after " + (attempt + 1) + " attempt(s).");
                    return tokenResponse;
                }
                catch (NetworkException ex)
                {
                    XasuTracker.Instance.Log("[OAuth2Device] Network error during token poll (will retry): " + ex.Message);
                    continue;
                }
                catch (APIException ex)
                {
                    // Fallback for request handlers that throw instead of returning the error response
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

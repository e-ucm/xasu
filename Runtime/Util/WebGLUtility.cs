using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions; 
using System.Globalization;           

#if !UNITY_EDITOR && UNITY_WEBGL
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using Xasu.Config;

namespace Xasu.Util
{
    internal static class WebGLUtility
    {
        private const string UNITY_TRACKER_WEBGL_LISTENING = "unity_tracker_webgl_listening";

#if !UNITY_EDITOR && UNITY_WEBGL
        [DllImport("__Internal")]
        public static extern void OpenUrl(string url);

        [DllImport("__Internal")]
        public static extern string GetParameter(string name);

        [DllImport("__Internal")]
        public static extern void ClearUrl();

        [DllImport("__Internal")]
        public static extern string GetUrl();

        [DllImport("__Internal")]
        public static extern string GetCompleteUrl();
#else

        public static void OpenUrl(string url) { throw new NotSupportedException(); }
        public static string GetParameter(string name) { throw new NotSupportedException(); }
        public static void ClearUrl() { throw new NotSupportedException(); }
        public static string GetUrl() { throw new NotSupportedException(); }
        public static string GetCompleteUrl() { throw new NotSupportedException(); }

#endif

        public static bool IsWebGLListening()
        {
            return PlayerPrefs.HasKey(UNITY_TRACKER_WEBGL_LISTENING) && PlayerPrefs.GetInt(UNITY_TRACKER_WEBGL_LISTENING) == 1;
        }

        public static void SetWebGLListening(bool value)
        {
            PlayerPrefs.SetInt(UNITY_TRACKER_WEBGL_LISTENING, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static TrackerConfig GetUrlTrackerConfig()
        {
            if(!GetCompleteUrl().Contains("?"))
            {
                return null;
            }

            // Initialize default values
            string resultUri = null;
            string backupUri = null;
            string backupType = "XAPI";
            string actorName = null;
            string actorHomePage = null;
            bool debug = false;
            int? batchLength = null;
            float? batchTimeout = null; // Assuming ms returns a long
            float? maxRetryDelay = null;

            // SSO / Auth variables
            string ssoTokenEndpoint = GetParameter("sso_token_endpoint");
            string ssoClientId = GetParameter("sso_client_id");
            string ssoLoginHint = GetParameter("sso_login_hint");
            string ssoGrantType = GetParameter("sso_grant_type");
            string ssoScope = GetParameter("sso_scope");
            string ssoUsername = GetParameter("sso_username");
            string ssoPassword = GetParameter("sso_password");
                
            string username = GetParameter("username");
            string password = GetParameter("password");
            string authToken = GetParameter("auth_token");

            // Check if any relevant params exist (if necessary for your logic)
            // C# doesn't have a direct 'size' property for URLSearchParams, 
            // so we assume if required params are present, we proceed.
                
            resultUri = GetParameter("result_uri");
            backupUri = GetParameter("backup_uri");
            backupType = GetParameter("backup_type") ?? "XAPI";
            actorHomePage = GetParameter("actor_homepage");
            actorName = GetParameter("actor_user");

            // Batch Parsing
            string batchLengthParam = GetParameter("batch_length");
            if (int.TryParse(batchLengthParam, out int bl)) batchLength = bl;

            batchTimeout = ParseToSeconds(GetParameter("batch_timeout"));
            maxRetryDelay = ParseToSeconds(GetParameter("max_retry_delay"));

            debug = "true".Equals(GetParameter("debug"), StringComparison.InvariantCultureIgnoreCase);

            var trackerConfig = new TrackerConfig();

            // If no password we assume the username is the password
            ssoPassword = string.IsNullOrEmpty(ssoPassword) ? ssoUsername : ssoPassword;

            if (!string.IsNullOrEmpty(ssoTokenEndpoint))
            {
                trackerConfig.AuthProtocol = "oauth2";
                if (ssoGrantType == "password")
                {
                    trackerConfig.AuthParameters = new Dictionary<string, string>
                    {
                        {"grant_type", "password"},
                        {"token_endpoint", ssoTokenEndpoint},
                        {"client_id", ssoClientId},
                        {"login_hint", ssoLoginHint},
                        {"username", ssoUsername},
                        {"scope", ssoScope},
                        {"password", ssoPassword}
                    };
                }   
            }
            else if (!string.IsNullOrEmpty(ssoUsername))
            {
                trackerConfig.Online = true;
                trackerConfig.AuthProtocol = "basic";
                trackerConfig.AuthParameters = new Dictionary<string, string>
                    {
                        {"username", ssoUsername},
                        {"password", ssoPassword}
                    };
            }

            if (batchLength!=null) trackerConfig.BatchSize = batchLength.Value;
            if (batchTimeout != null) trackerConfig.FlushInterval = batchTimeout.Value / 1000f; // Assuming batchTimeout is in ms and FlushInterval is in seconds
            // TODO implement maxRetryDelay in the tracker and add it to the config parsing here

            if (!string.IsNullOrEmpty(resultUri))
            {
                trackerConfig.Online = true;
                trackerConfig.LRSEndpoint = resultUri;
            }
            if (!string.IsNullOrEmpty(backupUri))
            {
                trackerConfig.Backup = true;
                trackerConfig.BackupEndpoint = backupUri;
                trackerConfig.BackupAuthProtocol = "same";
                if("XAPI".Equals(backupType, StringComparison.InvariantCultureIgnoreCase))
                {
                    trackerConfig.BackupTraceFormat = TraceFormats.XAPI;
                }
                else
                {
                    throw new NotSupportedException($"Backup type '{backupType}' is not supported. Supported types: XAPI");
                }
            }

            if(!string.IsNullOrEmpty(actorHomePage))
            {
                trackerConfig.AuthParameters["homepage"] = actorHomePage;
            }

            if(trackerConfig.Online == false && trackerConfig.Backup == false)
            {
                Debug.Log("Tracker configuration from URL is invalid: No result_uri or backup_uri provided.");
                return null;
            }

            return trackerConfig;

        }

        /// <summary>
        /// Parses a string duration (e.g., "5min", "2.5s", "500ms") into seconds as a double.
        /// </summary>
        private static float? ParseToSeconds(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
    
            // Regex: Matches (digits or decimals) followed by (optional unit)
            // Uses NumberStyles.Any to allow decimal points
            var match = Regex.Match(input.Trim(), @"^([\d\.]+)(ms|s|m|min|h|d|w)?$", RegexOptions.IgnoreCase);
    
            if (!match.Success) return null;
    
            // Use CultureInfo.InvariantCulture to ensure '.' is treated as a decimal separator
            if (!float.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float value))
                return null;
    
            string unit = match.Groups[2].Value.ToLower();
    
            return unit switch
            {
                "ms"         => value / 1000.0f,
                "s"          => value,
                "m" or "min" => value * 60.0f,
                "h"          => value * 3600.0f,
                "d"          => value * 86400.0f,
                "w"          => value * 604800.0f,
                _            => value // Default to seconds if no unit provided
            };
        }
    }
}

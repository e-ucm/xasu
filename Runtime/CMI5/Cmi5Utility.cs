using System;
using UnityEngine;
using Xasu.Util;

#if UNITY_WEBGL
using UnityEngine.Networking;
#endif

namespace Xasu.CMI5
{
    public static class Cmi5Utility
    {

        public static string GetParam(string name)
        {
#if UNITY_WEBGL
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                return UnityWebRequest.UnEscapeURL(WebGLUtility.GetParameter(name));
            }
#endif
            if(Application.platform == RuntimePlatform.WindowsPlayer)
            {
                var cliArgs = Environment.GetCommandLineArgs();
                var cmi5ParamIndex = Array.IndexOf(cliArgs, "-cmi5");
                if(cmi5ParamIndex == -1 || cmi5ParamIndex >= cliArgs.Length)
                {
                    throw new NotImplementedException("Cmi5 param not found or wrong formatted!");
                }
                var cmi5Args = cliArgs[cmi5ParamIndex + 1];

                var uri = new Uri(cmi5Args);
#if NET_4_6
                var queryDictionary = UriHelper.DecodeQueryParameters(uri.Query);
#else
                var queryDictionary = System.Web.HttpUtility.ParseQueryString(uri.Query);
#endif
                return queryDictionary.Get(name);
            }
            else if(Application.platform == RuntimePlatform.WindowsEditor)
            {
                var uri = new Uri(GameObject.FindObjectOfType<ArgSimulator>().cmi5Arg);
#if NET_4_6
                var queryDictionary = UriHelper.DecodeQueryParameters(uri.Query);
#else
                var queryDictionary = System.Web.HttpUtility.ParseQueryString(uri.Query);
#endif
                return queryDictionary.Get(name);
            }

            throw new NotImplementedException("Cmi5 not implemented in other platforms yet!");
        }
    }
}

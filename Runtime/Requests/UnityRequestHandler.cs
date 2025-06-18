using Xasu.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Xasu.Util;

namespace Xasu.Requests
{
    public class UnityRequestHandler : IHttpRequestHandler
    {


        public async Task<MyHttpResponse> SendRequest(MyHttpRequest myRequest, IProgress<float> progress = null)
        {
            bool isSimvaStatements = false;
            // Simva Special cases
            if (XasuTracker.Instance.TrackerConfig.Simva)
            {
                if(Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    // CORS restricted
                    myRequest.headers.Remove("X-Experience-API-Version");
                }

                // statements endpoint is in /result
                if (myRequest.url.EndsWith("statements"))
                {
                    myRequest.url = myRequest.url.Replace("statements", "result");
                    isSimvaStatements = true;
                }
            }

            // Set URL
            if (string.IsNullOrEmpty(myRequest.url))
            {
                throw new ArgumentNullException("RequestsUtility.DoRequest needs the final URL to make que request (without the query parameters)");
            }

            // Set query params
            string qs = string.Empty;
            qs = AppendParamsToExistingQueryString(qs, myRequest.queryParams);
            if (!string.IsNullOrEmpty(qs))
            {
                myRequest.url += "?" + qs;
            }

            // Await auth
            if (myRequest.authorization != null)
            {
                await myRequest.authorization.UpdateParamsForAuth(myRequest);
            }

            // Perform request
            MyHttpResponse result;

            UnityWebRequest webResult = null;
            try
            {
                if (myRequest.policy != null)
                {
                    webResult = await myRequest.policy.ExecuteAsync(
                        async (_) => {
                            return await DoRequest(myRequest.ToWebRequest());
                            }
                        , new CancellationToken(), true);
                }
                else
                {
                    webResult = await DoRequest(myRequest.ToWebRequest());
                }

                var responseData = webResult.downloadHandler.data;

                if (isSimvaStatements)
                {
                    var jArray = JArray.Parse(Encoding.UTF8.GetString(myRequest.content));
                    var idsArray = new JArray();
                    foreach(JObject state in jArray)
                    {
                        idsArray.Add(state.GetValue("id").ToString());
                    }
                    responseData = Encoding.UTF8.GetBytes(idsArray.ToString());
                }

                result = new MyHttpResponse()
                {
                    status = (int)webResult.responseCode,
                    content = responseData,
                    contentType = webResult.GetResponseHeader("Content-Type"),
                    etag = webResult.GetRequestHeader("Etag")
                };
            }
            catch (APIException ex)
            {
                Debug.Log(string.Format("[REQUESTS ({0})] I've seen API exceptions here... ", Thread.CurrentThread.ManagedThreadId));
                result = ex.Response;
            }
            catch (NetworkException)
            {
                Debug.Log(string.Format("[REQUESTS ({0})] I've seen network exceptions here... ", Thread.CurrentThread.ManagedThreadId));
                throw;
            }

            return result;
        }

        public static async Task<T> DoRequest<T>(UnityWebRequest webRequest, IProgress<float> progress = null)
        {
            var result = await DoRequest(webRequest, false, null);
            return JsonConvert.DeserializeObject<T>(result.downloadHandler.text);
        }

        public static async Task<UnityWebRequest> DoRequest(UnityWebRequest webRequest, IProgress<float> progress = null)
        {
            return await DoRequest(webRequest, false, null);
        }

        public static async Task<UnityWebRequest> DoRequestInBackground(UnityWebRequest webRequest, IProgress<float> progress = null)
        {
            return await DoRequest(webRequest, false, null);
        }

        private static async Task<UnityWebRequest> DoRequest(UnityWebRequest webRequest, bool inBackground, IProgress<float> progress = null)
        {
            UnityWebRequestAsyncOperation asyncRequest;
            string backgroundError = null;

            if (inBackground)
            {
                Application.runInBackground = true;
            }

            Debug.Log(string.Format("[REQUESTS ({2})] {1} Requesting \"{0}\"", webRequest.url, webRequest.method, Thread.CurrentThread.ManagedThreadId));
            asyncRequest = webRequest.SendWebRequest();

            if (inBackground)
            {
                asyncRequest.completed += asyncOperation =>
                {
                    Application.runInBackground = false;
                };
            }

            if (webRequest.uploadHandler != null)
            {
                while (!webRequest.isDone)
                {
                    await Task.Yield();
                    progress?.Report(asyncRequest.progress);
                }
            }
            else
            {
                await asyncRequest;
            }

            // Sometimes the webrequest is finished but the download is not
            while (!(webRequest.result == UnityWebRequest.Result.ConnectionError) && !(webRequest.result == UnityWebRequest.Result.ProtocolError) && !webRequest.downloadHandler.isDone && webRequest.downloadProgress != 0 && webRequest.downloadProgress != 1)
            {
                await Task.Yield();
            }

            // Network Error Exception
            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                NetworkInfo.Failed();
                throw new NetworkException(webRequest.error);
            }
            NetworkInfo.Worked();

            // API / Http Exception
            if (webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                var httpResponse = new MyHttpResponse()
                {
                    status = (int)webRequest.responseCode,
                    content = webRequest.downloadHandler.data,
                    contentType = webRequest.GetResponseHeader("Content-Type"),
                    etag = webRequest.GetRequestHeader("Etag")
                };

                throw new APIException((int)webRequest.responseCode, webRequest.downloadHandler.text, httpResponse);
            }

            // Background exclussive errors
            if (inBackground && !string.IsNullOrEmpty(backgroundError))
            {
                throw new BackgroundException(backgroundError);
            }

            Debug.Log(string.Format("[REQUESTS ({4})] {1} Request to \"{0}\" succedded ({2}): \"{3}\"",
                webRequest.url, webRequest.method, webRequest.responseCode, webRequest.downloadHandler.text, Thread.CurrentThread.ManagedThreadId));

            return webRequest;
        }


        public string AppendParamsToExistingQueryString(string currentQueryString, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            foreach (KeyValuePair<String, String> entry in parameters)
            {
                if (!string.IsNullOrEmpty(currentQueryString))
                {
                    currentQueryString += "&";
                }
#if NET_4_6
                currentQueryString += System.Net.WebUtility.UrlEncode(entry.Key) + "=" + System.Net.WebUtility.UrlEncode(entry.Value);
#else
                currentQueryString += System.Web.HttpUtility.UrlEncode(entry.Key) + "=" + System.Web.HttpUtility.UrlEncode(entry.Value);
#endif
            }

            return currentQueryString;
        }

    }
}

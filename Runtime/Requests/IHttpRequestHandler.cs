using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Xasu.Requests
{
    public interface IHttpRequestHandler
    {
        Task<MyHttpResponse> SendRequest(MyHttpRequest myRequest, IProgress<float> progress = null);

        string AppendParamsToExistingQueryString(string currentQueryString, IEnumerable<KeyValuePair<string, string>> parameters);
    }
}

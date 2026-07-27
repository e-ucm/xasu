

using System.Linq;
using UnityEngine.Networking;
using Xasu.Requests;

public static class UnityMyHttpRequestExtension
{
    public static UnityWebRequest ToWebRequest(this MyHttpRequest myRequest)
    {

        // Create the request
        UnityWebRequest request = null;
        switch (myRequest.method.ToUpper())
        {
            case "GET":
                request = UnityWebRequest.Get(myRequest.url);
                break;
            case "POST":
                request = UnityWebRequest.PostWwwForm(myRequest.url, "");
                break;
            case "PUT":
                request = UnityWebRequest.Put(myRequest.url, myRequest.content);
                break;
            case "DELETE":
                request = UnityWebRequest.Delete(myRequest.url);
                break;
        }

        if (myRequest.form != null)
        {
            var formUrlEncoded = myRequest.form
                .Select(kv => string.Format("{0}={1}", kv.Key, kv.Value))
                .Aggregate((p1, p2) => string.Format("{0}&{1}", p1, p2));
            myRequest.content = System.Text.Encoding.UTF8.GetBytes(formUrlEncoded);
            myRequest.contentType = "application/x-www-form-urlencoded";
        }

        // Set content type
        var contentType = GetContentType(myRequest);
        if (myRequest.content != null && myRequest.content.Length > 0)
        {
            request.uploadHandler = new UploadHandlerRaw(myRequest.content)
            {
                contentType = contentType
            };
        }

        // Set other headers
        myRequest.headers["Content-Type"] = contentType;
        foreach (var header in myRequest.headers)
        {
            request.SetRequestHeader(header.Key, header.Value);
        }

        return request;
    }

    private static string GetContentType(MyHttpRequest req)
    {
        var contentType = "application/octet-stream";
        if (!string.IsNullOrEmpty(req.contentType))
        {
            contentType = req.contentType;
        }
        else if (req.headers.ContainsKey("Content-Type"))
        {
            contentType = req.headers["Content-Type"];
        }

        return contentType;
    }
}
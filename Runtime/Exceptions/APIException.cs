using System;
using Xasu.Requests;

namespace Xasu.Exceptions
{
    [Serializable]
    public class APIException : Exception
    {
        public int HttpCode { get; private set; }
        public MyHttpResponse Response { get; private set; }

        public APIException(int httpCode, string message, MyHttpResponse response) : base(message)
        {
            this.HttpCode = httpCode;
            this.Response = response;
            this.Response.ex = this;
        }

        public APIException(int httpCode, string message, MyHttpResponse response, Exception innerException) : base(message, innerException)
        {
            this.HttpCode = httpCode;
            this.Response = response;
            this.Response.ex = this;
        }
    }
}

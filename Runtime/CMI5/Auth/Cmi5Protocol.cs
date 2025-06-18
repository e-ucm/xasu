using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Polly;
using TinCan;
using UnityEngine.Auth.Protocols.Cmi5;
using Xasu.CMI5;
using Xasu.Exceptions;
using Xasu.Requests;

namespace Xasu.Auth.Protocols
{
    public class Cmi5Protocol : IAuthProtocol
    {
        private readonly IHttpRequestHandler requestHandler;

        public IAsyncPolicy Policy { get; set; }

        public IHttpRequestHandler RequestHandler { get; set; }

        public Agent Agent => Cmi5Helper.Actor;

        public AuthState State { get; protected set; }

        public string ErrorMessage { get; protected set; }

        private Cmi5Fetch auth;

        public async Task Init(IDictionary<string, string> config)
        {
            // This initializes the Query parameters
            Cmi5Helper.SetQuery();
            auth = await DoFetch(Cmi5Helper.Fetch, Policy);
        }

        private async Task<Cmi5Fetch> DoFetch(System.Uri fetchUrl, IAsyncPolicy policy)
        {
            var request = new MyHttpRequest
            {
                url = fetchUrl.ToString(),
                method = "POST"
            };
            request.policy = policy;
            var response = await RequestHandler.SendRequest(request);
            return DeserializeFromResponse<Cmi5Fetch>(response);
        }

        private static T DeserializeFromResponse<T>(MyHttpResponse response)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(response.content));
        }

        public Task UpdateParamsForAuth(MyHttpRequest request)
        {
            request.headers.Add("Authorization", string.Format("Basic {0}", auth.AuthToken));
            return Task.FromResult(0);
        }

        // CMI-5 cannot recover from Unauthorized or Forbidden exceptions
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

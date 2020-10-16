using AsiCSharpClient.RequestHandlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsiCSharpClient
{
    public class ApiClientBase : IApiClientBase
    {
        private readonly Uri baseTargetUrl;

        protected readonly CancellationTokenSource cancellationTokenSource;
        protected Dictionary<string, string> DefaultHeaders = new Dictionary<string, string>();
        protected HttpClient httpClient;

        public ApiClientBase(CancellationTokenSource cancellationTokenSource)
        {
            this.cancellationTokenSource = cancellationTokenSource;
            baseTargetUrl = new Uri("https://tenant1.i100/imisMain100/"); //Config.TargetImisBaseAddress
        }

        public async Task<ResponseWrapper> SendRestRequest(RequestWrapper request)
        {
            var response = new ResponseWrapper();

            try
            {
                var httpRequestMessage = CreateRequest(request);

                var httpResponse = httpClient.SendAsync(httpRequestMessage, cancellationTokenSource.Token).Result;

                response.IsSuccess = httpResponse.IsSuccessStatusCode;
                response.Reason = httpResponse.ReasonPhrase;
                response.StatusCode = httpResponse.StatusCode;

                var responseContentAString = await httpResponse.Content.ReadAsStringAsync();

                response.Content = JsonConvert.DeserializeObject<dynamic>(responseContentAString);
            }
            catch (Exception e)
            {
                response = new ResponseWrapper();
                response.Reason = e.Message;
                response.StatusCode = HttpStatusCode.BadRequest;
            }

            return response;
        }

        protected HttpRequestMessage CreateRequest(RequestWrapper restRequest)
        {
            //Request Uri
            var partialRequestUri = new Uri(baseTargetUrl, restRequest.RelativePath);

            var completeRequestUri = AppendQueryParametersToUri(restRequest.QueryParameters, partialRequestUri);

            //REST HTTP Method and instantiation
            var httpRequestMessage = new HttpRequestMessage(restRequest.HttpMethod, completeRequestUri);

            if (restRequest.AuthenticationHeaderValue != null)
                httpRequestMessage.Headers.Authorization = restRequest.AuthenticationHeaderValue;

            //Default Headers
            foreach (var header in DefaultHeaders) httpRequestMessage.Headers.Add(header.Key, header.Value);

            //Request Specific Headers
            foreach (var kvpHeader in restRequest.RequestSpecificHeaders)
                httpRequestMessage.Headers.Add(kvpHeader.Key, kvpHeader.Value);

            //Adding HttpContent if present
            if (restRequest.HttpContent != null) httpRequestMessage.Content = restRequest.HttpContent;

            return httpRequestMessage;
        }

        protected Uri AppendQueryParametersToUri(List<KeyValuePair<string, string>> queryParams, Uri partialRequestUri)
        {
            var requestUri = new UriBuilder(partialRequestUri);

            foreach (var pair in queryParams)
            {
                var queryToAppend = $"{pair.Key}={pair.Value}";

                if (requestUri.Query != null && requestUri.Query.Length > 1)
                    requestUri.Query = requestUri.Query.Substring(1) + "&" + queryToAppend;
                else
                    requestUri.Query = queryToAppend;
            }

            return requestUri.Uri;
        }

    }

    public interface IApiClientBase
    {
        Task<ResponseWrapper> SendRestRequest(RequestWrapper request);
    }
}

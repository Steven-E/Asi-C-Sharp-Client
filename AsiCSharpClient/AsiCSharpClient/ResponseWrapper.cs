using System.Net;

namespace AsiCSharpClient
{
    public class ResponseWrapper
    {
        public dynamic Content { get; set; }

        public bool IsSuccess { get; set; }

        public string Reason { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }
}

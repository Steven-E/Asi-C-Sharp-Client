using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsiCSharpClient
{
    public class RequestHandler 
    {
        public event EventHandler<HasAccessTokenEventArgs> HasAccessTokenEventHandler;

        private readonly Dictionary<string, Action<HttpListenerContext>> routingMap = new Dictionary<string, Action<HttpListenerContext>>();

        private HttpListener httpListener;

        private static string exposedHttpEndpoint = "http://127.0.0.1:5600/";
        private CancellationTokenSource cancellationTokenSource;

        public RequestHandler(CancellationTokenSource cancellationTokenSource)
        {
            this.cancellationTokenSource = cancellationTokenSource;
            routingMap.Add("/iMISAuth", ProcessImisRedirect);
            routingMap.Add("/", ProcessImisRedirect);

            httpListener = new HttpListener();
            httpListener.Prefixes.Add(exposedHttpEndpoint);
        }

        public void Start() 
        {
            try
            {
                Console.WriteLine("Staring Http Listener");
                httpListener.Start();
            }
            catch (HttpListenerException e)
            {
                Console.WriteLine("Cannot Start HttpListener. ", e);
                Environment.Exit(-1);
            }

            Task.Run(()=> Listen());
        }

        private void Listen()
        {
            Console.WriteLine($"Listening to HTTP endpoint - {exposedHttpEndpoint}");

            while (!cancellationTokenSource.Token.WaitHandle.WaitOne(1))
            {
                HttpListenerContext receivedContext;

                try
                {
                    receivedContext = httpListener.GetContext();
                }
                catch (HttpListenerException e)
                {
                    if (e.ErrorCode != 995) throw;

                    Console.WriteLine(
                        $"Catching HttpListenerException, ErrorCode: {e.ErrorCode}, Message: {e.Message}");

                    continue;
                }

                Handle(receivedContext);
            }
        }

        public void Handle(HttpListenerContext httpListenerContext)
        {
            Console.WriteLine($"Received '{httpListenerContext.Request.HttpMethod}' from client - " +
                     $"{httpListenerContext.Request.RemoteEndPoint.Address}:{httpListenerContext.Request.RemoteEndPoint.Port} for '{httpListenerContext.Request.Url.AbsolutePath}'");

            ProcessRequest(httpListenerContext);
        }

        private void ProcessImisRedirect(HttpListenerContext context)
        {
            var requestMethodType = new HttpMethod(context.Request.HttpMethod);

            if (requestMethodType == HttpMethod.Head)
            {
                SendOkResponse(context);

                return;
            }
            else
            if (requestMethodType == HttpMethod.Post)
            {
                var requestContent = ReadContent(context);

                Console.WriteLine($"Recieved refresh token from iMIS: {requestContent}");

                if (!TryReadAccessToken(requestContent, out var imisToken))
                {
                    Console.WriteLine($"Recieved request did not provide expected refresh token: {JsonConvert.SerializeObject(imisToken)}");
                    return;
                }

                OnHasAccessTokenEvent(imisToken);

                SendOkResponse(context);

                return;
            }

        }

        public void ProcessRequest(HttpListenerContext context)
        {


            var requestPath = context.Request.RawUrl.TrimStart('/');

            if (routingMap.ContainsKey(requestPath))
            {
                routingMap[requestPath](context);
                return;
            }

        }

        private void OnHasAccessTokenEvent(ImisToken imisToken)
        {
            HasAccessTokenEventHandler?.Invoke(this, new HasAccessTokenEventArgs { ImisToken = imisToken });
        }

        public bool TryReadAccessToken(string requestContent, out ImisToken token)
        {
            bool retVal = false;
            token = null;

            if (requestContent.StartsWith("refresh_token="))
            {
                token = new ImisToken();

                token.AccessToken = requestContent.Substring(("refresh_token=").Length);
                retVal = true;
            }

            return retVal;
        }

        public void SendOkResponse(HttpListenerContext httpListenerContext)
        {
            httpListenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
            httpListenerContext.Response.OutputStream.Close();
        }

        public void SendOkResponseAndJsonPayload(HttpListenerContext httpListenerContext, string responseJson)
        {
            var responseBinary = Encoding.UTF8.GetBytes(responseJson);

            httpListenerContext.Response.ContentEncoding = Encoding.UTF8;
            httpListenerContext.Response.ContentType = "application/json";
            httpListenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
            using (var writeStream = httpListenerContext.Response.OutputStream)
            {
                writeStream.WriteAsync(responseBinary, 0, responseBinary.Length);
                writeStream.Close();
            }
        }

        public string ReadContent(HttpListenerContext httpListenerContext)
        {
            string requestText;

            using (var reader = new StreamReader(httpListenerContext.Request.InputStream,
                httpListenerContext.Request.ContentEncoding))
            {
                requestText = reader.ReadToEnd();
            }

            return requestText;
        }
    }

    public class HasAccessTokenEventArgs : EventArgs
    {
        public ImisToken ImisToken { get; set; }
    }

    public interface IRequestHandler
    {

        event EventHandler<HasAccessTokenEventArgs> HasAccessTokenEventHandler;

        string ReadContent(HttpListenerContext httpListenerContext);

        void ProcessRequest(HttpListenerContext httpListenerContext);

    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsiCSharpClient.RequestHandlers
{
    public class RequestHandler //: IRequestHandler
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
            //routingMap.Add("/", ProcessBaseUrlRequest);
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

                //requestQueue.Add(receivedContent);
                Handle(receivedContext);
            }
        }

        public void Handle(HttpListenerContext httpListenerContext)
        {
            Console.WriteLine($"Received '{httpListenerContext.Request.HttpMethod}' from client - " +
                     $"{httpListenerContext.Request.RemoteEndPoint.Address}:{httpListenerContext.Request.RemoteEndPoint.Port} for '{httpListenerContext.Request.Url.AbsolutePath}'");

            ProcessRequest(httpListenerContext);
        }

        //private void ProcessBaseUrlRequest(HttpListenerContext context)
        //{
        //    var requestMethodType = new HttpMethod(context.Request.HttpMethod);
        //    if (requestMethodType == HttpMethod.Head)
        //    {
        //        SendOkResponse(context);

        //        return;
        //    }
        //    //else if (requestMethodType == HttpMethod.Get)
        //    //{
        //    //    ProcessUiRequest(context);
        //    //    return;
        //    //}

        //    //ErrorResponse.Get(HttpStatusCode.BadRequest).Handle(context);
        //}

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
                    //ErrorResponse.Get(HttpStatusCode.BadRequest).Handle(context);
                    Console.WriteLine($"Recieved request did not provide expected refresh token: {JsonConvert.SerializeObject(imisToken)}");
                    return;
                }

                OnHasAccessTokenEvent(imisToken);

                SendOkResponse(context);

                return;
            }

            //ErrorResponse.Get(HttpStatusCode.BadRequest).Handle(context);
        }

        //private void ProcessUiRequest(HttpListenerContext context)
        //{

        //    var fullUrl = context.Request.Url.ToString();

        //    if (HttpMethod.Get != new HttpMethod(context.Request.HttpMethod))
        //    {
        //        ErrorResponse.Get(HttpStatusCode.NotFound).Handle(context);
        //        return;
        //    }
        //    string folder = AppContext.BaseDirectory;

        //    folder = Path.Combine(folder, "UI");

        //    if (fullUrl.Contains("index.js"))
        //    {
        //        var fullPath = Path.Combine(folder, "index.js");

        //        var fs = new FileStream(fullPath, FileMode.Open);

        //        string strContent;

        //        using (var reader = new StreamReader(fs))
        //        {
        //            strContent = reader.ReadToEnd();
        //        }

        //        SendOkAndUIContent(context, strContent, "application/javascript");
        //    }
        //    else if (fullUrl.Contains("manifest.json"))
        //    {
        //        var fullPath = Path.Combine(folder, "manifest.json");

        //        var fs = new FileStream(fullPath, FileMode.Open);

        //        string strContent;

        //        using (var reader = new StreamReader(fs))
        //        {
        //            strContent = reader.ReadToEnd();
        //        }

        //        SendOkAndUIContent(context, strContent, "application/json");
        //    }
        //    else if (fullUrl.Contains("index.css"))
        //    {
        //        var fullPath = Path.Combine(folder, "index.css");

        //        var fs = new FileStream(fullPath, FileMode.Open);

        //        string strContent;

        //        using (var reader = new StreamReader(fs))
        //        {
        //            strContent = reader.ReadToEnd();
        //        }

        //        SendOkAndUIContent(context, strContent, "text/css");
        //    }
        //    else if (fullUrl.Contains("App.js"))
        //    {
        //        var fullPath = Path.Combine(folder, "App.js");

        //        var fs = new FileStream(fullPath, FileMode.Open);

        //        string strContent;

        //        using (var reader = new StreamReader(fs))
        //        {
        //            strContent = reader.ReadToEnd();
        //        }

        //        SendOkAndUIContent(context, strContent, "application/javascript");
        //    }
        //    else if (fullUrl.Contains("index.html"))
        //    {
        //        var fullPath = Path.Combine(folder, "index.html");

        //        var fs = new FileStream(fullPath, FileMode.Open);

        //        string strContent;

        //        using (var reader = new StreamReader(fs))
        //        {
        //            strContent = reader.ReadToEnd();
        //        }

        //        SendOkAndUIContent(context, strContent, "text/html");
        //    }
        //}

        //private void SendOkAndUIContent(HttpListenerContext context, string content, string contentType)
        //{
        //    var responseBinary = Encoding.UTF8.GetBytes(content);

        //    context.Response.ContentEncoding = Encoding.UTF8;
        //    //httpListenerContext.Response.ContentType = "application/javascript";
        //    context.Response.ContentType = contentType;
        //    context.Response.StatusCode = (int)HttpStatusCode.OK;

        //    using (var writeStream = context.Response.OutputStream)
        //    {
        //        writeStream.WriteAsync(responseBinary, 0, responseBinary.Length);
        //        writeStream.Close();
        //    }
        //}

        public void ProcessRequest(HttpListenerContext context)
        {
            //var absolutePath = context.Request.Url.AbsolutePath;

            //if (context.Request.Url.Segments.Length > 2)
            //    absolutePath = context.Request.Url.Segments[0] + context.Request.Url.Segments[1];

            var requestPath = context.Request.RawUrl.TrimStart('/');

            if (routingMap.ContainsKey(requestPath))
            {
                routingMap[requestPath](context);
                return;
            }
            //else
                //ErrorResponse.Get(HttpStatusCode.BadRequest).Handle(context);
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

        //public void SendOkAndHtml(HttpListenerContext httpListenerContext, string content)
        //{
        //    var responseBinary = Encoding.UTF8.GetBytes(content);

        //    httpListenerContext.Response.ContentEncoding = Encoding.UTF8;
        //    //httpListenerContext.Response.ContentType = "application/javascript";
        //    httpListenerContext.Response.ContentType = "application/javascript";
        //    httpListenerContext.Response.StatusCode = (int)HttpStatusCode.OK;

        //    using (var writeStream = httpListenerContext.Response.OutputStream)
        //    {
        //        writeStream.WriteAsync(responseBinary, 0, responseBinary.Length);
        //        writeStream.Close();
        //    }
        //}

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
            //httpListenerContext.Response.ContentLength64 = responseBinary.Length;
            using (var writeStream = httpListenerContext.Response.OutputStream)
            {
                //httpListenerContext.Response.OutputStream.Write(responseBinary, 0, responseBinary.Length);

                //httpListenerContext.Response.OutputStream.Close();

                writeStream.WriteAsync(responseBinary, 0, responseBinary.Length);
                writeStream.Close();
            }
        }

        //protected void SendOKResponseAndPayload<T>(HttpListenerContext httpListenerContext,
        //    TransactionResult<T> transactionResult)
        //{
        //    SendOkResponseAndJsonPayload(httpListenerContext, JsonConvert.SerializeObject(transactionResult.Data));
        //}

        //protected void SendErrorResponseAndDetails<T>(HttpListenerContext httpListenerContext,
        //    TransactionResult<T> transactionResult)
        //{
        //    ErrorResponse.Get(HttpStatusCode.InternalServerError).Handle(httpListenerContext,
        //        JsonConvert.SerializeObject(transactionResult.Details));
        //}

        //protected void SendResponse<T>(HttpListenerContext httpListenerContext, TransactionResult<T> transactionResult)
        //{
        //    if (transactionResult.Success)
        //        SendOKResponseAndPayload(httpListenerContext, transactionResult);
        //    else
        //        SendErrorResponseAndDetails(httpListenerContext, transactionResult);
        //}

        //protected
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

        //protected void SendBadRequest(HttpListenerContext context)
        //{
        //    ErrorResponse.Get(HttpStatusCode.BadRequest).Handle(context);
        //}
    }

    public class HasAccessTokenEventArgs : EventArgs
    {
        public ImisToken ImisToken { get; set; }
    }

    public interface IRequestHandler
    {

        event EventHandler<HasAccessTokenEventArgs> HasAccessTokenEventHandler;

        string ReadContent(HttpListenerContext httpListenerContext);

        //void SendOKResponseAndPayload<T>(HttpListenerContext httpListenerContext,
        //    TransactionResult<T> transactionResult);
        //void SendOkResponse(HttpListenerContext httpListenerContext);

        //void ProcessRequest(HttpListenerContext httpListenerContext, HttpMethod requestMethodType);
        void ProcessRequest(HttpListenerContext httpListenerContext);

        //void SendOKResponseAndPayload(HttpListenerContext httpListenerContext, string responseJson);
    }
}

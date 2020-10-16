using AsiCSharpClient.RequestHandlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AsiCSharpClient
{
    public class ImisClient : ApiClientBase, IImisClient
    {

        public ImisToken ImisToken { get; set; }

        public event EventHandler<HasAccessTokenEventArgs> HasAccessTokenEventHandler;

        private RequestHandler requestHandler;

        private static string username = "";
        private static string password = "";
        private static string targetImisAddress = "";
               
               
        public ImisClient(CancellationTokenSource cancellationTokenSource) : base(cancellationTokenSource)
        {

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            httpClient = new HttpClient(clientHandler)
            {
                BaseAddress = new Uri(targetImisAddress)//config.TargetImisBaseAddress
            };

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            httpClient.DefaultRequestHeaders.Accept.Clear();
            //httpClient.DefaultRequestHeaders.Add("Accept", "application/json");           
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //HasAccessTokenEventHandler += Run;
            
            requestHandler = new RequestHandler(base.cancellationTokenSource);
            requestHandler.HasAccessTokenEventHandler += RequestHandlerHasAccessTokenEventHandler;

            requestHandler.Start();
        }

        private void RequestHandlerHasAccessTokenEventHandler(object sender, RequestHandlers.HasAccessTokenEventArgs e)
        {
            ImisToken = e.ImisToken;            

            if (Authenticate())
            {
                GetPartys();

                Task.Delay(100).Wait();

                GetParty101();

                Task.Delay(100).Wait();

                GetInvoiceSummarys();
            }
        }
               

        public void Run(object sender, AsiCSharpClient.HasAccessTokenEventArgs e) 
        {
            //HasAccessTokenEventHandler += RequestHandlerHasAccessTokenEventHandler;

            ImisToken = e?.ImisToken;

            //imisClient.ImisToken = imisToken;

            if (Authenticate())
            {
                GetPartys();

                Task.Delay(100).Wait();

                GetParty101();

                Task.Delay(100).Wait();

                GetInvoiceSummarys();
            }
        }

        private void OnHasAccessTokenEvent(ImisToken imisToken)
        {
            HasAccessTokenEventHandler?.Invoke(this, new HasAccessTokenEventArgs { ImisToken = imisToken });
        }

        public class HasAccessTokenEventArgs : EventArgs
        {
            public ImisToken ImisToken { get; set; }
        }

        public bool Authenticate()
        {
            try
            {
                var requestWrap = new RequestWrapper();
                requestWrap.HttpMethod = HttpMethod.Post;
                //requestWrap.HttpContent = new FormUrlEncodedContent(new[]
                //{
                //    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                //    new KeyValuePair<string, string>("client_id", "asicsharpclient"), //Config.ImisClientId asicsharpclient
                //    new KeyValuePair<string, string>("client_secret", "demo123"), //Config.ImisClientSecret

                //});

                var formUrlEncodedContent = new[]
                {
                    new KeyValuePair<string, string>("Grant_type", "password"),
                    new KeyValuePair<string, string>("Username", username),
                    new KeyValuePair<string, string>("Password", password),
                };

                if (!string.IsNullOrEmpty(ImisToken?.AccessToken))
                {
                    var tempFormUrlEncodedContent = new KeyValuePair<string, string>[4];
                    for (var i = 0; i < 3; i++)
                        tempFormUrlEncodedContent[i] = formUrlEncodedContent[i];

                    tempFormUrlEncodedContent[4] = new KeyValuePair<string, string>("refresh_token", ImisToken.AccessToken);

                    formUrlEncodedContent = tempFormUrlEncodedContent;
                    //.Add("refresh_token", ImisToken.AccessToken);
                }

                requestWrap.HttpContent = new FormUrlEncodedContent(formUrlEncodedContent);

                //new KeyValuePair<string, string>("refresh_token", ImisToken.AccessToken),
                

                requestWrap.RelativePath = "token";

                var response = SendRestRequest(requestWrap).Result;

                var ceralResult = JsonConvert.SerializeObject(response.Content);

                if (ImisToken == null)
                    ImisToken = new ImisToken();

                ImisToken.AccessToken = response.Content.access_token;
                ImisToken.ExpiresIn = response.Content.expires_in;
                ImisToken.Expires = response.Content.expires;
                ImisToken.TokenType = response.Content.token_type;
                ImisToken.UserName = response.Content.userName;
                ImisToken.Issued = response.Content.issued;               
                

                Console.WriteLine("api/Party response: ================================================================\n\n'" +
                    $"{ceralResult}'\n\n================================================================");

                //Log.Info("api/Party response: ================================================================\n\n'" +
                //    $"{ceralResult}'\n\n================================================================");

                return true;
            }
            catch (Exception e)
            {               
                return false;
            }

        }

        public void GetPartys()
        {
            var requestWrap = new RequestWrapper();
            requestWrap.HttpMethod = HttpMethod.Get;
            requestWrap.AuthenticationHeaderValue = new AuthenticationHeaderValue("Bearer", ImisToken.AccessToken);
            //requestWrap.HttpContent = new StringContent(null);
            //requestWrap.HttpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            requestWrap.RelativePath = "api/Party";



            var response = SendRestRequest(requestWrap).Result;

            var ceralResult = JsonConvert.SerializeObject(response, Formatting.Indented);

            Console.WriteLine("api/Party response: ================================================================\n\n'" +
                $"{ceralResult}'\n\n================================================================");
            //Log.Info("api/Party response: ================================================================\n\n'" +
            //    $"{ceralResult}'\n\n================================================================");
        }

        public void GetParty101()
        {
            var requestWrap = new RequestWrapper();
            requestWrap.HttpMethod = HttpMethod.Get;
            requestWrap.AuthenticationHeaderValue = new AuthenticationHeaderValue("Bearer", ImisToken.AccessToken);
            //requestWrap.HttpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            requestWrap.RelativePath = "api/Party/101";


            var response = base.SendRestRequest(requestWrap).Result;

            var ceralResult = JsonConvert.SerializeObject(response, Formatting.Indented);

            Console.WriteLine("api/Party/101 response: ================================================================\n\n'" +
                $"{ceralResult}'\n\n================================================================");

            //Log.Info("api/Party/101 response: ================================================================\n\n'" +
            //    $"{ceralResult}'\n\n================================================================");
        }

        public void GetInvoiceSummarys()
        {
            var requestWrap = new RequestWrapper();
            requestWrap.HttpMethod = HttpMethod.Get;
            requestWrap.AuthenticationHeaderValue = new AuthenticationHeaderValue("Bearer", ImisToken.AccessToken);
            //requestWrap.HttpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            requestWrap.RelativePath = "api/InvoiceSummary";


            var response = base.SendRestRequest(requestWrap).Result;

            var ceralResult = JsonConvert.SerializeObject(response, Formatting.Indented);

            Console.WriteLine("api/InvoiceSummary response:\n ================================================================\n\n'" +
                $"{ceralResult}'\n\n================================================================");

            //Log.Info("api/InvoiceSummary response:\n ================================================================\n\n'" +
            //    $"{ceralResult}'\n\n================================================================");
        }

        
    }

    public interface IImisClient
    {
        ImisToken ImisToken { get; set; }

        void GetPartys();
        void GetParty101();
        void GetInvoiceSummarys();
        bool Authenticate();
        void Run(object sender, HasAccessTokenEventArgs e);
    }

    public class HasAccessTokenEventArgs : EventArgs
    {
        public ImisToken ImisToken { get; set; }
    }

}

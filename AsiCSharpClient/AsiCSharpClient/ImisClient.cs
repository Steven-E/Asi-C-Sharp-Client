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

        private readonly string username;
        private readonly string password;

        private RequestHandler requestHandler;
        public event EventHandler<HasAccessTokenEventArgs> HasAccessTokenEventHandler;

        public ImisClient(CancellationTokenSource cancellationTokenSource, string targetImisAddress, string appUserName, string appPassword ) : base(cancellationTokenSource, targetImisAddress)
        {
            username = appUserName;
            password = appPassword;

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            httpClient = new HttpClient(clientHandler){ BaseAddress = new Uri(targetImisAddress) };

            //Allows for self-signed certificates
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //Prelim code for Sign-on redirect w/ iMIS
            //requestHandler = new RequestHandler(base.cancellationTokenSource);
            //requestHandler.HasAccessTokenEventHandler += RequestHandlerHasAccessTokenEventHandler;
            //requestHandler.Start();
        }

        //Prelim code for Sign-on redirect w/ iMIS
        //private void RequestHandlerHasAccessTokenEventHandler(object sender, HasAccessTokenEventArgs e)
        //{
        //    ImisToken = e.ImisToken;            

        //    if (Authenticate())
        //    {
        //        GetPartys();

        //        Task.Delay(100).Wait();

        //        GetPartyByPartyId("f9896bb9-1246-4ecc-b540-0486add9fe95");

        //        Task.Delay(100).Wait();

        //        GetInvoiceSummarys();
        //    }
        //}

        public void Run(object sender, HasAccessTokenEventArgs e) 
        {
            ImisToken = e?.ImisToken;

            if (Authenticate())
            {
                GetPartys();

                Task.Delay(100).Wait();

                GetPartyByPartyId("f9896bb9-1246-4ecc-b540-0486add9fe95");

                Task.Delay(100).Wait();

                GetInvoiceSummarys();
            }
        }

        public bool Authenticate()
        {
            try
            {
                var requestWrap = new RequestWrapper() { HttpMethod = HttpMethod.Post };

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
                }

                requestWrap.HttpContent = new FormUrlEncodedContent(formUrlEncodedContent);
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

                Console.WriteLine($"{DateTime.UtcNow} - api/Party response: \n\n'" +
                    $"{ceralResult}'\n\n================================================================");

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Encountered an exception while attempting to athenticated against iMIS instance - ", e);
                return false;
            }
        }

        public void GetPartys()
        {
            var requestWrap = new RequestWrapper();
            
            requestWrap.HttpMethod = HttpMethod.Get;
            requestWrap.AuthenticationHeaderValue = new AuthenticationHeaderValue("Bearer", ImisToken.AccessToken);            

            requestWrap.RelativePath = "api/Party";

            var response = SendRestRequest(requestWrap).Result;

            var ceralResult = JsonConvert.SerializeObject(response, Formatting.Indented);

            Console.WriteLine($"{DateTime.UtcNow} - api/Party response: \n\n'" +
                $"{ceralResult}'\n\n================================================================");
        }

        public void GetPartyByPartyId(string partyId)
        {
            var requestWrap = new RequestWrapper();
            requestWrap.HttpMethod = HttpMethod.Get;
            requestWrap.AuthenticationHeaderValue = new AuthenticationHeaderValue("Bearer", ImisToken.AccessToken);
            requestWrap.RelativePath = "api/Party";
            requestWrap.QueryParameters.Add(new KeyValuePair<string, string>("PartyId", partyId));

            var response = base.SendRestRequest(requestWrap).Result;

            var ceralResult = JsonConvert.SerializeObject(response, Formatting.Indented);

            Console.WriteLine($"{DateTime.UtcNow} - api/Party/ by PartyId QueryParameter response: \n\n'" +
                $"{ceralResult}'\n\n================================================================");
        }

        public void GetInvoiceSummarys()
        {
            var requestWrap = new RequestWrapper();
            requestWrap.HttpMethod = HttpMethod.Get;
            requestWrap.AuthenticationHeaderValue = new AuthenticationHeaderValue("Bearer", ImisToken.AccessToken);
            requestWrap.RelativePath = "api/InvoiceSummary";

            var response = base.SendRestRequest(requestWrap).Result;

            var ceralResult = JsonConvert.SerializeObject(response, Formatting.Indented);

            Console.WriteLine($"{DateTime.UtcNow} - api/InvoiceSummary response:\n\n'" +
                $"{ceralResult}'\n\n================================================================");
        }
    }

    public interface IImisClient
    {
        ImisToken ImisToken { get; set; }

        void GetPartys();
        void GetPartyByPartyId(string partyId);
        void GetInvoiceSummarys();
        bool Authenticate();
        void Run(object sender, HasAccessTokenEventArgs e);
    }
}

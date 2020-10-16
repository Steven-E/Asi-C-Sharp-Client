using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsiCSharpClient
{
    public class Program
    {        
        private static CancellationTokenSource cancellationTokenSource;

        private static IImisClient imisClient;


        public static int Main(string[] args)
        {
            cancellationTokenSource = new CancellationTokenSource();

            imisClient = new ImisClient(cancellationTokenSource);


            Task.Delay(500).Wait();

            imisClient.Run(null, null);
            //Task.Run(async () =>
            // {

            //     var isClientAuthenticated = imisClient.Authenticate();

            //     Console.WriteLine($"Client Succesfully authenticated: {isClientAuthenticated}");

            //     if (isClientAuthenticated)
            //     {
            //         //var cont = Console.ReadKey().Key == ConsoleKey.Enter;

                     

            //         imisClient.GetPartys();

            //         await Task.Delay(1000);

            //         imisClient.GetInvoiceSummarys();

            //         await Task.Delay(1000);

            //         imisClient.GetParty101();

            //         await Task.Delay(1000);
            //     }

            // });

            return ListenForExit().Result;

            ////Console.WriteLine("Hello World!");

            //HttpClientHandler clientHandler = new HttpClientHandler();
            //clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            //clientBase = new ApiClientBase(cancellationTokenSource);

            //httpClient = new HttpClient(clientHandler)
            //{
            //    BaseAddress = new Uri("")//config.TargetImisBaseAddress
            //};

            //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            //httpClient.DefaultRequestHeaders.Accept.Clear();

            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static async Task<int> ListenForExit() 
        {
            await Task.Run(async () =>
             {
                 while (!cancellationTokenSource.IsCancellationRequested)
                 {
                     if (Console.ReadKey().Key == ConsoleKey.Escape)
                     {
                         cancellationTokenSource.Cancel();
                     }

                     await Task.Delay(250).ConfigureAwait(false);
                 }
             });

            return cancellationTokenSource.IsCancellationRequested ? 1067 : 0;
        }
    }
}

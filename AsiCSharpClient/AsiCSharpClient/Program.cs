using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsiCSharpClient
{
    public class Program
    {        
        private static CancellationTokenSource cancellationTokenSource;

        private static IImisClient imisClient;
        private static string appUserName = "";
        private static string appPassword = "";
        private static string targetImisAddress = "";

        public static int Main(string[] args)
        {
            cancellationTokenSource = new CancellationTokenSource();

            imisClient = new ImisClient(cancellationTokenSource, targetImisAddress, appUserName, appPassword);

            Task.Delay(500).Wait();

            imisClient.Run(null, null);

            return ListenForExit().Result;            
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

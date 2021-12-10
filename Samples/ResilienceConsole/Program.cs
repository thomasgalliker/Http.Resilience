using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Http.Resilience;
using Http.Resilience.Internals;

namespace ResilienceConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Http.Resilience Sample Console App");
            Console.WriteLine();

            Http.Resilience.Internals.Logging.Logger.SetLogger(new ConsoleLogger());

            var httpClient = new HttpClient();
            var requestUri = "https://quotes.rest/qod?language=en";

            var httpRetryOptions = new HttpRetryOptions();
            httpRetryOptions.MaxRetries = 4;

            var httpRetryHelper = new HttpRetryHelper(httpRetryOptions);

            try
            {
                var httpResponseMessage = await httpRetryHelper.InvokeAsync(async () => await httpClient.GetAsync(requestUri));
                var jsonContent = await httpResponseMessage.Content.ReadAsStringAsync();

                Console.WriteLine();
                Console.WriteLine($"{jsonContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"{ex.Message}");
            }
            Console.ReadKey();
        }
    }
}

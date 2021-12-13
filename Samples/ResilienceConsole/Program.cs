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

            await Example1();

            Console.ReadKey();
        }

        private static async Task Example1()
        {
            var httpClient = new HttpClient();
            var requestUri = "https://quotes.rest/qod?language=en";

            var httpRetryHelper = new HttpRetryHelper(maxRetries: 3);

            try
            {
                var httpResponseMessage = await httpRetryHelper.InvokeAsync(async () => await httpClient.GetAsync(requestUri));
                var jsonContent = await httpResponseMessage.Content.ReadAsStringAsync();

                Console.WriteLine($"{jsonContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        private static async Task Example2()
        {
            var httpClient = new HttpClient();
            var requestUri = "https://quotes.rest/qod?language=en";

            var httpRetryHelper = new HttpRetryHelper(maxRetries: 3);
            httpRetryHelper.RetryOnException<HttpRequestException>(ex => { return ex.StatusCode == HttpStatusCode.ServiceUnavailable; });

            try
            {
                var httpResponseMessage = await httpRetryHelper.InvokeAsync(async () => await httpClient.GetAsync(requestUri));
                var jsonContent = await httpResponseMessage.Content.ReadAsStringAsync();

                Console.WriteLine($"{jsonContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        private static async Task Example3()
        {
            var httpClient = new HttpClient();
            var requestUri = "https://quotes.rest/qod?language=en";

            var httpRetryOptions = new HttpRetryOptions();
            httpRetryOptions.MaxRetries = 4;

            var httpRetryHelper = new HttpRetryHelper(httpRetryOptions);
            httpRetryHelper.RetryOnException<HttpRequestException>(ex => { return ex.StatusCode == HttpStatusCode.ServiceUnavailable; });

            try
            {
                var httpResponseMessage = await httpRetryHelper.InvokeAsync(async () => await httpClient.GetAsync(requestUri));
                var jsonContent = await httpResponseMessage.Content.ReadAsStringAsync();

                Console.WriteLine($"{jsonContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }
    }
}

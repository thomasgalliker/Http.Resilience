using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Http.Resilience;
using Http.Resilience.Logging;
using ResilienceConsole.Logging;

namespace ResilienceConsole
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Http.Resilience Sample Console App");
            Console.WriteLine();

            Logger.SetLogger(new ConsoleLogger());

            // Example 1:
            // Everything is okay. The first attempts succeeds
            // so that there is no further retry necessary.
            await Example1_OK();

            // Example 2:
            // We configure HTTP status code 404 (NotFound) as a retryable status code
            // and we use a non-existing URL which returns 404 - just to simulate retries.
            await Example2_RetryOnNotFound();

            // Example 3:
            // Retries may also happen if Invoke/InvokeAsync throws an exception.
            // In this case we can configure RetryOnException and specify under what
            // conditions we want to retry the failed attempt.
            await Example3_RetryOnException();

            Console.ReadKey();
        }

        private static async Task Example1_OK()
        {
            var httpClient = new HttpClient();
            var requestUri = "http://worldtimeapi.org/api/timezone/Europe/Zurich";

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

        private static async Task Example2_RetryOnNotFound()
        {
            var httpClient = new HttpClient();
            var requestUri = "http://worldtimeapi.org/not-found";

            var httpRetryHelper = new HttpRetryHelper(maxRetries: 3);
            httpRetryHelper.Options.EnsureSuccessStatusCode = false;
            httpRetryHelper.Options.RetryableStatusCodes.Add(HttpStatusCode.NotFound);

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

        private static async Task Example3_RetryOnException()
        {
            var httpClient = new HttpClient();
            var requestUri = "https://quotes.rest/qod?language=en";

            var httpRetryOptions = new HttpRetryOptions();
            httpRetryOptions.MaxRetries = 4;

            var httpRetryHelper = new HttpRetryHelper(httpRetryOptions);
            httpRetryHelper.RetryOnException<HttpRequestException>(ex => { return ex.StatusCode == HttpStatusCode.Unauthorized; });

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

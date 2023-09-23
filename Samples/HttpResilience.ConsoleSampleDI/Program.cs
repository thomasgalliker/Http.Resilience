using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Http.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    private static async Task Main(string[] _)
    {
        Console.WriteLine($"HttpResilience.ConsoleSampleDI [Version 1.0.0.0]");
        Console.WriteLine($"(c)2023 superdev gmbh. All rights reserved.");
        Console.WriteLine();

        // Create DI container and register services
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        // Use code-based configuration:
        serviceCollection.AddHttpResilience(o =>
        {
            o.MaxRetries = 2;
        });

        // Use configuration from appSettings.json:
        //var configuration = new ConfigurationBuilder()
        //    .SetBasePath(Directory.GetCurrentDirectory())
        //    .AddJsonFile("appsettings.json", true, true)
        //    .Build();

        //var configurationSection = configuration.GetSection("HttpResilience");
        //serviceCollection.AddHttpResilience(configurationSection);

        // Example 1:
        // Everything is okay. The first attempts succeeds
        // so that there is no further retry necessary.
        await Example1_OK(serviceCollection.BuildServiceProvider());

        Console.ReadKey();
    }

    private static async Task Example1_OK(IServiceProvider serviceProvider)
    {
        var httpClient = new HttpClient();
        var requestUri = "http://worldtimeapi.org/api/timezone/Europe/Zurich";

        // Resolve services from DI container
        var httpRetryHelper = serviceProvider.GetRequiredService<IHttpRetryHelper>();

        httpRetryHelper.RetryOnException<HttpRequestException>(ex =>
        {
            return ex.StatusCode == HttpStatusCode.ServiceUnavailable;
        });

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
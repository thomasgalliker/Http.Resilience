# Http.Resilience
[![Version](https://img.shields.io/nuget/v/Http.Resilience.svg)](https://www.nuget.org/packages/Http.Resilience)  [![Downloads](https://img.shields.io/nuget/dt/Http.Resilience.svg)](https://www.nuget.org/packages/Http.Resilience)

<img src="https://raw.githubusercontent.com/thomasgalliker/Http.Resilience/develop/logo.png" width="100" height="100" alt="Http.Resilience" align="right"></img>

Http.Resilience adds fault tolerance to any HTTP request. It can be used together with C# HttpClient, WebRequest or any other HTTP client. Transient network failures are automatically catched and a configurable number of retries is issued.

### Download and Install Http.Resilience
This library is available on NuGet: https://www.nuget.org/packages/Http.Resilience
Use the following command to install Http.Resilience using NuGet package manager console:

    PM> Install-Package Http.Resilience

You can use this library in any .NET Standard or .NET Core project.

### API Usage
#### Recover from transient network failure
The following sample demonstrates a simple HTTP request using HttpClient. HttpRetryHelper is used to wrap httpClient.GetAsync(...). Whenever GetAsync(...) fails due to a transilient network failure, HttpRetryHelper attempts to recover the problem by repeatedly calling InvokeAsync.
```C#
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
```

#### Recover from unsuccessful HTTP status code
Retries can be configured using the RetryOnException delegate. If Invoke/Async throws an exception, we can intercept it with RetryOnException((ex) => ...) and return a bool value to indicate whether we want to retry the particular HTTP request (true=retry, false=do not retry).
```C#
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
```

### License
This project is Copyright &copy; 2021 [Thomas Galliker](https://ch.linkedin.com/in/thomasgalliker). Free for non-commercial use. For commercial use please contact the author.

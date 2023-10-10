using consumer.TypedHttpClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        var currentDirectory = hostingContext.HostingEnvironment.ContentRootPath;

        config.SetBasePath(currentDirectory)
            .AddJsonFile("settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
        config.Build();
    })
    .ConfigureServices((services) =>
    {
        var circuitBreakPolicy = Policy
            .HandleResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
            .CircuitBreakerAsync(3, 
                TimeSpan.FromSeconds(15),
                onBreak: (_, _) =>
                {
                    Console.Out.WriteLine("*****Open*****");
                },
                onReset: () =>
                {
                    Console.Out.WriteLine("*****Closed*****");
                },
                onHalfOpen: () =>
                {
                    Console.Out.WriteLine("*****Half Open*****");
                });

        services.AddHttpClient<StateCounterHttpClient>()
            .AddPolicyHandler(circuitBreakPolicy);
    })
    .Build();

host.Run();

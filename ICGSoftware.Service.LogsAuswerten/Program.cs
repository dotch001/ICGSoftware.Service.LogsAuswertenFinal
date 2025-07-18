using ICGSoftware.FilterErrorsAndAskAI;
using ICGSoftware.GetAppSettings;
using ICGSoftware.LogHandeling;
using ICGSoftware.Service;

class Program
{
    public static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        await Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((hostingContext, services) =>
            {
                // Register settings
                services.Configure<AppSettingsClassDev>(
                    hostingContext.Configuration.GetSection("AppSettings"));

                services.Configure<AppSettingsClassConf>(
                    hostingContext.Configuration.GetSection("AuthenticationSettings"));

                // Register the custom class for DI
                services.AddTransient<FilterErrAndAskAI>();
                services.AddTransient<Logging>();

                // Register the background worker
                services.AddHostedService<Worker>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build()
            .RunAsync(cts.Token);
    }
}

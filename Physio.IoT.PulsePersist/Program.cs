using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using System.Reflection;
using Physio.IoT.PulsePersist.Configuration;
using Physio.IoT.PulsePersistPhysio.IoT.PulsePersist.Configuration;
using Physio.IoT.PulsePersist.HostedServices;

class Program
{
    private static readonly string AppVersion = Attribute.GetCustomAttributes(Assembly.GetExecutingAssembly(), typeof(AssemblyInformationalVersionAttribute))
        .Cast<AssemblyInformationalVersionAttribute>()
        .Select(x => x.InformationalVersion)
        .First();

    private static readonly Assembly assembly = Assembly.GetEntryAssembly()!;
    public static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine(@"

  ____        _          ____               _     _   
 |  _ \ _   _| |___  ___|  _ \ ___ _ __ ___(_)___| |_ 
 | |_) | | | | / __|/ _ \ |_) / _ \ '__/ __| / __| __|
 |  __/| |_| | \__ \  __/  __/  __/ |  \__ \ \__ \ |_ 
 |_|    \__,_|_|___/\___|_|   \___|_|  |___/_|___/\__|
                                                      
");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen, outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("logs/mqttAdapter.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .CreateLogger();

            var configUri = new Uri(args.FirstOrDefault() ?? Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
            Log.Information($"Starting {assembly.GetName().Name} version {AppVersion} with configuration coming from {configUri}");

            AppConfig appConfig = await ConfigurationLoader.LoadAsync(configUri);
            IHostBuilder hostBuilder = CreateHostBuilder(appConfig);

            CreateHostBuilder(appConfig).Build().Run();

        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(AppConfig appConfig)
    {
        return new HostBuilder()
            .UseDefaultServiceProvider(provider =>
            {
                provider.ValidateScopes = true;
                provider.ValidateOnBuild = true;
            })
            .ConfigureLogging(logging => { logging.ClearProviders(); })
            .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
                    .Enrich.FromLogContext()
                    .MinimumLevel.Verbose()
            )
            .ConfigureServices(services =>
            {
                services.AddSingleton(appConfig);
                services.AddSingleton<ILoggerFactory, LoggerFactory>();
                services.AddLogging(x =>
                {
                    x.ClearProviders();
                    x.AddSerilog(dispose: true);
                });
                services.AddHostedService<RabbitMqConsumerService>();
            })
            .UseConsoleLifetime(console =>
            {
                console.SuppressStatusMessages = true;
            });
    }


}
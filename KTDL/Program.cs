using KTDL;
using KTDL.Orchestrator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("config/config.json", optional: false, reloadOnChange: true)
    .AddJsonFile("config/secrets.json", optional: true, reloadOnChange: true);

IConfiguration configuration = builder.Build();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        theme: AnsiConsoleTheme.Code,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/app.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog();
});
var logger = loggerFactory.CreateLogger<Program>();

try
{
    BotClient client;
    client = new BotClient(loggerFactory, configuration);
    client.Connect().Wait();

    JobPipelineOrchestrator orchestrator = new JobPipelineOrchestrator(loggerFactory, configuration);
    orchestrator.StartWorkflow();

    BotController botController = new BotController(loggerFactory, configuration, client.TelegramBot, orchestrator);

    logger.LogInformation($"Bot has been launched");

    // TODO: Implement better shutdown handling
    while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }

    logger.LogInformation($"Bot shutdown started...");

    orchestrator.StopWorkflow().Wait();
    client.Close().Wait();

    logger.LogInformation($"Closing logs");
    Log.CloseAndFlush();
}
catch(Exception e)
{
    logger.LogError(e.Message);
}
using KTDL;
using KTDL.Orchestrator;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("config/config.json", optional: false, reloadOnChange: true)
    .AddJsonFile("config/secrets.json", optional: true, reloadOnChange: true);

IConfiguration configuration = builder.Build();

JobPipelineOrchestrator orchestrator = new JobPipelineOrchestrator(configuration);

BotClient client = new BotClient(configuration);
client.Connect().Wait();

orchestrator.StartWorkflow();

BotController botController = new BotController(configuration, orchestrator, client.TelegramBot);

Console.WriteLine("___________________________________________________\n");
Console.WriteLine("I'm listening now. Send me a command. Or press Escape to exit");

// TODO: Implement better shutdown handling
while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
Console.WriteLine("Exiting...");

orchestrator.StopWorkflow().Wait();
client.Close().Wait();


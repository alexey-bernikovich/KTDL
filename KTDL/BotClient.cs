using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;


namespace KTDL
{
    internal class BotClient
    {
        private readonly ILogger<BotClient> _logger;
        private readonly IConfiguration _configuration;

        public WTelegram.Bot TelegramBot;

        private StreamWriter _wTelegramLogs;
        private SqliteConnection _sqliteConnection;


        public BotClient(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<BotClient>();
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // TODO: Get from config file if I want any WTelegram logs
            _wTelegramLogs = new StreamWriter(@"logs\WTelegramBot.log", true, Encoding.UTF8) { AutoFlush = true };
            WTelegram.Helpers.Log = (lvl, str) => _wTelegramLogs.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{"TDIWE!"[lvl]}] {str}");
        }

        public async Task Connect()
        {
            if (!int.TryParse(_configuration["ApiId"], out int apiId))
            {
                throw new ArgumentException("Telegram API ID is not set nor not a valid integer");
            }
            string apiHash = _configuration["ApiHash"] 
                ?? throw new ArgumentException("Telegram API Hash is not set");
            string botToken = _configuration["BotToken"] 
                ?? throw new ArgumentException("Telegram Bot Token is not set");

            _sqliteConnection = new SqliteConnection(@"Data Source=WTelegramBot.sqlite");
            TelegramBot = new WTelegram.Bot(botToken, apiId, apiHash, _sqliteConnection);

            // TODO: set from the config file
            await TelegramBot.DropPendingUpdates();
            TelegramBot.WantUnknownTLUpdates = true;

        }

        public async Task Close()
        {
            _logger.LogInformation($"Closing Telegram Bot client.");
            await TelegramBot.Close();
            _logger.LogInformation($"Closing SQLite connectionm.");
            await _sqliteConnection.CloseAsync();
            _logger.LogInformation($"Closing Telegram logs.");
            _wTelegramLogs.Close();            
        }
    }
}

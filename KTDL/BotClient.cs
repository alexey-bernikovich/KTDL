using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.Data.Sqlite;


namespace KTDL
{
    internal class BotClient
    {
        private readonly IConfiguration _configuration;

        public WTelegram.Bot TelegramBot;

        private StreamWriter _wTelegramLogs;
        private SqliteConnection _sqliteConnection;


        public BotClient(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _wTelegramLogs = new StreamWriter("WTelegramBot.log", true, Encoding.UTF8) { AutoFlush = true };
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

            await TelegramBot.DropPendingUpdates();
            TelegramBot.WantUnknownTLUpdates = true;
        }

        public async Task Close()
        {
            await TelegramBot.Close();
            await _sqliteConnection.CloseAsync();
            _wTelegramLogs.Close();            
        }
    }
}

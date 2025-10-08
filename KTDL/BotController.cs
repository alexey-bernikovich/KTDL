using KTDL.Common;
using KTDL.Orchestrator;
using KTDL.Pipeline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace KTDL
{
    internal class BotController
    {
        private readonly ILogger<BotController> _logger;        
        private IConfiguration _configuration;
        private WTelegram.Bot _bot;
        private JobPipelineOrchestrator _orchestrator;

        public BotController(ILoggerFactory loggerFactory, IConfiguration configuration,
            WTelegram.Bot bot, JobPipelineOrchestrator orchestrator)
        {
            _logger = loggerFactory.CreateLogger<BotController>();
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _bot = bot;
            _orchestrator = orchestrator;            

            _bot.OnMessage += OnMessage;
            _bot.OnError += OnError;            
        }

        // TODO: Make it in the right way
        private async Task TesLinkHandle(WTelegram.Types.Message msg, string url)
        {
            _logger.LogInformation("Recived task for {Username} - {Url}.", msg.From.Username, url);
            var context = new PipelineContext
            {
                UserId = msg.From.Id,
                ChatId = msg.Chat.Id,

                OnProgress = async (status) =>
                {
                    _logger.LogInformation($"{status}.");
                    //await _bot.SendMessage(msg.Chat, status, replyParameters: msg);
                },

                OnCompleted = async (result) =>
                {
                    if (result.Success && result.Data.TryGetValue(
                        PipelineContextDataNames.ARCHIVE_PATH, out var archivePathObj))
                    {
                        var archivePath = archivePathObj as string;

                        string? albumTitle = string.Empty;
                        if (result.Data.ContainsKey(PipelineContextDataNames.ALBUM_TITLE))
                        {
                            albumTitle = result.Data[PipelineContextDataNames.ALBUM_TITLE].ToString();
                        }

                        string? albumYear = string.Empty;
                        if (result.Data.ContainsKey(PipelineContextDataNames.ALBUM_YEAR))
                        {
                            albumYear = result.Data[PipelineContextDataNames.ALBUM_YEAR].ToString();
                        }

                        string? imagePath = string.Empty;
                        if (result.Data.ContainsKey(PipelineContextDataNames.ALBUM_COVER))
                        {
                            imagePath = result.Data[PipelineContextDataNames.ALBUM_COVER].ToString();
                        }

                        _logger.LogInformation("Sending an archive {Path} to {Username}...", archivePath, msg.From.Username);
                        await SendFileAsync(_bot, msg, archivePath,
                            albumTitle, albumYear, imagePath);
                        _logger.LogInformation("Archive {Path} was sent to {Username}!", archivePath, msg.From.Username);
                    }
                }
            };

            await _orchestrator.InitJob(
                context,
                url
                //@"https://downloads.khinsider.com/game-soundtracks/album/city-life-windows-gamerip-2008"
                //@"https://downloads.khinsider.com/game-soundtracks/album/marc-ecko-s-getting-up-contents-under-pressure-original-soundtrack-2006"
                //@"https://downloads.khinsider.com/game-soundtracks/album/logo-commodore-64"
                );
            //await _bot.SendMessage(msg.Chat, "Job started. You can send /cancel to stop it.", replyParameters: msg);
            await _bot.SendMessage(msg.Chat, "Job started. Please wait :)", replyParameters: msg);
        }

        // TODO: double check the reults of asynchronous + job manager
        private async Task OnMessage(WTelegram.Types.Message msg, UpdateType type)
        {
            if(msg is null)
            {
                throw new Exception("Received message obj is null");
            }

            _logger.LogInformation("Recived message from {Username}.", msg.From.Username);

            if (msg.Text is null)
            {
                throw new Exception("Received text is null");
            }

            _logger.LogInformation("Message: {Text}.", msg.Text); 

            var text = msg.Text.ToLower();

            switch (text)
            {
                case "/start":
                    // temporarily
                    await _bot.SendMessage(msg.Chat, $"Hello there, {msg.From.Username}");
                    break;

                case var url when url.StartsWith("https://downloads.khinsider.com/game-soundtracks/album/"):                    
                    await TesLinkHandle(msg, url);
                    break;

                default:
                    // temporarily
                    await _bot.SendMessage(msg.Chat, "Nope");
                    break;
            }           
        }

        private async Task SendFileAsync(WTelegram.Bot bot, Telegram.Bot.Types.Message msg, 
            string path, string title, string year, string albumCover)
        {
            await using var stream = File.OpenRead(path);

            string fullTitle = "Album";
            if(!string.IsNullOrEmpty(title))
            {
                fullTitle = title;
                if(!string.IsNullOrEmpty(year))
                {
                    fullTitle += $"\n{year}";
                }
            }

            // TODO: rewrite it
            if (!string.IsNullOrEmpty(albumCover))
            {
                await using var thumgnalStream = File.OpenRead(albumCover);
                await bot.SendDocument(
                    msg.Chat,
                    stream,
                    fullTitle,
                    thumbnail: thumgnalStream,
                    replyParameters: msg);
            }
            else
            {
                await bot.SendDocument(
                    msg.Chat,
                    stream,
                    fullTitle,
                    replyParameters: msg);
            }
        }

        private Task OnError(Exception exception, HandleErrorSource errorSource)
        {
            _logger.LogError(exception.Message);
            return Task.CompletedTask;
        }
    }
}

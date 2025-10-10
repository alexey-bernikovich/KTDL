using KTDL.Common;
using KTDL.Common.StringConst;
using KTDL.Executors;
using KTDL.Pipeline;
using Microsoft.Extensions.Logging;

namespace KTDL.Steps
{
    internal class DownloadStep : IPipelineStep
    {
        private readonly ILogger<SimpleFileDownloader> _logger;
        private readonly IFileDownloader _downloader;

        public DownloadStep(ILoggerFactory loggerFactory, IFileDownloader donwloader)
        {
            _logger = loggerFactory.CreateLogger<SimpleFileDownloader>();
            _downloader = donwloader ?? throw new ArgumentNullException(nameof(donwloader));
        }

        public async Task ExecuteAsync(PipelineContext context)
        {
            var url = context.Data[PipelineContextDataNames.URL] as string;

            if (context.OnProgress != null)
            {
                await context.OnProgress(new ProgressInfo 
                { 
                    Message = UserNotificationMessages.DOWNLOAD_STARTED,
                    Stage = PipelineStepStage.Initialized
                });
            }

            _logger.LogInformation($"Calling download album files method.");
            var files = await _downloader.DownloadAlbumFilesAsync(
                url,
                context.TempDirectory,
                async (donwloaded, total) =>
                {                    
                    if (context.OnProgress != null)
                    {
                        await context.OnProgress(new ProgressInfo
                        {
                            Message = UserNotificationMessages.DOWNLOAD_PROCESS,
                            Stage = PipelineStepStage.Executing,
                            Processed = donwloaded,
                            Total = total
                        });
                    }
                },
                context.CancellationToken);

            // TO-DO: Check if files is null
            context.Data[PipelineContextDataNames.DOWNLOADED_FILES] = files;

            _logger.LogInformation($"Calling get album cover method.");
            var albumCover = await _downloader.GetAlbumCoverAsync(
                url,
                context.TempDirectory,
                context.CancellationToken);

            if (albumCover is not null)
            {
                context.Data[PipelineContextDataNames.ALBUM_COVER] = albumCover;
            }

            _logger.LogInformation($"Calling download album info method.");
            var infoDict = await _downloader.GetAlbumInfoAsync(
                url,
                context.CancellationToken);

            MapTwoDict(context.Data, infoDict);

            if (context.OnProgress != null)
            {
                await context.OnProgress(new ProgressInfo
                {
                    Message = UserNotificationMessages.DOWNLOAD_FINISHED,
                    Stage = PipelineStepStage.Completed,
                });
            }
        }

        private void MapTwoDict(Dictionary<string, object> first, Dictionary<string, string> second)
        {
            foreach (var kv in second)
            {
                first[kv.Key] = kv.Value.ToString();
            }
        }
    }
}

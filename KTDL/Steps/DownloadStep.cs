using KTDL.Common;
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
                await context.OnProgress("Starting download...");
            }

            _logger.LogInformation($"Calling download album files method.");
            var files = await _downloader.DownloadAlbumFilesAsync(
                url,
                context.TempDirectory,
                async (donwloaded, total, itemType) =>
                {
                    _logger.LogInformation("Downloaded {File} of {Total} {Type} file(s) ({Percent}%)",
                            donwloaded, total, itemType, (total > 0 ? (donwloaded * 100 / total) : 0));
                    if (context.OnProgress != null)
                    {
                        // TODO: Implement user inform                                 
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
                async (donwloaded, total, itemType) =>
                {
                    if (context.OnProgress != null)
                    {
                        // TODO: Implement user inform
                    }
                },
                context.CancellationToken);

            MapTwoDict(context.Data, infoDict);
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

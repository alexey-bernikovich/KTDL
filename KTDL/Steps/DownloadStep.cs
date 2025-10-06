using KTDL.Common;
using KTDL.Executors;
using KTDL.Pipeline;
using System;
using System.Collections.Generic;
using System.Text;

namespace KTDL.Steps
{
    internal class DownloadStep : IPipelineStep
    {
        private readonly IFileDownloader _downloader;

        public DownloadStep() : this(new SimpleFileDownloader()) { }

        public DownloadStep(IFileDownloader donwloader)
        {
            _downloader = donwloader ?? throw new ArgumentNullException(nameof(donwloader));
        }

        public async Task ExecuteAsync(PipelineContext context)
        {
            var url = context.Data[PipelineContextDataNames.URL] as string;

            if (context.OnProgress != null)
            {
                await context.OnProgress("Starting download...");
            }

            var files = await _downloader.DownloadAlbumFilesAsync(
                url,
                context.TempDirectory,
                async (donwloaded, total, itemType) =>
                {
                    if (context.OnProgress != null)
                    {
                        await context.OnProgress($"Downloaded {donwloaded} of " +
                            $"{total} {itemType} file(s) ({(total > 0 ? (donwloaded * 100 / total) : 0)}%)");
                    }
                },
                context.CancellationToken);

            // TO-DO: Check if files is null
            context.Data[PipelineContextDataNames.DOWNLOADED_FILES] = files;

            var albumCover = await _downloader.GetAlbumCoverAsync(
                url,
                context.TempDirectory,
                context.CancellationToken);

            if (albumCover is not null)
            {
                context.Data[PipelineContextDataNames.ALBUM_COVER] = albumCover;
            }

            var infoDict = await _downloader.GetAlbumInfoAsync(
                url,
                async (donwloaded, total, itemType) =>
                {
                    if (context.OnProgress != null)
                    {
                        await context.OnProgress($"Downloaded {donwloaded} of " +
                            $"{total} {itemType} ({(total > 0 ? (donwloaded * 100 / total) : 0)}%)");
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

using System;
using System.Collections.Generic;
using System.Text;
using KTDL.Common;
using KTDL.Executors;
using KTDL.Pipeline;
using Microsoft.Extensions.Logging;

namespace KTDL.Steps
{
    internal class ArchiveStep : IPipelineStep
    {
        private readonly ILogger<ArchiveStep> _logger;
        private readonly IArchiver _archiver;

        public ArchiveStep(ILoggerFactory loggerFactory, IArchiver archiver)
        {
            _logger = loggerFactory.CreateLogger<ArchiveStep>();
            _archiver = archiver ?? throw new ArgumentNullException(nameof(archiver));
        }

        public async Task ExecuteAsync(PipelineContext context)
        {
            if(context.OnCompleted != null)
            {
                await context.OnProgress("Archiving files...");
            }

            var archiveName = $"{context.WorkflowId}.zip";
            if (context.Data.ContainsKey(PipelineContextDataNames.ALBUM_TITLE))
            {
                archiveName = $"{context.Data[PipelineContextDataNames.ALBUM_TITLE]}.zip";
            }

            var archivePath = await _archiver.CreateArchiveAsync(
                context.TempDirectory, 
                archiveName, 
                context.CancellationToken);

            context.Data[PipelineContextDataNames.ARCHIVE_PATH] = archivePath;
        }
    }
}

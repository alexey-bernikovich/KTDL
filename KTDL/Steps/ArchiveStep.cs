using KTDL.Common;
using KTDL.Common.StringConst;
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
            if (context.OnProgress != null)
            {
                await context.OnProgress(new ProgressInfo
                {
                    Message = UserNotificationMessages.ARCHIVING_STARTED,
                    Stage = PipelineStepStage.Initialized
                });
            }

            var archiveName = $"{context.WorkflowId}.zip";
            if (context.Data.ContainsKey(PipelineContextDataNames.ALBUM_TITLE))
            {
                archiveName = $"{context.Data[PipelineContextDataNames.ALBUM_TITLE]}.zip";
            }

            var archivePath = await _archiver.CreateArchiveAsync(
                context.TempDirectory, 
                archiveName,
                async (archived, total) =>
                {
                    _logger.LogInformation("Archived {File} of {Total} file(s) ({Percent}%)",
                            archived, total, (total > 0 ? (archived * 100 / total) : 0));
                    if (context.OnProgress != null)
                    {
                        await context.OnProgress(new ProgressInfo
                        {
                            Message = UserNotificationMessages.ARCHIVING_PROCESS,
                            Stage = PipelineStepStage.Executing,
                            Processed = archived,
                            Total = total
                        });
                    }
                },
                context.CancellationToken);

            context.Data[PipelineContextDataNames.ARCHIVE_PATH] = archivePath;

            if (context.OnProgress != null)
            {
                await context.OnProgress(new ProgressInfo
                {
                    Message = UserNotificationMessages.ARCHIVING_FINISHED,
                    Stage = PipelineStepStage.Completed,
                });
            }
        }
    }
}

using KTDL.Executors;
using KTDL.Pipeline;
using KTDL.Jobs;
using KTDL.Steps;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using Microsoft.VisualBasic;
using KTDL.Common;
using Microsoft.Extensions.Logging;


namespace KTDL.Orchestrator
{
    internal class JobPipelineOrchestrator
    {
        private readonly ILogger<JobPipelineOrchestrator> _logger;
        private IConfiguration _configuration;        
        private JobManager _jobManager;
        private JobPipeline _jobPipeline;
        private ConcurrentDictionary<long, CancellationTokenSource> _userCancellations;

        public JobPipelineOrchestrator(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = loggerFactory.CreateLogger<JobPipelineOrchestrator>();
            _userCancellations = new ConcurrentDictionary<long, CancellationTokenSource>();            

            var downloader = new SimpleFileDownloader(loggerFactory);
           // var processor = new SimpleFileProcessor();
            var archiver = new SimpleArchiver(loggerFactory);

            _jobPipeline = new JobPipeline(loggerFactory)
                .AddStep(() => new DownloadStep(loggerFactory, downloader))
                //.AddStep(() => new ProcessFilesStep(processor))
                .AddStep(() => new ArchiveStep(loggerFactory, archiver));

            // TODO: Get that number from config file
            _jobManager = new JobManager(5);
        }

        public CancellationTokenSource AddUserCancellation(long userId)
        {            
            var cancellationToken = _userCancellations[userId] = new CancellationTokenSource();
            _logger.LogInformation("Add used cancellation {UserId}", userId);
            return cancellationToken;
        }

        public void TryRemoveUserCancellation(long userId)
        {
            if (_userCancellations.TryRemove(userId, out var cts))
            {
                cts.Dispose();
                _logger.LogInformation("Removed used cancellation {UsedId}", userId);
            }
        }

        public async Task InitJob(PipelineContext context, string url)
        {
            Guid workflowId = Guid.NewGuid();
            _logger.LogInformation("Initialization job {JobId}.", workflowId);
            //TODO: Add temp path from config file
            var tempDir = Path.Combine("temp", workflowId.ToString());
            Directory.CreateDirectory(tempDir);

            context.WorkflowId = workflowId;
            context.TempDirectory = tempDir;
            context.CancellationToken = AddUserCancellation(context.UserId).Token;
            context.Data = new Dictionary<string, object>
            {
                ["Url"] = url
            };
            context.OnFinished += async (userId, result) =>
            {
                TryRemoveUserCancellation(userId);

                // TODO: Check if archive even exists
                result.Data.TryGetValue(PipelineContextDataNames.ARCHIVE_PATH, out var archivePathObj);
                var archivePath = archivePathObj as string;

                File.Delete(archivePath);
                _logger.LogInformation("Finished job {JobId}", context.WorkflowId);
            };

            await _jobManager.EnqueueJob(async () =>
            {
                await _jobPipeline.ExecuteAsync(context);
            });
        }

        public void StartWorkflow()
        {
            _jobManager.Start();
        }

        public async Task StopWorkflow()
        {
            await _jobManager.StopAsync();
        }
    }
}

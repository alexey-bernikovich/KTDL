using KTDL.Executors;
using KTDL.Pipeline;
using KTDL.Jobs;
using KTDL.Steps;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using Microsoft.VisualBasic;


namespace KTDL.Orchestrator
{
    internal class JobPipelineOrchestrator
    {
        private IConfiguration _configuration;
        private JobManager _jobManager;
        private JobPipeline _jobPipeline;
        private ConcurrentDictionary<long, CancellationTokenSource> _userCancellations;

        public JobPipelineOrchestrator(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _userCancellations = new ConcurrentDictionary<long, CancellationTokenSource>();

            var downloader = new SimpleFileDownloader();
            var processor = new SimpleFileProcessor();
            var archiver = new SimpleArchiver();

            _jobPipeline = new JobPipeline()
                .AddStep(() => new DownloadStep(downloader))
                //.AddStep(() => new ProcessFilesStep(processor))
                .AddStep(() => new ArchiveStep(archiver));

            _jobManager = new JobManager(workerCount: 5);
        }

        public CancellationTokenSource AddUserCancellation(long userId)
        {
            return _userCancellations[userId] = new CancellationTokenSource();
        }

        public void TryRemoveUserCancellation(long userId)
        {
            if (_userCancellations.TryRemove(userId, out var cts))
            {
                cts.Dispose();
            }
        }

        public async Task InitJob(PipelineContext context, string url)
        {
            Guid workflowId = Guid.NewGuid();
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
                result.Data.TryGetValue("ArchivePath", out var archivePathObj);
                var archivePath = archivePathObj as string;

                File.Delete(archivePath);
                Console.WriteLine($"Finished {context.WorkflowId}");
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

using Microsoft.Extensions.Logging;

namespace KTDL.Pipeline
{
    internal class JobPipeline
    {
        private readonly ILogger<JobPipeline> _logger;
        private readonly List<Func<IPipelineStep>> _stepsFactories = new List<Func<IPipelineStep>>();

        public JobPipeline(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<JobPipeline>();
        }

        public JobPipeline AddStep<TStep>() where TStep : IPipelineStep, new()
        {
            _stepsFactories.Add(() => new TStep());
            return this;
        }
        
        public JobPipeline AddStep(Func<IPipelineStep> stepFactory)
        {
            _stepsFactories.Add(stepFactory);
            return this;
        }

        public async Task<PipelineResult> ExecuteAsync(PipelineContext pipelineContext)
        {
            _logger.LogInformation("Starting job {JobId}.", pipelineContext.WorkflowId);
            var result = new PipelineResult { Success = true };

            try
            {
                foreach (var factory in _stepsFactories)
                {
                    pipelineContext.CancellationToken.ThrowIfCancellationRequested();

                    var step = factory();
                    await step.ExecuteAsync(pipelineContext);
                }

                result.Data = pipelineContext.Data;

                // Call callback if everything ok
                if (pipelineContext.OnCompleted != null)
                {
                    await pipelineContext.OnCompleted(result);
                    await pipelineContext.OnFinished(pipelineContext.UserId, result);
                }
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.ErrorMessage = "Operation was cancelled.";

                if (pipelineContext.OnProgress != null)
                {
                    await pipelineContext.OnProgress("Operation was cancelled.");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;

                if (pipelineContext.OnProgress != null)
                {
                    await pipelineContext.OnProgress($"Error: {ex.Message}");
                }
            }
            finally // cleanup
            {
                CleanupTempDirectory(pipelineContext.TempDirectory);
            }
            return result;
        }

        private void CleanupTempDirectory(string tempDirectory)
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                    _logger.LogInformation("Deleted {Folder}.", tempDirectory);
                }
            }
            catch
            {
                _logger.LogError("Error occurred while trying to delete {Folder} folder.", tempDirectory);
            }
        }
    }
}

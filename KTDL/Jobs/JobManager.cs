using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace KTDL.Jobs
{
    internal class JobManager
    {
        private readonly Channel<Func<Task>> _jobQueue;
        private readonly int _workerCount; // Number of concurrent workers
        private readonly List<Task> _workers;
        private readonly CancellationTokenSource _shutdownToken;

        public JobManager(int workerCount = 5)
        {
            _workerCount = workerCount;
            _jobQueue = Channel.CreateUnbounded<Func<Task>>();
            _workers = new List<Task>();
            _shutdownToken = new CancellationTokenSource();
        }

        public void Start()
        {
            for (int i = 0; i < _workerCount; i++)
            {
                var workerTask = Task.Run(() => WorkerLoop(_shutdownToken.Token));
                _workers.Add(workerTask);
            }
        }

        public async Task StopAsync()
        {
            _shutdownToken.Cancel();
            _jobQueue.Writer.Complete();
            await Task.WhenAll(_workers);
        }
        // Enqueue a job to be processed
        public async Task EnqueueJob(Func<Task> job)
        {
            await _jobQueue.Writer.WriteAsync(job);
        }

        // Main worker loop
        private async Task WorkerLoop(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var job in _jobQueue.Reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        await job();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Job failed: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
        }
    }
}

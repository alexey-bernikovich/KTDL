using System;
using System.Collections.Generic;
using System.Text;

namespace KTDL.Pipeline
{
    internal class PipelineContext
    {
        public Guid WorkflowId { get; set; }
        public long UserId { get; set; } // Telegram UserId
        public long ChatId { get; set; } // Telegram ChatId
        public string TempDirectory { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        public Func<string, Task> OnProgress { get; set; }
        public Func<PipelineResult, Task> OnCompleted { get; set; }
        public Func<long, PipelineResult, Task> OnFinished { get; set; }
    }
}

using KTDL.Executors;
using KTDL.Pipeline;
using System;
using System.Collections.Generic;
using System.Text;

namespace KTDL.Steps
{
    internal class ArchiveStep : IPipelineStep
    {
        private readonly IArchiver _archiver;

        public ArchiveStep() : this(new SimpleArchiver()) { }

        public ArchiveStep(IArchiver archiver)
        {
            _archiver = archiver ?? throw new ArgumentNullException(nameof(archiver));
        }

        public async Task ExecuteAsync(PipelineContext context)
        {
            if(context.OnCompleted != null)
            {
                await context.OnProgress("Archiving files...");
            }

            var archiveName = $"{context.WorkflowId}.zip";
            if (context.Data.ContainsKey("AlbumTitle"))
            {
                archiveName = $"{context.Data["AlbumTitle"]}.zip";
            }
                
            var archivePath = await _archiver.CreateArchiveAsync(
                context.TempDirectory, 
                archiveName, 
                context.CancellationToken);

            context.Data["ArchivePath"] = archivePath;
        }
    }
}

using KTDL.Common;
using KTDL.Executors;
using KTDL.Pipeline;
using System;
using System.Collections.Generic;
using System.Text;

namespace KTDL.Steps
{
    internal class ProcessFilesStep : IPipelineStep
    {
        private readonly IFileProcessor _fileProcessor;

        public ProcessFilesStep() : this(new SimpleFileProcessor()) { }

        public ProcessFilesStep(IFileProcessor fileProcessor)
        {
            _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
        }

        public async Task ExecuteAsync(PipelineContext context)
        {
            var files = context.Data[PipelineContextDataNames.DOWNLOADED_FILES] as List<string>;

            if(files == null || files.Count == 0)
            {
                throw new InvalidOperationException("No files to process");
            }

            if(context.OnProgress != null)
            {
                await context.OnProgress("Processing files...");
            }

            int processedCount = 0;
            foreach (var file in files)
            {
                //await _fileProcessor.ProccessFileAsync(file, imgaeData, context.CancellationToken);
                processedCount++;

                if (context.OnProgress != null)
                {
                    await context.OnProgress($"Processed {processedCount}/{files.Count} files.");
                }
            }
        }
    }
}

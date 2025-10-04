using System;
using System.Collections.Generic;
using System.Text;

namespace KTDL.Pipeline
{
    internal interface IPipelineStep
    {
        Task ExecuteAsync(PipelineContext context);
    }
}

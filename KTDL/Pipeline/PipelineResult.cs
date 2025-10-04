using System;
using System.Collections.Generic;
using System.Text;

namespace KTDL.Pipeline
{
    internal class PipelineResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}

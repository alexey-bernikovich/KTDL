namespace KTDL.Common
{
    internal class ProgressInfo
    {
        public string? Message { get; set; }
        public PipelineStepStage Stage { get; set; }
        public int Processed { get; set; }
        public int Total { get; set; }
    }
}

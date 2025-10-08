namespace KTDL.Pipeline
{
    internal interface IPipelineStep
    {
        Task ExecuteAsync(PipelineContext context);
    }
}

namespace KTDL.Executors
{
    internal interface IFileProcessor
    {
        Task ProccessFileAsync(
            string filePath,
            byte[] imageData,
            CancellationToken cancellationToken);
    }
}

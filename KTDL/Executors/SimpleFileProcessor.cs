namespace KTDL.Executors
{
    internal class SimpleFileProcessor : IFileProcessor
    {
        public async Task ProccessFileAsync(
            string filePath,
            byte[] imageData,
            CancellationToken cancellationToken)
        {
            //TODO: Implement actual file processing logic here
        }
    }
}

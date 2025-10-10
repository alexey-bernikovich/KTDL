namespace KTDL.Executors
{
    internal interface IArchiver
    {
        Task<string> CreateArchiveAsync(
            string sourceDir,
            string arhiveName,
            Func<int, int, Task> onProgress,
            CancellationToken cancellationToken);
    }
}

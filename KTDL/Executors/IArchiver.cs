namespace KTDL.Executors
{
    internal interface IArchiver
    {
        Task<string> CreateArchiveAsync(
            string sourceDir,
            string arhiveName,
            CancellationToken cancellationToken);
    }
}

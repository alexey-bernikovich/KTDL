namespace KTDL.Executors
{
    internal interface IFileDownloader
    {
        Task<List<string>> DownloadAlbumFilesAsync(
            string url,
            string ouptputDir,
            Func<int, int, Task> onProgress,
            CancellationToken cancellationToken);
        Task<Dictionary<string, string>> GetAlbumInfoAsync(
            string url,
            CancellationToken cancellationToken);
        Task<string> GetAlbumCoverAsync(
            string url,
            string outputDir,
            CancellationToken cancellationToken);
    }
}

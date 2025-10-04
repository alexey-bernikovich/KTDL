using System;
using System.Collections.Generic;
using System.Text;

namespace KTDL.Executors
{
    internal interface IFileDownloader
    {
        Task<List<string>> DownloadAlbumFilesAsync(
            string url,
            string ouptputDir,
            Func<int, int, string, Task> onProgress,
            CancellationToken cancellationToken);
        Task<Dictionary<string, string>> GetAlbumInfoAsync(
            string url,
            Func<int, int, string, Task> onProgress,
            CancellationToken cancellationToken);
        Task<string> GetAlbumCoverAsync(
            string url,
            string outputDir,
            CancellationToken cancellationToken);
    }
}

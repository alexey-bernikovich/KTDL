using KTDL.Orchestrator;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Compression;

namespace KTDL.Executors
{
    internal class SimpleArchiver : IArchiver
    {
        private readonly ILogger<SimpleArchiver> _logger;
        private CompressionLevel _compressionLevel = CompressionLevel.Fastest;

        public SimpleArchiver(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SimpleArchiver>();
        }

        // TODO: cancellationToken is not used - fix this
        public async Task<string> CreateArchiveAsync(
            string sourceDir, 
            string archiveName, 
            CancellationToken cancellationToken)
        {
            string safeArchiveName = MakeFileNameSafe(archiveName);
            _logger.LogInformation("Updated file name: {OriginalName} -> {UpdatedName}", archiveName, safeArchiveName);

            var archivePath = Path.Combine(sourceDir, safeArchiveName);
            var files = Directory.GetFiles(sourceDir).Where(x => x.Contains(".mp3")).ToList();

            _logger.LogInformation("Get {Count} files to archive.", files.Count);

            int archivedCount = 0;
            using (var zip = await ZipFile.OpenAsync(archivePath, ZipArchiveMode.Create))
            {
                foreach (string file in files)
                {
                    zip.CreateEntryFromFile(file, Path.GetFileName(file), _compressionLevel);
                    _logger.LogInformation("Archived {File} file. ({Current}/{Count})", file, ++archivedCount, files.Count);
                }
            }
            _logger.LogInformation("Archive size: {Size:F2} MB.", new FileInfo(archivePath).Length / 1024.0 / 1024.0);
            return archivePath;
        }

        private string MakeFileNameSafe(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }
    }
}

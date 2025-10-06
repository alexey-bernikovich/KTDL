using System.IO.Compression;

namespace KTDL.Executors
{
    internal class SimpleArchiver : IArchiver
    {
        private CompressionLevel _compressionLevel = CompressionLevel.Fastest;

        // TODO: cancellationToken is not used - fix this
        public async Task<string> CreateArchiveAsync(
            string sourceDir, 
            string arhiveName, 
            CancellationToken cancellationToken)
        {
            var archivePath = Path.Combine(sourceDir, MakeFileNameSafe(arhiveName));
            var files = Directory.GetFiles(sourceDir).Where(x => x.Contains(".mp3"));

            using (var zip = await ZipFile.OpenAsync(archivePath, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    zip.CreateEntryFromFile(file, Path.GetFileName(file), _compressionLevel);
                }
            }
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

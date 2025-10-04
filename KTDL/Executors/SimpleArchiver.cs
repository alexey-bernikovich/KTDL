using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace KTDL.Executors
{
    internal class SimpleArchiver : IArchiver
    {
        public async Task<string> CreateArchiveAsync(
            string sourceDir, 
            string arhiveName, 
            CancellationToken cancellationToken)
        {
            // TODO: Issue: duplicate names. Need to put in folder with jobid
            var archivePath = Path.Combine("archives", arhiveName);
            Directory.CreateDirectory("archives");

            // TODO: Too slow. Replace with SharpZipLib or DotNetZib
            using (var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    zip.CreateEntryFromFile(file, Path.GetFileName(file));
                }
            }

            return archivePath;
        }

        private void CreateArhive(string sourceDir)
        {

        }
    }
}

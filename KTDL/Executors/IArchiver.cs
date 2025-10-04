using System;
using System.Collections.Generic;
using System.Text;

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

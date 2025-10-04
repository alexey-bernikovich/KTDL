using System;
using System.Collections.Generic;
using System.Text;

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

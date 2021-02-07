using System;
using System.IO;

namespace MtgProfilerTools
{
    internal class OutputStreamProvider
    {
        public OutputStreamProvider(string dataDirectory)
        {
            DataDirectory = dataDirectory;
        }

        public string DataDirectory { get; set; }

        public Stream OpenNewStream()
        {
            string outputPath = GetNextFilePath();
            var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
            return fs;
        }

        private string GetNextFilePath()
        {
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"mtg_profile_data_{ts}.bin";
            return Path.Combine(DataDirectory, fileName);
        }
    }
}

using System.IO;
using System.IO.Compression;

namespace MtgInstrumenter
{
    /// <summary>
    /// Processes a single zip file which might contain multiple files.
    /// The output is a new zip.
    /// </summary>
    internal class ZipProcessor : InstrumentationProcessor
    {
        public ZipProcessor(string file)
        {
            ZipFile = file;
        }

        public string ZipFile { get; }

        public override void Process(ProcessingOptions options)
        {
            using var inputZipFs = File.OpenRead(ZipFile);
            using var inputArchive = new ZipArchive(inputZipFs);

            string outputFile = Path.Combine(options.OutputDirectory, Path.GetFileName(ZipFile));
            using var outputZipFs = File.OpenWrite(outputFile);
            using var outputArchive = new ZipArchive(outputZipFs, ZipArchiveMode.Create);

            foreach (var entry in inputArchive.Entries)
            {
                using var entryStream = entry.Open();
                var outputEntry = outputArchive.CreateEntry(entry.FullName);
                using var outputStream = outputEntry.Open();
                entryStream.CopyTo(outputStream);
            }
        }
    }
}

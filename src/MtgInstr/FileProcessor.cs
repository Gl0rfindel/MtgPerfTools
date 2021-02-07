using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace MtgInstrumenter
{
    /// <summary>
    /// Processes s single dll file
    /// </summary>
    internal class FileProcessor : InstrumentationProcessor
    {
        public FileProcessor(string file)
        {
            FilePath = file;
        }

        public string FilePath { get; }

        public override string DisplayName => Path.GetFileName(FilePath);

        public override void ReportItems(IList<string> items)
        {
            items.Add(FilePath);
        }

        public override void Process(ProcessingContext context)
        {
            using var instrumented = context.Instrumenter.InstrumentFile(FilePath, context.ReaderParams);
            string outputDllName = Path.Combine(context.OutputDirectory, Path.GetFileName(FilePath));
            instrumented.Write(outputDllName);
            Console.WriteLine($"Wrote to {outputDllName}");
        }
    }
}

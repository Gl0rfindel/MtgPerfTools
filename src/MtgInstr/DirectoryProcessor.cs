using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace MtgInstrumenter
{
    /// <summary>
    /// Processes a directory
    /// </summary>
    internal class DirectoryProcessor : InstrumentationProcessor
    {
        private string[] _files;

        public DirectoryProcessor(string directory)
        {
            DirectoryPath = directory;
        }

        public string DirectoryPath { get; }

        public override string DisplayName => Path.GetDirectoryName(DirectoryPath);

        public override void ReportItems(IList<string> items)
        {
            _files = Directory.GetFiles(DirectoryPath, "*.dll");
            foreach (var file in _files)
            {
                items.Add(file);
            }
        }

        public override void Process(ProcessingContext options)
        {
            Console.WriteLine($"Skipping {DirectoryPath}. Not implemented.");
        }
    }
}

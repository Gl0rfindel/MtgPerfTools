﻿using System;
using System.Collections.Generic;
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

        public override string DisplayName => Path.GetFileName(ZipFile);

        public override void ReportItems(IList<string> items)
        {
            items.Add(ZipFile);
        }

        public override void Process(ProcessingContext context)
        {
            using var inputZipFs = File.OpenRead(ZipFile);
            using var inputArchive = new ZipArchive(inputZipFs);

            string outputFile = Path.Combine(context.OutputDirectory, Path.GetFileName(ZipFile));
            using var outputZipFs = new FileStream(outputFile, FileMode.Create);
            using var outputArchive = new ZipArchive(outputZipFs, ZipArchiveMode.Create);

            bool isModZip = false;
            var metadataEntry = inputArchive.GetEntry("metadata.txt");
            string primaryDll = null;
            if (metadataEntry != null)
            {
                isModZip = true;
                primaryDll = GetPrimaryDllName(metadataEntry);
                if (primaryDll != null)
                {
                    Console.WriteLine("Appears to be a mod zip. Handling primary dll only.");
                }
            }

            foreach (var entry in inputArchive.Entries)
            {
                using var entryStream = entry.Open();

                bool shouldProcess = entry.FullName.EndsWith(".dll");
                if (isModZip && primaryDll != null)
                {
                    shouldProcess = entry.Name == primaryDll;
                }

                if (shouldProcess)
                {
                    // we need a seekable stream to read so we have to move the whole uncompressed file into memory.
                    int length = checked((int)entry.Length);
                    using var inputMs = new MemoryStream(length);
                    entryStream.CopyTo(inputMs);
                    inputMs.Seek(0, SeekOrigin.Begin);

                    using var asmDef = context.Instrumenter.InstrumentStream(inputMs, context.ReaderParams);
                    var outputEntry = outputArchive.CreateEntry(entry.FullName);

                    // also need a seekable stream for output... sigh
                    using var outputMs = new MemoryStream();
                    asmDef.Write(outputMs);
                    outputMs.Seek(0, SeekOrigin.Begin);

                    using var outputStream = outputEntry.Open();
                    outputMs.CopyTo(outputStream);
                }
                else
                {
                    var outputEntry = outputArchive.CreateEntry(entry.FullName);
                    using var outputStream = outputEntry.Open();
                    entryStream.CopyTo(outputStream);
                }
            }
        }

        private static string GetPrimaryDllName(ZipArchiveEntry metadataEntry)
        {
            using var metadataStream = metadataEntry.Open();
            string dllName = ModMetadataHelper.GetMainDllName(metadataStream);
            return dllName;
        }
    }
}

using System;
using System.IO;

namespace MtgInstrumenter
{
    internal static class ModMetadataHelper
    {
        public static string GetMainDllName(Stream metadataStream)
        {
            string line;
            using var reader = new StreamReader(metadataStream, leaveOpen: true);
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("#"))
                    continue;

                if (line.StartsWith("DLL:", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = line.Split();
                    if (parts.Length != 2)
                    {
                        return null;
                    }

                    return parts[1];
                }
            }

            return null;
        }
    }
}

using System.Collections.Generic;

namespace MtgInstrumenter
{
    internal class InstrumenterOptions
    {
        public List<string> AssemblyIncludes { get; set; } = new List<string>();

        public List<string> AssemblyExcludes { get; set; } = new List<string>();

        public List<string> TypeIncludes { get; set; } = new List<string>();

        public List<string> TypeExcludes { get; set; } = new List<string>();
    }
}

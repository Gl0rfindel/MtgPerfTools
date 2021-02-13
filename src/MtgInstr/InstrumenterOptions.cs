using System.Collections.Generic;

namespace MtgInstrumenter
{
    internal class InstrumenterOptions
    {
        public List<string> Excludes { get; set; } = new List<string>();

        public List<string> Includes { get; set; } = new List<string>();
    }
}

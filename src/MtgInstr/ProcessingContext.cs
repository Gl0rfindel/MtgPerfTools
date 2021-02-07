using Mono.Cecil;

namespace MtgInstrumenter
{
    internal class ProcessingContext
    {
        public string OutputDirectory { get; set; }

        public AssemblyInstrumenter Instrumenter { get; set; }

        public ReaderParameters ReaderParams { get; set; }
    }
}

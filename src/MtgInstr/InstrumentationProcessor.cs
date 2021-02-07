using System.Collections.Generic;

namespace MtgInstrumenter
{
    abstract class InstrumentationProcessor
    {
        public abstract string DisplayName { get; }

        public abstract void ReportItems(IList<string> items);

        public abstract void Process(ProcessingContext context);
    }
}

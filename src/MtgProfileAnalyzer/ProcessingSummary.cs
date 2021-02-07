namespace MtgProfileAnalyzer
{
    internal class ProcessingSummary
    {
        /// <summary>
        /// Events that are dropped from the trace for any reason.
        /// </summary>
        public int DroppedEvents { get; set; }

        /// <summary>
        /// EVents that have an open but not a close. 
        /// These are forced to close at the end of the trace.
        /// Otherwise some tools might not accept the output as valid.
        /// </summary>
        public int FixedUnbalancedOpenEvents { get; set; }
    }
}

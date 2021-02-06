namespace MtgProfileAnalyzer.Export.Speedscope
{
    class CloseEvent : IEvent
    {
        public string Type => "C";

        public double At { get; set; }

        public int Frame { get; set; }
    }
}

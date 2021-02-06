namespace MtgProfileAnalyzer.Export.Speedscope
{
    class OpenEvent : IEvent
    {
        public string Type => "O";

        public double At { get; set; }

        public int Frame { get; set; }
    }
}

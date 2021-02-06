namespace MtgProfileAnalyzer.Export.Speedscope
{
    interface IEvent
    {
        string Type { get; }
        double At { get; }
    }
}

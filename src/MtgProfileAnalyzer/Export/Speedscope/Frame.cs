using System.Collections.Generic;

namespace MtgProfileAnalyzer.Export.Speedscope
{
    class SpeedscopeFile
    {
        public SharedData Shared { get; set; } = new SharedData();

        public List<object> Profiles { get; set; } = new List<object>();

        public string Name { get; set; }

        public int ActiveProfileIndex { get; set; }

        public string Exporter { get; set; } = "mtgprofiler";
    }

    class SharedData
    {
        public List<Frame> Frames { get; set; } = new List<Frame>();
    }

    class Frame
    {
        public string Name { get; set; }

        public string File { get; set; }

        public string Line { get; set; }

        public string Col { get; set; }
    }
}

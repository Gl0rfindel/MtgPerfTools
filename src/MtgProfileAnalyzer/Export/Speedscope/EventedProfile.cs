using System.Collections.Generic;

namespace MtgProfileAnalyzer.Export.Speedscope
{
    class EventedProfile : IProfile
    {
        public string Type => "evented";

        public string Name { get; set; }

        public ValueUnit Unit { get; set; }

        public double StartValue { get; set; }

        public double EndValue { get; set; }

        public List<object> Events { get; set; } = new List<object>();
    }
}

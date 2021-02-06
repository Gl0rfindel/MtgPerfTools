using System.Collections.Generic;

namespace MtgProfileAnalyzer
{
    internal class D3FlameObject
    {
        public string Name { get; set; }

        public long Value { get; set; }

        public List<D3FlameObject> Children { get; set; } = new List<D3FlameObject>();
    }
}

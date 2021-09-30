using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynmap.API
{
    public partial class Markers
    {
        [JsonProperty("sets")]
        public Sets Sets { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }

    public partial class Sets
    {
        [JsonProperty("markers")]
        public SetsMarkers Markers { get; set; }
    }

    public partial class SetsMarkers
    {
        [JsonProperty("hide")]
        public bool Hide { get; set; }

        [JsonProperty("circles")]
        public Circles Circles { get; set; }

        [JsonProperty("areas")]
        public Dictionary<string, Worldborder> Areas { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("markers")]
        public MarkersMarkers Markers { get; set; }

        [JsonProperty("lines")]
        public Circles Lines { get; set; }

        [JsonProperty("layerprio")]
        public long Layerprio { get; set; }
    }

    public partial class Worldborder
    {
        [JsonProperty("fillcolor")]
        public string Fillcolor { get; set; }

        [JsonProperty("ytop")]
        public long Ytop { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("markup")]
        public bool Markup { get; set; }

        [JsonProperty("x")]
        public List<long> X { get; set; }

        [JsonProperty("weight")]
        public long Weight { get; set; }

        [JsonProperty("z")]
        public List<long> Z { get; set; }

        [JsonProperty("ybottom")]
        public long Ybottom { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("opacity")]
        public double Opacity { get; set; }

        [JsonProperty("fillopacity")]
        public long Fillopacity { get; set; }
    }

    public partial class Circles
    {
    }

    public partial class MarkersMarkers
    {
        [JsonProperty("_spawn_world")]
        public SpawnWorld SpawnWorld { get; set; }
    }

    public partial class SpawnWorld
    {
        [JsonProperty("markup")]
        public bool Markup { get; set; }

        [JsonProperty("x")]
        public long X { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("y")]
        public long Y { get; set; }

        [JsonProperty("dim")]
        public string Dim { get; set; }

        [JsonProperty("z")]
        public long Z { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }
    }

}

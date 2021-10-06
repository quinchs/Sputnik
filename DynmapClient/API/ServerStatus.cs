namespace Dynmap.API
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class ServerStatus
    {
        [JsonProperty("description")]
        public Description Description { get; set; }

        [JsonProperty("players")]
        public Players Players { get; set; }

        [JsonProperty("version")]
        public Version Version { get; set; }

        [JsonProperty("favicon")]
        public string Favicon { get; set; }
    }

    public partial class Description
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public partial class Players
    {
        [JsonProperty("max")]
        public long Max { get; set; }

        [JsonProperty("online")]
        public long Online { get; set; }

        [JsonProperty("sample")]
        public List<Sample> Sample { get; set; }
    }

    public partial class Sample
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public partial class Version
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("protocol")]
        public long Protocol { get; set; }
    }
}

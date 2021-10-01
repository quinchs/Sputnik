namespace Dynmap.API
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class WorldState
    {
        [JsonProperty("currentcount")]
        public int Currentcount { get; set; }

        [JsonProperty("hasStorm")]
        public bool HasStorm { get; set; }

        [JsonProperty("players")]
        public List<Player> Players { get; set; }

        [JsonProperty("isThundering")]
        public bool IsThundering { get; set; }

        [JsonProperty("confighash")]
        public int Confighash { get; set; }

        [JsonProperty("servertime")]
        public int Servertime { get; set; }

        [JsonProperty("updates")]
        public List<Update> Updates { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }

    public partial class Player
    {
        [JsonProperty("world")]
        public string World { get; set; }

        [JsonProperty("armor")]
        public int Armor { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("health")]
        public double Health { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }

        [JsonProperty("sort")]
        public int Sort { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("account")]
        public string Account { get; set; }
    }

    public partial class Update
    {
        [JsonProperty("type")]
        public TypeEnum Type { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("timestamp")]
        public int Timestamp { get; set; }

        [JsonProperty("msg", NullValueHandling = NullValueHandling.Ignore)]
        public string Msg { get; set; }

        [JsonProperty("x", NullValueHandling = NullValueHandling.Ignore)]
        public int? X { get; set; }

        [JsonProperty("y", NullValueHandling = NullValueHandling.Ignore)]
        public int? Y { get; set; }

        [JsonProperty("z", NullValueHandling = NullValueHandling.Ignore)]
        public int? Z { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
        public string Label { get; set; }

        [JsonProperty("icon", NullValueHandling = NullValueHandling.Ignore)]
        public string Icon { get; set; }

        [JsonProperty("set", NullValueHandling = NullValueHandling.Ignore)]
        public string Set { get; set; }

        [JsonProperty("markup", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Markup { get; set; }

        [JsonProperty("desc")]
        public object Desc { get; set; }

        [JsonProperty("dim", NullValueHandling = NullValueHandling.Ignore)]
        public string Dim { get; set; }

        [JsonProperty("minzoom", NullValueHandling = NullValueHandling.Ignore)]
        public int? Minzoom { get; set; }

        [JsonProperty("maxzoom", NullValueHandling = NullValueHandling.Ignore)]
        public int? Maxzoom { get; set; }

        [JsonProperty("ctype", NullValueHandling = NullValueHandling.Ignore)]
        public string Ctype { get; set; }
    }

    public enum TypeEnum { Component, Tile };

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                TypeEnumConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class TypeEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(TypeEnum) || t == typeof(TypeEnum?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "component":
                    return TypeEnum.Component;
                case "tile":
                    return TypeEnum.Tile;
            }
            throw new Exception("Cannot unmarshal type TypeEnum");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (TypeEnum)untypedValue;
            switch (value)
            {
                case TypeEnum.Component:
                    serializer.Serialize(writer, "component");
                    return;
                case TypeEnum.Tile:
                    serializer.Serialize(writer, "tile");
                    return;
            }
            throw new Exception("Cannot marshal type TypeEnum");
        }

        public static readonly TypeEnumConverter Singleton = new TypeEnumConverter();
    }
}

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dynmap.API
{
    public class Configuration
    {
        [JsonProperty("updaterate")]
        public double Updaterate { get; set; }

        [JsonProperty("chatlengthlimit")]
        public int Chatlengthlimit { get; set; }

        [JsonProperty("components")]
        public List<Component> Components { get; set; }

        [JsonProperty("worlds")]
        public List<World> Worlds { get; set; }

        [JsonProperty("confighash")]
        public int Confighash { get; set; }

        [JsonProperty("spammessage")]
        public string Spammessage { get; set; }

        [JsonProperty("defaultmap")]
        public string Defaultmap { get; set; }

        [JsonProperty("msg-chatrequireslogin")]
        public string MsgChatrequireslogin { get; set; }

        [JsonProperty("msg-hiddennamejoin")]
        public string MsgHiddennamejoin { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("grayplayerswhenhidden")]
        public bool Grayplayerswhenhidden { get; set; }

        [JsonProperty("quitmessage")]
        public string Quitmessage { get; set; }

        [JsonProperty("defaultzoom")]
        public int Defaultzoom { get; set; }

        [JsonProperty("allowwebchat")]
        public bool Allowwebchat { get; set; }

        [JsonProperty("allowchat")]
        public bool Allowchat { get; set; }

        [JsonProperty("sidebaropened")]
        public bool Sidebaropened { get; set; }

        [JsonProperty("webchat-interval")]
        public long WebchatInterval { get; set; }

        [JsonProperty("msg-chatnotallowed")]
        public string MsgChatnotallowed { get; set; }

        [JsonProperty("loggedin")]
        public bool Loggedin { get; set; }

        [JsonProperty("coreversion")]
        public string Coreversion { get; set; }

        [JsonProperty("joinmessage")]
        public string Joinmessage { get; set; }

        [JsonProperty("webchat-requires-login")]
        public bool WebchatRequiresLogin { get; set; }

        [JsonProperty("showlayercontrol")]
        public bool Showlayercontrol { get; set; }

        [JsonProperty("login-enabled")]
        public bool LoginEnabled { get; set; }

        [JsonProperty("maxcount")]
        public int Maxcount { get; set; }

        [JsonProperty("dynmapversion")]
        public string Dynmapversion { get; set; }

        [JsonProperty("msg-maptypes")]
        public string MsgMaptypes { get; set; }

        [JsonProperty("cyrillic")]
        public bool Cyrillic { get; set; }

        [JsonProperty("msg-hiddennamequit")]
        public string MsgHiddennamequit { get; set; }

        [JsonProperty("msg-players")]
        public string MsgPlayers { get; set; }

        [JsonProperty("webprefix")]
        public string Webprefix { get; set; }

        [JsonProperty("showplayerfacesinmenu")]
        public bool Showplayerfacesinmenu { get; set; }

        [JsonProperty("defaultworld")]
        public string Defaultworld { get; set; }
    }

    public class Component
    {
        [JsonProperty("spawnlabel", NullValueHandling = NullValueHandling.Ignore)]
        public string Spawnlabel { get; set; }

        [JsonProperty("spawnbedhidebydefault", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Spawnbedhidebydefault { get; set; }

        [JsonProperty("spawnbedformat", NullValueHandling = NullValueHandling.Ignore)]
        public string Spawnbedformat { get; set; }

        [JsonProperty("worldborderlabel", NullValueHandling = NullValueHandling.Ignore)]
        public string Worldborderlabel { get; set; }

        [JsonProperty("showworldborder", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Showworldborder { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("showlabel", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Showlabel { get; set; }

        [JsonProperty("offlineicon", NullValueHandling = NullValueHandling.Ignore)]
        public string Offlineicon { get; set; }

        [JsonProperty("showspawnbeds", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Showspawnbeds { get; set; }

        [JsonProperty("showofflineplayers", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Showofflineplayers { get; set; }

        [JsonProperty("spawnbedicon", NullValueHandling = NullValueHandling.Ignore)]
        public string Spawnbedicon { get; set; }

        [JsonProperty("offlinehidebydefault", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Offlinehidebydefault { get; set; }

        [JsonProperty("offlinelabel", NullValueHandling = NullValueHandling.Ignore)]
        public string Offlinelabel { get; set; }

        [JsonProperty("enablesigns", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Enablesigns { get; set; }

        [JsonProperty("default-sign-set", NullValueHandling = NullValueHandling.Ignore)]
        public string DefaultSignSet { get; set; }

        [JsonProperty("spawnicon", NullValueHandling = NullValueHandling.Ignore)]
        public string Spawnicon { get; set; }

        [JsonProperty("offlineminzoom", NullValueHandling = NullValueHandling.Ignore)]
        public long? Offlineminzoom { get; set; }

        [JsonProperty("spawnbedminzoom", NullValueHandling = NullValueHandling.Ignore)]
        public long? Spawnbedminzoom { get; set; }

        [JsonProperty("showspawn", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Showspawn { get; set; }

        [JsonProperty("spawnbedlabel", NullValueHandling = NullValueHandling.Ignore)]
        public string Spawnbedlabel { get; set; }

        [JsonProperty("maxofflinetime", NullValueHandling = NullValueHandling.Ignore)]
        public long? Maxofflinetime { get; set; }

        [JsonProperty("allowurlname", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Allowurlname { get; set; }

        [JsonProperty("focuschatballoons", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Focuschatballoons { get; set; }

        [JsonProperty("showplayerfaces", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Showplayerfaces { get; set; }

        [JsonProperty("sendbutton", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Sendbutton { get; set; }

        [JsonProperty("messagettl", NullValueHandling = NullValueHandling.Ignore)]
        public long? Messagettl { get; set; }

        [JsonProperty("hidebydefault", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Hidebydefault { get; set; }

        [JsonProperty("showplayerhealth", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Showplayerhealth { get; set; }

        [JsonProperty("showplayerbody", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Showplayerbody { get; set; }

        [JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
        public string Label { get; set; }

        [JsonProperty("smallplayerfaces", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Smallplayerfaces { get; set; }

        [JsonProperty("layerprio", NullValueHandling = NullValueHandling.Ignore)]
        public long? Layerprio { get; set; }

        [JsonProperty("showdigitalclock", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Showdigitalclock { get; set; }

        [JsonProperty("showweather", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Showweather { get; set; }

        [JsonProperty("show-mcr", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShowMcr { get; set; }

        [JsonProperty("show-chunk", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShowChunk { get; set; }

        [JsonProperty("hidey", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Hidey { get; set; }
    }

    public class World
    {
        [JsonProperty("sealevel")]
        public long Sealevel { get; set; }

        [JsonProperty("protected")]
        public bool Protected { get; set; }

        [JsonProperty("maps")]
        public List<Map> Maps { get; set; }

        [JsonProperty("extrazoomout")]
        public long Extrazoomout { get; set; }

        [JsonProperty("center")]
        public Center Center { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("worldheight")]
        public long Worldheight { get; set; }
    }

    public class Center
    {
        [JsonProperty("x")]
        public long X { get; set; }

        [JsonProperty("y")]
        public long Y { get; set; }

        [JsonProperty("z")]
        public long Z { get; set; }
    }

    public class Map
    {
        [JsonProperty("inclination")]
        public long Inclination { get; set; }

        [JsonProperty("nightandday")]
        public bool Nightandday { get; set; }

        [JsonProperty("image-format")]
        public string ImageFormat { get; set; }

        [JsonProperty("shader")]
        public string Shader { get; set; }

        [JsonProperty("compassview")]
        public string Compassview { get; set; }

        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        [JsonProperty("icon")]
        public object Icon { get; set; }

        [JsonProperty("scale")]
        public long Scale { get; set; }

        [JsonProperty("azimuth")]
        public long Azimuth { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("lighting")]
        public string Lighting { get; set; }

        [JsonProperty("backgroundday")]
        public object Backgroundday { get; set; }

        [JsonProperty("bigmap")]
        public bool Bigmap { get; set; }

        [JsonProperty("maptoworld")]
        public List<double> Maptoworld { get; set; }

        [JsonProperty("protected")]
        public bool Protected { get; set; }

        [JsonProperty("background")]
        public string Background { get; set; }

        [JsonProperty("mapzoomout")]
        public long Mapzoomout { get; set; }

        [JsonProperty("boostzoom")]
        public long Boostzoom { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("backgroundnight")]
        public object Backgroundnight { get; set; }

        [JsonProperty("perspective")]
        public string Perspective { get; set; }

        [JsonProperty("mapzoomin")]
        public long Mapzoomin { get; set; }

        [JsonProperty("worldtomap")]
        public List<double> Worldtomap { get; set; }
    }
}

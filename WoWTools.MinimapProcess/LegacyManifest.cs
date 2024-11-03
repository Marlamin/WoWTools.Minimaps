using System.Text.Json.Serialization;

namespace WoWTools.MinimapProcess
{
    public class LegacyManifest
    {
        public List<LegacyMapEntry> maps { get; set; }
        public Dictionary<string, Dictionary<string, LegacyVersionEntry>> versions { get; set; }


    }
    public class LegacyMapEntry
    {
        public int id { get; set; }
        public string name { get; set; }

        [JsonPropertyName("internal")]
        public string internal_name { get; set; }
        public int? internal_mapid { get; set; }
        public int? wdtFileDataID { get; set; }
        public int firstseen { get; set; }
    }

    public class LegacyVersionEntry
    {
        public int versionid { get; set; }
        public string md5 { get; set; }
        public int build { get; set; }
        public string branch { get; set; }
        public string fullbuild { get; set; }
        public LegacyVersionConfig config { get; set; }
    }

    public class LegacyVersionConfig
    {
        public int resx { get; set; }
        public int resy { get; set; }
        public int zoom { get; set; }
        public int minzoom { get; set; }
        public int maxzoom { get; set; }
        public LegacyOffset offset { get; set; }
    }

    public class LegacyOffset
    {
        public LegacyOffsetMin min { get; set; }
    }

    public class LegacyOffsetMin
    {
        public int y { get; set; }
        public int x { get; set; }
    }


}

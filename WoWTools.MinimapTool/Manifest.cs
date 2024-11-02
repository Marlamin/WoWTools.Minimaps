namespace WoWTools.MinimapTool
{
    public class Manifest
    {
        public Dictionary<int, MapEntry> Maps { get; set; }
        public Dictionary<int, VersionEntry> Versions { get; set; }
        public Dictionary<int, Dictionary<int, MapVersionEntry>> MapVersions { get; set; }
    }

    public record MapEntry(string Name, string InternalName, int? InternalMapID, int? WDTFileDataID, int FirstSeenBuild);

    public record VersionEntry(int Build, string Branch, string FullBuild);

    public record MapVersionEntry(int VersionID, string MD5, MapVersionConfig Config);

    public record MapVersionConfig(int ResX, int ResY, int Zoom, int MinZoom, int MaxZoom, int MinX, int MinY);
}

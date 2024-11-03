using System.Text.Json;

namespace WoWTools.MinimapProcess
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var mode = "generate";

            if (mode == "convert")
            {
                var oldjson = JsonSerializer.Deserialize<LegacyManifest>(File.ReadAllText("D:\\Projects\\map.wow.tools\\data\\data.json"));

                var newjson = new Manifest
                {
                    Maps = new Dictionary<int, MapEntry>(),
                    Versions = new Dictionary<int, VersionEntry>(),
                    MapVersions = new Dictionary<int, Dictionary<int, MapVersionEntry>>()
                };

                foreach (var map in oldjson.maps)
                {
                    newjson.Maps[map.id] = new MapEntry(map.name, map.internal_name, map.internal_mapid, map.wdtFileDataID, map.firstseen);
                }

                foreach (var version in oldjson.versions)
                {
                    foreach (var versionEntry in version.Value)
                    {
                        if (!newjson.Versions.ContainsKey(versionEntry.Value.versionid))
                        {
                            newjson.Versions.Add(versionEntry.Value.versionid, new VersionEntry(versionEntry.Value.build, versionEntry.Value.branch, versionEntry.Value.fullbuild));
                        }

                        if (!newjson.MapVersions.ContainsKey(int.Parse(version.Key)))
                        {
                            newjson.MapVersions.Add(int.Parse(version.Key), new Dictionary<int, MapVersionEntry>());
                        }

                        newjson.MapVersions[int.Parse(version.Key)].Add(versionEntry.Value.versionid, new MapVersionEntry(versionEntry.Value.versionid, versionEntry.Value.md5, new MapVersionConfig(versionEntry.Value.config.resx, versionEntry.Value.config.resy, versionEntry.Value.config.zoom, versionEntry.Value.config.minzoom, versionEntry.Value.config.maxzoom, versionEntry.Value.config.offset.min.x, versionEntry.Value.config.offset.min.y)));

                    }
                }

                File.WriteAllText("D:\\Projects\\map.wow.tools\\data\\manifest.json", JsonSerializer.Serialize(newjson, new JsonSerializerOptions { WriteIndented = true }));
            }
            else if (mode == "generate")
            {
                var current = JsonSerializer.Deserialize<Manifest>(File.ReadAllText("D:\\Projects\\map.wow.tools\\data\\manifest.json"));

                foreach (var version in current.Versions)
                {
                    if (version.Value.Branch == null)
                        Console.WriteLine("Warning: Branch is null for build " + version.Value.FullBuild + " (ID " + version.Key + ")");
                }
            }
        }
    }
}

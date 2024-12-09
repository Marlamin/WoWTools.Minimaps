using System.Text.Json;
using System.Text.Json.Serialization;

namespace WoWTools.MinimapProcess
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: MinimapProcess <mode: convert/generate> [args]");
                return;
            }

            var mode = args[0];

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
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: MinimapProcess generate <inputDir> <outputDir>");
                    return;
                }

                var inputDir = args[1];
                var outputDir = args[2];
                var fullrun = false;

                var current = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(outputDir, "manifest_v2.json")));

                foreach (var version in current.Versions)
                {
                    if (version.Value.Branch == null)
                        Console.WriteLine("Warning: Branch is null for build " + version.Value.FullBuild + " (ID " + version.Key + ")");
                }

                var inputBuildManifest = Path.Combine(inputDir, "buildMap.json");
                if (!File.Exists(inputBuildManifest))
                {
                    Console.WriteLine("Error: buildMap.json not found in input directory");
                    return;
                }

                var buildManifest = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(inputBuildManifest));
                var wagoBuild2Branch = new Dictionary<string, string>();

                if (fullrun)
                    current.MapVersions = new Dictionary<int, Dictionary<int, MapVersionEntry>>();

                // Remove builds not in build manifest
                foreach (var version in current.Versions)
                {
                    if (!buildManifest.ContainsKey(version.Value.FullBuild))
                    {
                        Console.WriteLine("Warning: Build " + version.Value.FullBuild + " not found in build manifest, removing..");
                        current.Versions.Remove(version.Key);
                    }
                }

                foreach (var build in buildManifest)
                {
                    var buildPath = Path.Combine(inputDir, build.Value);
                    if (!Directory.Exists(buildPath))
                    {
                        Console.WriteLine("Build directory for " + build.Key + " does not exist, skipping..");
                        continue;
                    }

                    var compiledMapPath = Path.Combine(buildPath, "compiled");

                    if (!Directory.Exists(compiledMapPath) || Directory.GetFiles(compiledMapPath).Count() == 0)
                    {
                        Console.WriteLine("Build directory for " + build.Key + " has no compiled maps, skipping..");
                        continue;
                    }

                    Console.WriteLine(build.Key);

                    // Check if build exists in versions list, if not, add it.
                    if (!current.Versions.Any(x => x.Value.FullBuild == build.Key))
                    {
                        if (wagoBuild2Branch.Count == 0)
                        {
                            var httpClient = new HttpClient();
                            var response = httpClient.GetAsync("https://wago.tools/api/builds").Result;
                            var json = response.Content.ReadAsStringAsync().Result;
                            var wagoBuilds = JsonSerializer.Deserialize<Dictionary<string, List<WagoBuild>>>(json);
                            foreach (var wagoBranch in wagoBuilds)
                            {
                                foreach (var wagoBuild in wagoBranch.Value)
                                {
                                    var niceBranch = ProductToBranch(wagoBuild.product);
                                    wagoBuild2Branch[wagoBuild.version] = niceBranch;
                                }
                            }
                        }

                        if (wagoBuild2Branch.TryGetValue(build.Key, out var branch))
                        {
                            var maxID = current.Versions.Keys.Max();
                            current.Versions.Add(maxID + 1, new VersionEntry(int.Parse(build.Key.Split('.')[3]), branch, build.Key));
                            Console.WriteLine("Added new build " + build.Key + " to manifest with branch " + branch);
                        }
                        else
                        {
                            Console.WriteLine("Error: Could not find branch for build " + build.Key);
                        }
                    }

                    if (!Directory.Exists(Path.Combine(buildPath, "compiled")))
                    {
                        Console.WriteLine("Error: compiled map directory not found for build " + build.Key);
                        continue;
                    }

                    var mapsPath = Path.Combine(buildPath, "maps");

                    foreach (var compiledMap in Directory.GetFiles(Path.Combine(buildPath, "compiled"), "*.png"))
                    {
                        var mapName = Path.GetFileNameWithoutExtension(compiledMap);
                        if (!current.Maps.Any(x => x.Value.InternalName == mapName))
                        {
                            var maxID = current.Maps.Keys.Max();

                            Console.WriteLine("Warning: Map " + mapName + " not found in manifest, adding placeholder entry for it..");
                            current.Maps.Add(maxID + 1, new MapEntry(mapName, mapName, int.TryParse(mapName, out var mapAsNumber) ? mapAsNumber : null, null, int.Parse(build.Key.Split('.')[3])));
                        }

                        var mapID = current.Maps.First(x => x.Value.InternalName == mapName).Key;

                        if (!current.MapVersions.ContainsKey(mapID))
                            current.MapVersions.Add(mapID, []);

                        var versionID = current.Versions.First(x => x.Value.FullBuild == build.Key).Key;
                        if (!current.MapVersions[mapID].ContainsKey(versionID))
                        {
                            var mapManifestFile = Path.Combine(mapsPath, mapName + ".json");
                            MapManifestNew mapManifest = null;

                            try
                            {
                                mapManifest = JsonSerializer.Deserialize<MapManifestNew>(File.ReadAllText(mapManifestFile));
                            }
                            catch (JsonException e)
                            {
                                Console.WriteLine("Error: Failed to parse map manifest for " + mapName + " in build " + build.Key + ", trying to parse as old manifest instead..");

                                var oldMapManifest = JsonSerializer.Deserialize<MapManifestOld>(File.ReadAllText(mapManifestFile));

                                mapManifest = new MapManifestNew
                                {
                                    MinX = sbyte.Parse(oldMapManifest.MinX),
                                    MinY = sbyte.Parse(oldMapManifest.MinY),
                                    MaxX = sbyte.Parse(oldMapManifest.MaxX),
                                    MaxY = sbyte.Parse(oldMapManifest.MaxY),
                                    MapName = oldMapManifest.MapName,
                                    InternalMapID = oldMapManifest.InternalMapID,
                                    WDTFileDataID = oldMapManifest.WDTFileDataID,
                                    TileHashes = oldMapManifest.TileHashes
                                };
                            }

                            uint resX = 0;
                            uint resY = 0;
                            using (var fs = File.OpenRead(Path.Combine(buildPath, "compiled", mapName + ".png")))
                            using (var bin = new BinaryReader(fs))
                            {
                                bin.BaseStream.Position = 16;
                                var resXBytes = bin.ReadBytes(4);
                                Array.Reverse(resXBytes);
                                resX = BitConverter.ToUInt32(resXBytes, 0);

                                var resYBytes = bin.ReadBytes(4);
                                Array.Reverse(resYBytes);
                                resY = BitConverter.ToUInt32(resYBytes, 0);
                            }

                            var zoom = 1;
                            var minZoom = 0;
                            var maxZoom = 0;

                            // Find max zoom level
                            for (byte i = 0; i < 8; i++)
                            {
                                if (Directory.Exists(Path.Combine(buildPath, "tiles", mapName, i.ToString())))
                                    maxZoom = i;
                            }

                            current.MapVersions[mapID][versionID] =
                                new MapVersionEntry(
                                    versionID,
                                    build.Value,
                                    new MapVersionConfig(
                                        (int)resX,
                                        (int)resY,
                                        1,
                                        minZoom,
                                        maxZoom,
                                        mapManifest.MinX,
                                        mapManifest.MinY
                                        )
                                );

                            Console.WriteLine("Added new version " + build.Key + " for map " + mapName);
                        }
                    }
                }

                // Cleanup
                var usedVersionIDs = new List<int>();
                var usedMapIDs = new List<int>();

                var mapVersions = new Dictionary<int, Dictionary<int, MapVersionEntry>>(current.MapVersions);
                foreach (var map in current.MapVersions)
                {
                    foreach(var version in map.Value)
                    {
                        if (!current.Versions.ContainsKey(version.Key))
                        {
                            Console.WriteLine("Warning: Version " + version.Key + " does not exist, removing..");
                            mapVersions[map.Key].Remove(version.Key);
                        }
                    }
                }

                current.MapVersions = mapVersions;

                foreach (var map in current.MapVersions)
                {
                    usedMapIDs.Add(map.Key);
                    foreach (var version in map.Value)
                    {
                        usedVersionIDs.Add(version.Key);
                    }
                }

                foreach (var version in current.Versions)
                {
                    if (!usedVersionIDs.Contains(version.Key))
                    {
                        Console.WriteLine("Warning: Version " + version.Value.FullBuild + " is not used by any map, removing..");
                        current.Versions.Remove(version.Key);
                    }
                }

                foreach (var map in current.Maps)
                {
                    if (!usedMapIDs.Contains(map.Key))
                    {
                        Console.WriteLine("Warning: Map " + map.Value.InternalName + " is not used by any version, removing..");
                        current.Maps.Remove(map.Key);
                    }
                }

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                File.WriteAllText(Path.Combine(outputDir, "manifest_v2.json"), JsonSerializer.Serialize(current, options));
            }
        }

        private static string ProductToBranch(string product)
        {
            return product switch
            {
                "wow" or "wowlivetest" => "Retail",
                "wowt" or "wowxptr" => "PTR",
                "wow_beta" => "Beta",
                "wowz" => "Submission",
                "wow_classic" => "Retail (Classic)",
                "wow_classic_ptr" => "PTR (Classic)",
                "wow_classic_beta" => "Beta (Classic)",
                "wow_classic_era" => "Retail (Classic Era)",
                "wow_classic_era_ptr" => "PTR (Classic Era)",
                "wow_classic_era_beta" => "Beta (Classic Era)",
                _ => throw new Exception("Unhandled product: " + product),
            };
        }

        class WagoBuild
        {
            public string product { get; set; }
            public string version { get; set; }
            public string created_at { get; set; }
            public string build_config { get; set; }
            public string product_config { get; set; }
            public string cdn_config { get; set; }
            public bool is_bgdl { get; set; }
        }
        public record MapManifestOld
        {
            public string MinX { get; set; }
            public string MinY { get; set; }
            public string MaxX { get; set; }
            public string MaxY { get; set; }
            public string? MapName { get; set; }
            public int? InternalMapID { get; set; }
            public int? WDTFileDataID { get; set; }
            public string[][] TileHashes { get; set; }
        }

        public record MapManifestNew
        {
            public sbyte MinX { get; set; }
            public sbyte MinY { get; set; }
            public sbyte MaxX { get; set; }
            public sbyte MaxY { get; set; }
            public string? MapName { get; set; }
            public int? InternalMapID { get; set; }
            public int? WDTFileDataID { get; set; }
            public string[][] TileHashes { get; set; }
        }
    }
}

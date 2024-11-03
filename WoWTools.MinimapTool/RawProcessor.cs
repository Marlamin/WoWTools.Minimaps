using DBDefsLib;
using NetVips;
using SereniaBLPLib;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TACT.Net.Root;

namespace WoWTools.MinimapTool
{
    public static class RawProcessor
    {
        private static VersionManifest previousVersion = new();
        private static VersionManifest currentVersion = new();

        private static string BaseInDir;
        private static string BaseOutDir;

        public static void Start(string baseoutdir)
        {
            BaseOutDir = baseoutdir;
        }

        public static void ProcessBuild(string inputdir, Build build)
        {
            if (build.expansion != 0)
                return;

            BaseInDir = inputdir;

            if (previousVersion.maps == null)
                previousVersion.maps = new();

            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [Raw] Processing build " + build);

            var buildMap = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(BaseOutDir, "buildMap.json")));

            var hash = MD5.HashData(Encoding.UTF8.GetBytes(build.ToString()));

            var fakeRootKey = Convert.ToHexString(hash).ToLower();

            var outdir = Path.Combine(BaseOutDir, fakeRootKey);

            //if (Directory.Exists(Path.Combine(outdir, "compiled")))
            //{
            //    Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [Raw] Build " + build + " already processed, skipping");
            //    return;
            //}

            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);

            buildMap[build.ToString()] = fakeRootKey;

            currentVersion.version = build.ToString();
            currentVersion.product = "wowold";
            currentVersion.rootCKey = fakeRootKey;
            currentVersion.maps = new();


            foreach (var mapDir in Directory.GetDirectories(Path.Combine(inputdir, "World", "Minimaps")))
            {
                var mapName = Path.GetFileName(mapDir);

                if (mapName.ToLower() == "wmo")
                    continue;

                Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] " + mapName);

                // Fill up the current version tile dictionary
                if (!currentVersion.maps.ContainsKey(mapName))
                    currentVersion.maps[mapName] = new MapManifest() { MaxX = "-1", MaxY = "-1", MinX = "-1", MinY = "-1", TileHashes = new string[64][] };

                for (byte x = 0; x < 64; x++)
                    currentVersion.maps[mapName].TileHashes[x] = new string[64];

                Dictionary<(sbyte x, sbyte y), RootRecord> toCompile = [];

                sbyte min_x = 64;
                sbyte min_y = 64;
                sbyte max_x = 0;
                sbyte max_y = 0;

                for (sbyte x = 0; x < 64; x++)
                {
                    for (sbyte y = 0; y < 64; y++)
                    {
                        var tileName = Path.Combine(mapDir, "map" + x.ToString().PadLeft(2, '0') + "_" + y.ToString().PadLeft(2, '0') + ".blp");
                        if (File.Exists(tileName))
                        {
                            var fakeRootRecord = new RootRecord
                            {
                                CKey = new TACT.Net.Cryptography.MD5Hash(MD5.HashData(File.ReadAllBytes(tileName)))
                            };

                            toCompile.Add((x, y), fakeRootRecord);

                            if (x < min_x) { min_x = x; }
                            if (y < min_y) { min_y = y; }

                            if (x > max_x) { max_x = x; }
                            if (y > max_y) { max_y = y; }

                            currentVersion.maps[mapName].TileHashes[x][y] = fakeRootRecord.CKey.ToString();
                        }
                    }
                }

                currentVersion.maps[mapName].MinX = min_x.ToString();
                currentVersion.maps[mapName].MinY = min_y.ToString();
                currentVersion.maps[mapName].MaxX = max_x.ToString();
                currentVersion.maps[mapName].MaxY = max_y.ToString();

                var compileTiles = !previousVersion.maps.ContainsKey(mapName);

                if (!compileTiles)
                {
                    if (previousVersion == null)
                        compileTiles = true;

                    if (!compileTiles)
                    {
                        for (byte x = 0; x < 64; x++)
                        {
                            for (byte y = 0; y < 64; y++)
                            {
                                if (previousVersion.maps[mapName].TileHashes[x][y] != currentVersion.maps[mapName].TileHashes[x][y])
                                {
                                    Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Tile hash mismatch for " + mapName + " " + x + " " + y + ", map flagged for extraction.");

                                    if (previousVersion.maps[mapName].TileHashes[x][y] == null)
                                    {
                                        Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t null != " + currentVersion.maps[mapName].TileHashes[x][y].ToString());
                                    }
                                    else if (currentVersion.maps[mapName].TileHashes[x][y] == null)
                                    {
                                        Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t " + previousVersion.maps[mapName].TileHashes[x][y] + " != null");
                                    }
                                    else
                                    {
                                        Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t " + previousVersion.maps[mapName].TileHashes[x][y] + " != " + currentVersion.maps[mapName].TileHashes[x][y].ToString());
                                    }

                                    compileTiles = true;
                                    break;
                                }
                            }

                            if (compileTiles)
                                break;
                        }
                    }
                }

                if (compileTiles)
                {
                    if (!Directory.Exists(outdir))
                        Directory.CreateDirectory(outdir);

                    if (!Directory.Exists(Path.Combine(outdir, "compiled")))
                        Directory.CreateDirectory(Path.Combine(outdir, "compiled"));

                    if (!File.Exists(Path.Combine(outdir, "compiled", mapName + ".png")))
                        CompileMap(toCompile, mapName, outdir, min_x, min_y, max_x, max_y);
                }
            }

            // Save the version manifest
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);

            if (!Directory.Exists(Path.Combine(outdir, "maps")))
                Directory.CreateDirectory(Path.Combine(outdir, "maps"));

            File.WriteAllText(Path.Combine(outdir, "versionManifest.json"), JsonSerializer.Serialize(currentVersion, new JsonSerializerOptions { WriteIndented = true }));

            foreach (var mapManifest in currentVersion.maps)
            {
                var newMapManifest = new MapManifestNew
                {
                    MinX = sbyte.Parse(mapManifest.Value.MinX),
                    MinY = sbyte.Parse(mapManifest.Value.MinY),
                    MaxX = sbyte.Parse(mapManifest.Value.MaxX),
                    MaxY = sbyte.Parse(mapManifest.Value.MaxY),
                    TileHashes = mapManifest.Value.TileHashes
                };

                File.WriteAllText(Path.Combine(outdir, "maps", mapManifest.Key + ".json"), JsonSerializer.Serialize(newMapManifest, new JsonSerializerOptions { WriteIndented = true }));
            }

            previousVersion = currentVersion;

            // Re-sort buildmap in expected build order
            var sorted = buildMap.OrderBy(x => new Build(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            buildMap = sorted;

            // Save the build map
            File.WriteAllText(Path.Combine(BaseOutDir, "buildMap.json"), JsonSerializer.Serialize(buildMap, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static void TryLoadVersionManifest(string buildRootKey)
        {
            var outdir = Path.Combine(BaseOutDir, buildRootKey);
            if (!Directory.Exists(outdir))
                return;

            var versionManifestPath = Path.Combine(outdir, "versionManifest.json");
            if (!File.Exists(versionManifestPath))
                return;

            previousVersion = JsonSerializer.Deserialize<VersionManifest>(File.ReadAllText(versionManifestPath));
            Console.WriteLine("Loaded build " + previousVersion.version + " as previous version");
        }

        public static void CompileMap(Dictionary<(sbyte, sbyte), RootRecord> tiles, string mapName, string outDir, sbyte min_x, sbyte min_y, sbyte max_x, sbyte max_y)
        {
            var blpRes = 512;

            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Compiling map " + mapName + " (" + tiles.Count + " tiles)");

            var emptyTile = Image.Black(blpRes, blpRes);
            var mask = emptyTile.Equal(new[] { 0, 0, 0, 255 }).BandAnd();
            emptyTile = mask.Ifthenelse(new[] { 0, 0, 0, 0 }, emptyTile);

            var mapDir = Path.Combine(BaseInDir, "World", "Minimaps", mapName);

            Dictionary<string, Image> TileCache = [];

            var imageList = new List<Image>();
            for (sbyte cur_y = 0; cur_y < 64; cur_y++)
            {
                for (sbyte cur_x = 0; cur_x < 64; cur_x++)
                {
                    if (cur_x > max_x || cur_y > max_y)
                        continue;

                    if (cur_x < min_x || cur_y < min_y)
                        continue;

                    if (!tiles.TryGetValue((cur_x, cur_y), out var rootRecord))
                    {
                        imageList.Add(emptyTile);
                    }
                    else
                    {
                        Image? image;

                        if (TileCache.TryGetValue(rootRecord.CKey.ToString(), out var cachedImage))
                        {
                            image = cachedImage;
                        }
                        else
                        {
                            using (var stream = new MemoryStream())
                            {
                                var tileName = Path.Combine(mapDir, "map" + cur_x.ToString().PadLeft(2, '0') + "_" + cur_y.ToString().PadLeft(2, '0') + ".blp");

                                new BlpFile(File.OpenRead(tileName)).GetBitmap(0).Save(stream, System.Drawing.Imaging.ImageFormat.Png);

                                try
                                {
                                    image = Image.NewFromBuffer(stream.ToArray());
                                }
                                catch (Exception e)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Failed to create new image from BLP: " + e.Message);
                                    Console.ResetColor();
                                    continue;
                                }

                                if (image.Width != blpRes)
                                {
                                    if (blpRes == 512 && image.Width == 256)
                                    {
                                        image = image.Resize(2, Enums.Kernel.Nearest);
                                    }
                                }

                                TileCache[rootRecord.CKey.ToString()] = image;
                            }
                        }

                        imageList.Add(image);
                    }
                }
            }

            var timer = System.Diagnostics.Stopwatch.StartNew();

            // Generate compiled map
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Writing to output file..");
            var outpng = Path.Combine(outDir, "compiled", mapName + ".png");
            var compiled = Image.Arrayjoin(imageList.ToArray(), (max_x - min_x) + 1);
            compiled.WriteToFile(outpng);
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Done, took " + timer.ElapsedMilliseconds + "ms");

            timer.Restart();

            // Generate tilesets
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Generating tiles..");
            var outdir = Path.Combine(outDir, "tiles", mapName);
            compiled.Dzsave(outdir, mapName, Enums.ForeignDzLayout.Google, ".png", background: [0, 0, 0, 0]);
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Done, took " + timer.ElapsedMilliseconds + "ms");

            timer.Stop();
        }
    }
}

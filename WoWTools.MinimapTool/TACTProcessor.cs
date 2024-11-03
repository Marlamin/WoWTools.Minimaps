using DBCD.Providers;
using DBDefsLib;
using NetVips;
using SereniaBLPLib;
using System.Text.Json;
using TACT.Net;
using TACT.Net.Configs;
using TACT.Net.Root;

namespace WoWTools.MinimapTool
{
    public static class TACTProcessor
    {
        public static TACTRepo TACTRepo;
        private static string BaseOutDir;
        private static string RepoPath;

        private static bool warmedUp = false;

        private static DBCD.DBCD DBCDInstance;

        private static VersionManifest previousVersion = new();
        private static VersionManifest currentVersion = new();

        public static void Start(string baseOutDir, string repoPath)
        {
            BaseOutDir = baseOutDir;
            RepoPath = repoPath;

            TACTRepo = new TACTRepo(repoPath)
            {
                ManifestContainer = new ManifestContainer("wow", Locale.US),
                ConfigContainer = new ConfigContainer()
            };

            TACTKeys.Load();
            Listfile.Load();

            // Set up DBCD
            var dbdProvider = new GithubDBDProvider();
            var dbcProvider = new CASCDBCProvider();
            DBCDInstance = new DBCD.DBCD(dbcProvider, dbdProvider);

            var manifestPath = "manifests";

            // open the configs
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [TACT] Opening manifest at " + manifestPath + "...");
            TACTRepo.ManifestContainer.OpenLocal(manifestPath);

            // set up version manifests
            previousVersion.maps = new Dictionary<string, MapManifest>();
            currentVersion.maps = new Dictionary<string, MapManifest>();
        }

        public static void WarmUpIndexes(List<string> cdnConfigs)
        {
            var archiveList = new List<string>();
            foreach (var cdnConfig in cdnConfigs)
            {
                var parsed = new KeyValueConfig(cdnConfig, RepoPath, ConfigType.CDNConfig);
                foreach (var archive in parsed.GetValues("archives"))
                    if (!archiveList.Contains(archive))
                        archiveList.Add(archive);
            }

            var indexPathList = new List<string>();
            foreach (var archive in archiveList)
            {
                indexPathList.Add(MakeCDNPath(RepoPath, "data", archive) + ".index");
            }

            // load the archives
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [TACT] Loading " + archiveList.Count + " archives...");
            TACTRepo.IndexContainer = new TACT.Net.Indices.IndexContainer();
            TACTRepo.IndexContainer.Open(TACTRepo.BaseDirectory, indexPathList, true);

            warmedUp = true;
        }

        public static void ProcessBuild(string buildConfig, string cdnConfig, string product, Build build)
        {
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [TACT] Opening config " + buildConfig + " at " + RepoPath + "...");
            TACTRepo.ConfigContainer.OpenLocal(TACTRepo.BaseDirectory, buildConfig, "");

            var buildMap = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(BaseOutDir, "buildMap.json")));

            var rootKey = Convert.ToHexString(TACTRepo.ConfigContainer.RootCKey.Value).ToLower();

            string buildName;

            try
            {
                var configBuildName = TACTRepo.ConfigContainer.BuildConfig.GetValue("build-name");
                var splitName = configBuildName.Replace("WOW-", "").Split("patch");
                buildName = splitName[1].Split("_")[0] + "." + splitName[0];
                Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [TACT] Loaded build: " + configBuildName + " (" + buildName + ")");
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error parsing build name, falling back to given build.");
                buildName = build.ToString();
                Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [TACT] Loaded build: " + buildName);
            }

            buildMap[buildName] = rootKey;

            currentVersion.version = buildName;
            currentVersion.product = product;
            currentVersion.rootCKey = rootKey;
            currentVersion.maps = new();

            var outdir = Path.Combine(BaseOutDir, rootKey);

            //if (Directory.Exists(outdir) && Directory.GetFiles(outdir).Length > 0)
            //{
            //    Console.WriteLine("Output directory already exists, skipping build " + buildName);
            //    File.WriteAllText(Path.Combine(BaseOutDir, "buildMap.json"), JsonSerializer.Serialize(buildMap, new JsonSerializerOptions { WriteIndented = true }));
            //    return;
            //}

            // open the encoding
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [TACT] Opening encoding...");
            TACTRepo.EncodingFile = new TACT.Net.Encoding.EncodingFile(TACTRepo.BaseDirectory, TACTRepo.ConfigContainer.EncodingEKey);

            // get the root ckey
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [TACT] Getting root CKey...");
            if (!TACTRepo.EncodingFile.TryGetCKeyEntry(TACTRepo.ConfigContainer.RootCKey, out var rootCEntry))
                throw new Exception("Root CKey not found");

            // open the root
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [TACT] Opening root...");
            TACTRepo.RootFile = new TACT.Net.Root.RootFile(TACTRepo.BaseDirectory, rootCEntry.EKeys[0]);

            // Open Map DBC with local defs
            var mapdb = DBCDInstance.Load("Map", buildName);

            foreach (dynamic map in mapdb.Values)
            {
                var mapName = (string)map.Directory;
                //if (map.Directory != "PVPZone01")
                //    continue;

                //if (!string.IsNullOrEmpty(mapFilter) && map.Directory != mapFilter)
                //    continue;

                Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] " + mapName);

                // Load WDT
                Stream? wdtStream = null;

                try
                {
                    if (!mapdb.AvailableColumns.Contains("WdtFileDataID"))
                    {
                        wdtStream = TACTRepo.RootFile.OpenFile("world/maps/" + mapName + "/" + mapName + ".wdt", TACTRepo);
                    }
                    else
                    {
                        if (map.WdtFileDataID != 0)
                        {
                            wdtStream = TACTRepo.RootFile.OpenFile((uint)map.WdtFileDataID, TACTRepo);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to open WDT: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.ResetColor();
                }

                Dictionary<(sbyte x, sbyte y), RootRecord> toExtract = [];

                if (wdtStream != null)
                {
                    var minimapFDIDs = WDT.FileDataIdsFromWDT(wdtStream);
                    if (minimapFDIDs.Length == 0)
                    {
                        // Pre-MAID build, extract by filename
                        for (sbyte x = 0; x < 64; x++)
                        {
                            for (sbyte y = 0; y < 64; y++)
                            {
                                string tileName = "world/minimaps/" + mapName + "/map" + x.ToString().PadLeft(2, '0') + "_" + y.ToString().PadLeft(2, '0') + ".blp";
                                if (!TACTRepo.RootFile.ContainsFilename(tileName))
                                    continue;

                                var rootRecord = TACTRepo.RootFile.Get(tileName).FirstOrDefault();
                                if (rootRecord == null)
                                    continue;

                                var minimapName = "map" + x.ToString().PadLeft(2, '0') + "_" + y.ToString().PadLeft(2, '0') + ".blp";
                                toExtract.Add((x, y), rootRecord);

                                if (!Listfile.NameToFDIDMap.ContainsKey(tileName.ToLowerInvariant()))
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("!!!!! Non-listfile referenced minimap " + tileName);
                                    Console.ReadLine();
                                    Console.ResetColor();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Extract tiles by FDID
                        minimapFDIDs = minimapFDIDs.Where(chunk => chunk.fileDataId != 0).ToArray();
                        foreach (var minimap in minimapFDIDs)
                        {
                            if (!TACTRepo.RootFile.ContainsFileId(minimap.fileDataId))
                                continue;

                            var rootRecord = TACTRepo.RootFile.Get(minimap.fileDataId).FirstOrDefault();
                            if (rootRecord == null)
                                continue;

                            var minimapName = "map" + minimap.x.ToString().PadLeft(2, '0') + "_" + minimap.y.ToString().PadLeft(2, '0') + ".blp";
                            toExtract.Add((minimap.x, minimap.y), rootRecord);
                        }

                        // Extract everything known in listfile too just in case
                        for (sbyte x = 0; x < 64; x++)
                        {
                            for (sbyte y = 0; y < 64; y++)
                            {
                                var minimapName = "map" + x.ToString().PadLeft(2, '0') + "_" + y.ToString().PadLeft(2, '0') + ".blp";
                                var tileName = "world/minimaps/" + mapName + "/" + minimapName;

                                if (toExtract.ContainsKey((x, y)))
                                    continue;

                                if (Listfile.NameToFDIDMap.TryGetValue(tileName, out uint fdid))
                                {
                                    if (!TACTRepo.RootFile.ContainsFileId(fdid))
                                        continue;

                                    var rootRecord = TACTRepo.RootFile.Get(fdid).FirstOrDefault();
                                    if (rootRecord == null)
                                        continue;

                                    toExtract.Add((x, y), rootRecord);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Extract everything known in listfile too just in case
                    for (sbyte x = 0; x < 64; x++)
                    {
                        for (sbyte y = 0; y < 64; y++)
                        {
                            if (toExtract.ContainsKey((x, y)))
                                continue;

                            var minimapName = "map" + x.ToString().PadLeft(2, '0') + "_" + y.ToString().PadLeft(2, '0') + ".blp";
                            var tileName = "world/minimaps/" + mapName + "/" + minimapName;

                            if (Listfile.NameToFDIDMap.TryGetValue(tileName, out uint fdid))
                            {
                                if (!TACTRepo.RootFile.ContainsFileId(fdid))
                                    continue;

                                var rootRecord = TACTRepo.RootFile.Get(fdid).FirstOrDefault();
                                if (rootRecord == null)
                                    continue;

                                toExtract.Add((x, y), rootRecord);
                            }
                        }
                    }
                }

                // Fill up the current version tile dictionary
                if (!currentVersion.maps.ContainsKey(mapName))
                    currentVersion.maps[mapName] = new MapManifest() { MaxX = "-1", MaxY = "-1", MinX = "-1", MinY = "-1", TileHashes = new string[64][] };

                for (byte x = 0; x < 64; x++)
                    currentVersion.maps[mapName].TileHashes[x] = new string[64];

                if (toExtract.Count == 0)
                {
                    Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t No tiles to extract for map " + mapName);
                    continue;
                }

                sbyte min_x = 64;
                sbyte min_y = 64;
                sbyte max_x = 0;
                sbyte max_y = 0;

                for (sbyte x = 0; x < 64; x++)
                {
                    for (sbyte y = 0; y < 64; y++)
                    {
                        if (toExtract.ContainsKey((x, y)))
                        {
                            if (x < min_x) { min_x = x; }
                            if (y < min_y) { min_y = y; }

                            if (x > max_x) { max_x = x; }
                            if (y > max_y) { max_y = y; }

                            currentVersion.maps[mapName].TileHashes[x][y] = toExtract[(x, y)].CKey.ToString();
                        }
                    }
                }

                currentVersion.maps[mapName].MinX = min_x.ToString();
                currentVersion.maps[mapName].MinY = min_y.ToString();
                currentVersion.maps[mapName].MaxX = max_x.ToString();
                currentVersion.maps[mapName].MaxY = max_y.ToString();

                // Decide whether or not to actually extract the minimaps for this map by comparing to files from previous build's map
                var extractTiles = !previousVersion.maps.ContainsKey(mapName);

                if (!extractTiles)
                {
                    if (previousVersion == null)
                        extractTiles = true;

                    if (!extractTiles)
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

                                    extractTiles = true;
                                    break;
                                }
                            }

                            if (extractTiles)
                                break;
                        }
                    }
                }

                if (extractTiles)
                {
                    if (!Directory.Exists(outdir))
                        Directory.CreateDirectory(outdir);

                    if (!Directory.Exists(Path.Combine(outdir, "compiled")))
                        Directory.CreateDirectory(Path.Combine(outdir, "compiled"));

                    if (!File.Exists(Path.Combine(outdir, "compiled", mapName + ".png")))
                        CompileMap(toExtract, mapName, outdir, min_x, min_y, max_x, max_y);
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

            // Save the build map
            File.WriteAllText(Path.Combine(BaseOutDir, "buildMap.json"), JsonSerializer.Serialize(buildMap, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static void CompileMap(Dictionary<(sbyte, sbyte), RootRecord> tiles, string mapName, string outDir, sbyte min_x, sbyte min_y, sbyte max_x, sbyte max_y)
        {
            var blpRes = 512;

            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Compiling map " + mapName + " (" + tiles.Count + " tiles)");

            var emptyTile = Image.Black(blpRes, blpRes);
            var mask = emptyTile.Equal(new[] { 0, 0, 0, 255 }).BandAnd();
            emptyTile = mask.Ifthenelse(new[] { 0, 0, 0, 0 }, emptyTile);

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
                                var minimapStream = TACTRepo.RootFile.OpenFile(rootRecord.FileId, TACTRepo);

                                if (minimapStream == null)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Unable to extract minimap " + rootRecord.FileId);
                                    Console.ResetColor();
                                    continue;
                                }

                                new BlpFile(minimapStream).GetBitmap(0).Save(stream, System.Drawing.Imaging.ImageFormat.Png);

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
        public static string MakeCDNPath(string basePath, string subFolder, string hash)
        {
            var path = Path.Combine(basePath, subFolder, hash.Substring(0, 2), hash.Substring(2, 2), hash);
            return path;
        }
    }

    public record VersionManifest
    {
        public string product { get; set; }
        public string version { get; set; }
        public string rootCKey { get; set; }
        public Dictionary<string, MapManifest> maps { get; set; }
    }

    public record MapManifest
    {
        public string MinX { get; set; }
        public string MinY { get; set; }
        public string MaxX { get; set; }
        public string MaxY { get; set; }
        public string[][] TileHashes { get; set; }
    }

    public record MapManifestNew
    {
        public sbyte MinX { get; set; }
        public sbyte MinY { get; set; }
        public sbyte MaxX { get; set; }
        public sbyte MaxY { get; set; }
        public string[][] TileHashes { get; set; }
    }
}

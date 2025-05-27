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

        private static JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

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

            Dictionary<string, string> buildMap = [];

            if (File.Exists(Path.Combine(BaseOutDir, "buildMap.json")))
                buildMap = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(BaseOutDir, "buildMap.json")));

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

            if(!buildMap.ContainsKey(buildName))
                buildMap[buildName] = rootKey;

            File.WriteAllText(Path.Combine(BaseOutDir, "buildMap.json"), JsonSerializer.Serialize(buildMap, jsonOptions));

            var outdir = Path.Combine(BaseOutDir, rootKey);

            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);

            if (!Directory.Exists(Path.Combine(outdir, "maps")))
                Directory.CreateDirectory(Path.Combine(outdir, "maps"));

            if (!Directory.Exists(Path.Combine(outdir, "compiled")))
                Directory.CreateDirectory(Path.Combine(outdir, "compiled"));

            currentVersion = new VersionManifest();

            if (File.Exists(Path.Combine(outdir, "versionManifest.json")))
            {
                var existingVerison = TryLoadVersionManifest(rootKey, false);
                if (existingVerison != null)
                    currentVersion = existingVerison;
            }
            else
            {
                currentVersion.version = buildName;
                currentVersion.product = product;
                currentVersion.rootCKey = rootKey;
                currentVersion.maps = new();
            }

            if (Directory.Exists(outdir) && Directory.GetFiles(outdir).Length > 0)
            {
                Console.WriteLine("Output directory already exists, skipping build " + buildName);
                File.WriteAllText(Path.Combine(BaseOutDir, "buildMap.json"), JsonSerializer.Serialize(buildMap, jsonOptions));
                return;
            }

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

            // TODO: This misses maps where minimaps are in the listfile but not in DB2

            var targetMaps = new List<MapTarget>();

            foreach (dynamic map in mapdb.Values)
            {
                var mapName = (string)map.Directory;

                if (!mapdb.AvailableColumns.Contains("WdtFileDataID"))
                {
                    targetMaps.Add(new MapTarget { ID = (int)map.ID, MapName = (string)map.MapName_lang, Directory = (string)map.Directory, WdtFileDataID = 0 });
                }
                else
                {
                    targetMaps.Add(new MapTarget { ID = (int)map.ID, MapName = (string)map.MapName_lang, Directory = (string)map.Directory, WdtFileDataID = (uint)map.WdtFileDataID });
                }
            }

            foreach (var map in Listfile.ListfileMaps)
            {
                if (targetMaps.Any(m => m.Directory.ToLowerInvariant() == map.ToLowerInvariant()))
                    continue;

                targetMaps.Add(new MapTarget { ID = -1, MapName = map, Directory = map, WdtFileDataID = 0 });
            }

            foreach (var targetMap in targetMaps)
            {
                var mapName = targetMap.Directory;

                // Yikes
                if (mapName == "artifact???dalaranvaultacquisition")
                    continue;

                //if (map.Directory != "PVPZone01")
                //    continue;

                //if (!string.IsNullOrEmpty(mapFilter) && map.Directory != mapFilter)
                //    continue;

                if (targetMap.ID != -1)
                    Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] " + mapName);

                // Load WDT
                Stream? wdtStream = null;

                try
                {
                    if (targetMap.WdtFileDataID == 0)
                    {
                        wdtStream = TACTRepo.RootFile.OpenFile("world/maps/" + mapName + "/" + mapName + ".wdt", TACTRepo);
                    }
                    else
                    {
                        wdtStream = TACTRepo.RootFile.OpenFile(targetMap.WdtFileDataID, TACTRepo);
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

                #region Tile finding
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

                                if (Listfile.NameToFDIDMap.TryGetValue(tileName.ToLower(), out uint fdid))
                                {
                                    if (!TACTRepo.RootFile.ContainsFileId(fdid))
                                        continue;

                                    var rootRecord = TACTRepo.RootFile.Get(fdid).FirstOrDefault();
                                    if (rootRecord == null)
                                        continue;

                                    //Console.WriteLine("Adding listfile-only tile " + fdid + ": " + mapName + "_" + x + "_" + y);
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

                            if (Listfile.NameToFDIDMap.TryGetValue(tileName.ToLower(), out uint fdid))
                            {
                                if (!TACTRepo.RootFile.ContainsFileId(fdid))
                                    continue;

                                var rootRecord = TACTRepo.RootFile.Get(fdid).FirstOrDefault();
                                if (rootRecord == null)
                                    continue;

                                //Console.WriteLine("Adding listfile-only tile " + fdid + ": " + mapName + "_" + x + "_" + y);
                                toExtract.Add((x, y), rootRecord);
                            }
                        }
                    }
                }
                #endregion

                sbyte min_x = 64;
                sbyte min_y = 64;
                sbyte max_x = 0;
                sbyte max_y = 0;

                var tileHashes = new string[64][];
                for (byte x = 0; x < 64; x++)
                    tileHashes[x] = new string[64];

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

                            tileHashes[x][y] = toExtract[(x, y)].CKey.ToString();
                        }
                    }
                }

                if (toExtract.Count == 0)
                {
                    if (targetMap.ID != -1)
                        Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t No tiles to extract for map " + mapName);

                    continue;
                }

                // Check if we need to actually compile this map
                var compileMap = false;

                var currentVersionMatches = false;

                // Compile map if it doesn't exist in currentVersion & add the map
                if (!currentVersion.maps.TryGetValue(mapName, out MapManifest? currentMap))
                {
                    Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Adding map " + mapName + " to current version");

                    currentVersion.maps.Add(mapName, new MapManifest()
                    {
                        WDTFileDataID = targetMap.WdtFileDataID != 0 ? (int)targetMap.WdtFileDataID : null,
                        InternalMapID = targetMap.ID,
                        MapName = targetMap.MapName,
                        MaxX = max_x,
                        MaxY = max_y,
                        MinX = min_x,
                        MinY = min_y,
                        TileHashes = tileHashes
                    });
                }
                else
                {
                    // Compile if the map is in the current version manifest but the tile hashes differ
                    if (DoTilesDiffer(mapName, currentMap.TileHashes, tileHashes))
                    {
                        Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Triggering compile for map " + mapName + " as tile hashes differ from current version");
                        compileMap = true;
                    }
                    else
                    {
                        currentVersionMatches = true;
                    }

                    // Set current version tile hashes as we don't need the old ones anymore
                    currentVersion.maps[mapName].TileHashes = tileHashes;
                }

                if (!compileMap)
                {
                    if (previousVersion != null && previousVersion.maps.TryGetValue(mapName, out var previousMap))
                    {
                        // Compile if the map is in the previous version manifest but the tile hashes differ
                        if (DoTilesDiffer(mapName, previousMap.TileHashes, tileHashes))
                        {
                            // TODO: This always triggers compiles even if the intended version is already on disk
                            if (!currentVersionMatches)
                            {
                                Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Triggering compile for map " + mapName + " as tile hashes differ from previous version");
                                compileMap = true;
                            }
                            else
                            {
                                Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Skipping compile for map " + mapName + ", it was different in previous version but current version already matches what we were going to compile");
                            }
                        }
                    }
                    else
                    {
                        // Previous version does not have this map or isn't set at all and current version doesn't match
                        if (!currentVersionMatches)
                        {
                            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Triggering compile for map " + mapName + " as it was not in previous version");
                            compileMap = true;
                        }
                        else
                        {
                            //Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Skipping compile for map " + mapName + ", it was not in previous version but matches current version");
                        }
                    }
                }

                if (compileMap)
                    CompileMap(toExtract, mapName, outdir, min_x, min_y, max_x, max_y);

                File.WriteAllText(Path.Combine(outdir, "maps", mapName + ".json"), JsonSerializer.Serialize(currentVersion.maps[mapName], jsonOptions));
            }

            // Don't save maps to version manifest
            currentVersion.maps = [];

            // Save the version manifest
            File.WriteAllText(Path.Combine(outdir, "versionManifest.json"), JsonSerializer.Serialize(currentVersion, jsonOptions));

            // Save the build map
            File.WriteAllText(Path.Combine(BaseOutDir, "buildMap.json"), JsonSerializer.Serialize(buildMap, jsonOptions));
        }

        private static bool DoTilesDiffer(string mapName, string[][] oldTiles, string[][] newTiles)
        {
            for (byte x = 0; x < 64; x++)
            {
                for (byte y = 0; y < 64; y++)
                {
                    if (oldTiles[x][y] != newTiles[x][y])
                    {
                        if (oldTiles[x][y] == null)
                        {
                            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t (" + x + "," + y + ") null != " + newTiles[x][y].ToString());
                            return true;
                        }
                        else if (newTiles[x][y] == null)
                        {
                            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t (" + x + "," + y + ") " + oldTiles[x][y] + " != null");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t (" + x + "," + y + ") " + oldTiles[x][y] + " != " + newTiles[x][y].ToString());
                            return true;
                        }
                    }
                }
            }

            //Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t All hashes match.");
            return false;
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
            if(!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);

            compiled.Dzsave(outdir, mapName, Enums.ForeignDzLayout.Google, ".png", background: [0, 0, 0, 0]);
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "]\t Done, took " + timer.ElapsedMilliseconds + "ms");

            timer.Stop();
        }

        public static VersionManifest? TryLoadVersionManifest(string buildRootKey, bool isPrevious)
        {
            var outdir = Path.Combine(BaseOutDir, buildRootKey);
            if (!Directory.Exists(outdir))
                return null;

            var versionManifestPath = Path.Combine(outdir, "versionManifest.json");
            if (!File.Exists(versionManifestPath))
                return null;

            var versionManifest = JsonSerializer.Deserialize<VersionManifest>(File.ReadAllText(versionManifestPath));
            versionManifest.maps = new();

            foreach (var map in Directory.GetFiles(Path.Combine(outdir, "maps"), "*.json"))
            {
                MapManifest mapManifest;

                try
                {
                    mapManifest = JsonSerializer.Deserialize<MapManifest>(File.ReadAllText(map));
                }
                catch (JsonException e)
                {
                    Console.WriteLine("Error: Failed to parse map manifest " + map + " in build " + buildRootKey + ", trying to parse as old manifest instead..");
                    var oldMapManifest = JsonSerializer.Deserialize<MapManifestOld>(File.ReadAllText(map));
                    mapManifest = new MapManifest
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

                    Console.WriteLine("Rewriting old manifest");

                    File.WriteAllText(map, JsonSerializer.Serialize(mapManifest, jsonOptions));
                }

                var mapName = Path.GetFileNameWithoutExtension(map);

                if (mapManifest == null || (mapManifest.MinX == -1 && mapManifest.MinY == -1 && mapManifest.MaxX == -1 && mapManifest.MaxY == -1))
                {
                    //Console.WriteLine("Skipping map " + mapName + " as it has no tiles");
                }
                else
                {
                    versionManifest.maps[mapName] = new MapManifest
                    {
                        MinX = mapManifest.MinX,
                        MinY = mapManifest.MinY,
                        MaxX = mapManifest.MaxX,
                        MaxY = mapManifest.MaxY,
                        MapName = mapManifest.MapName,
                        InternalMapID = mapManifest.InternalMapID,
                        WDTFileDataID = mapManifest.WDTFileDataID,
                        TileHashes = mapManifest.TileHashes
                    };
                }
            }

            Console.WriteLine("Loaded build " + versionManifest.version + " and " + versionManifest.maps.Count + " maps" + (isPrevious ? " as previous version" : " as current version"));

            if (isPrevious)
                previousVersion = versionManifest;

            return versionManifest;
        }

        public static string MakeCDNPath(string basePath, string subFolder, string hash)
        {
            var path = Path.Combine(basePath, subFolder, hash.Substring(0, 2), hash.Substring(2, 2), hash);
            return path;
        }
    }

    public record MapTarget
    {
        public int ID { get; set; }
        public string MapName { get; set; }
        public string Directory { get; set; }
        public uint WdtFileDataID { get; set; }
    }

    public record VersionManifest
    {
        public string product { get; set; }
        public string version { get; set; }
        public string rootCKey { get; set; }
        public Dictionary<string, MapManifest> maps { get; set; }
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

    public record MapManifest
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

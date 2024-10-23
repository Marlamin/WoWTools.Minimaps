using DBCD.Providers;
using System.Globalization;
using TACT.Net;
using TACT.Net.Configs;

namespace WoWTools.MinimapExtractTACT
{
    internal class Program
    {
        public static TACTRepo tactRepo;
        public static Dictionary<uint, string> Listfile = new();
        public static Dictionary<string, uint> ListfileReverse = new();

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Required arguments: wowtprdir outdir (mapFilter)");
            }

            var repoPath = args[0];
            var outdir = args[1];

            var mapFilter = "";
            if (args.Length == 3)
                mapFilter = args[2];

            var manifestPath = "manifests";

            tactRepo = new TACTRepo(repoPath)
            {
                ManifestContainer = new ManifestContainer("wow", Locale.US),
                ConfigContainer = new ConfigContainer()
            };

            // open the configs
            Console.WriteLine("[TACT.Net] Opening manifest at " + manifestPath + "...");
            tactRepo.ManifestContainer.OpenLocal(manifestPath);

            Console.WriteLine("[TACT.Net] Opening config at " + repoPath + "...");
            tactRepo.ConfigContainer.OpenLocal(tactRepo.BaseDirectory, tactRepo.ManifestContainer);

            // load the archives
            Console.WriteLine("[TACT.Net] Loading archives...");
            tactRepo.IndexContainer = new TACT.Net.Indices.IndexContainer();
            tactRepo.IndexContainer.Open(tactRepo.BaseDirectory, tactRepo.ConfigContainer);

            // open the encoding
            Console.WriteLine("[TACT.Net] Opening encoding...");
            tactRepo.EncodingFile = new TACT.Net.Encoding.EncodingFile(tactRepo.BaseDirectory, tactRepo.ConfigContainer.EncodingEKey);

            // get the root ckey
            Console.WriteLine("[TACT.Net] Getting root CKey...");
            if (!tactRepo.EncodingFile.TryGetCKeyEntry(tactRepo.ConfigContainer.RootCKey, out var rootCEntry))
                throw new Exception("Root CKey not found");

            // open the root
            Console.WriteLine("[TACT.Net] Opening root...");
            tactRepo.RootFile = new TACT.Net.Root.RootFile(tactRepo.BaseDirectory, rootCEntry.EKeys[0]);

            var configBuildName = tactRepo.ConfigContainer.BuildConfig.GetValue("build-name");
            var splitName = configBuildName.Replace("WOW-", "").Split("patch");
            var buildName = splitName[1].Split("_")[0] + "." + splitName[0];

            Console.WriteLine("[TACT.Net] Loaded build: " + configBuildName + " (" + buildName + ")");

            Console.WriteLine("Loading TACT keys..");
            try
            {
                var downloadKeys = false;
                if (File.Exists("TactKey.csv"))
                {
                    var info = new FileInfo("TactKey.csv");
                    if (info.Length == 0 || DateTime.Now.Subtract(TimeSpan.FromDays(1)) > info.LastWriteTime)
                    {
                        Console.WriteLine("TACT Keys outdated, redownloading..");
                        downloadKeys = true;
                    }
                }
                else
                {
                    downloadKeys = true;
                }

                if (downloadKeys)
                {
                    Console.WriteLine("Downloading TACT keys");

                    List<string> tactKeyLines = new();
                    using (var w = new HttpClient())
                    using (var s = w.GetStreamAsync("https://github.com/wowdev/TACTKeys/raw/master/WoW.txt").Result)
                    using (var sr = new StreamReader(s))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            if (string.IsNullOrEmpty(line))
                                continue;

                            var splitLine = line.Split(" ");
                            tactKeyLines.Add(splitLine[0] + ";" + splitLine[1]);
                        }
                    }

                    File.WriteAllLines("TactKey.csv", tactKeyLines);
                }


                foreach (var line in File.ReadAllLines("TactKey.csv"))
                {
                    var splitLine = line.Split(";");
                    if (splitLine.Length != 2)
                        continue;

                    TACT.Net.Cryptography.KeyService.TryAddKey(ulong.Parse(splitLine[0], NumberStyles.HexNumber), splitLine[1]);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred retrieving/loading TACT keys: " + e.Message);
            }

            Console.WriteLine("Loading listfile..");
            try
            {
                var downloadListfile = false;
                if (File.Exists("listfile.csv"))
                {
                    var info = new FileInfo("listfile.csv");
                    if (info.Length == 0 || DateTime.Now.Subtract(TimeSpan.FromDays(1)) > info.LastWriteTime)
                    {
                        Console.WriteLine("Listfile outdated, redownloading..");
                        downloadListfile = true;
                    }
                }
                else
                {
                    downloadListfile = true;
                }

                if (downloadListfile)
                {
                    Console.WriteLine("Downloading listfile");

                    using (var w = new HttpClient())
                    using (var s = w.GetStreamAsync("https://github.com/wowdev/wow-listfile/releases/latest/download/community-listfile.csv").Result)
                    {
                        using var fs = new FileStream("listfile.csv", FileMode.OpenOrCreate);
                        s.CopyTo(fs);
                    }
                }

                if (!File.Exists("listfile.csv"))
                {
                    throw new FileNotFoundException("Could not find listfile.csv");
                }

                foreach (var line in File.ReadAllLines("listfile.csv"))
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var splitLine = line.Split(";");
                    var fdid = uint.Parse(splitLine[0]);

                    if (!splitLine[1].StartsWith("world"))
                        continue;

                    if (!tactRepo.RootFile.ContainsFileId(fdid))
                        continue;

                    if (splitLine[1].StartsWith("world/minimaps") || splitLine[1].EndsWith(".wdt"))
                    {
                        Listfile[fdid] = splitLine[1];
                        ListfileReverse[splitLine[1]] = fdid;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred retrieving/loading listfile: " + e.Message);
            }

            // Set up DBCD
            var dbdProvider = new GithubDBDProvider();
            var dbcProvider = new CASCDBCProvider();
            var dbcd = new DBCD.DBCD(dbcProvider, dbdProvider);

            // Open Map DBC with local defs
            Console.WriteLine("Opening Map.db2..");

            var mapdb = dbcd.Load("Map");

            // Run through map db
            Console.WriteLine("Extracting tiles..");

            foreach (dynamic map in mapdb.Values)
            {
                if (!string.IsNullOrEmpty(mapFilter) && map.Directory != mapFilter)
                    continue;

                Console.WriteLine(map.Directory);

                // Load WDT
                Stream? wdtStream = null;

                try
                {
                    if (map.WdtFileDataID == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Skipping map " + map.Directory + " with no WDT!");
                        Console.ResetColor();
                        continue;
                    }

                    if (!mapdb.AvailableColumns.Contains("WdtFileDataID"))
                    {
                        Console.WriteLine("Older build detected: map record has no wdtFileDataID property, opening by filename!");
                        Console.ResetColor();
                        wdtStream = tactRepo.RootFile.OpenFile("world/maps/" + map.Directory + "/" + map.Directory + ".wdt", tactRepo);
                    }
                    else
                    {
                        wdtStream = tactRepo.RootFile.OpenFile((uint)map.WdtFileDataID, tactRepo);
                    }

                    if (wdtStream == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Skipping map " + map.Directory + " with unshipped WDT!");
                        Console.ResetColor();
                        continue;
                    }

                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to open WDT: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.ResetColor();
                }

                if (wdtStream == null)
                    continue;

                var minimapFDIDs = WDT.FileDataIdsFromWDT(wdtStream);
                if (minimapFDIDs.Count() == 0)
                {
                    // Pre-MAID build, extract by filename
                    for (var x = 0; x < 64; x++)
                    {
                        for (var y = 0; y < 64; y++)
                        {
                            string tileName = "world/minimaps/" + map.Directory + "/map" + x.ToString().PadLeft(2, '0') + "_" + y.ToString().PadLeft(2, '0') + ".blp";

                            if (!tactRepo.RootFile.ContainsFilename(tileName))
                                continue;

                            var minimapStream = tactRepo.RootFile.OpenFile(tileName, tactRepo);
                            if (minimapStream == null)
                            {
                                continue;
                            }

                            var minimapName = "map" + x.ToString().PadLeft(2, '0') + "_" + y.ToString().PadLeft(2, '0') + ".blp";
                            var minimapPath = Path.Combine(outdir, "world", "minimaps", map.Directory, minimapName);

                            Directory.CreateDirectory(Path.GetDirectoryName(minimapPath));

                            using (var fileStream = File.Create(minimapPath))
                            {
                                minimapStream.CopyTo(fileStream);
                            }
                        }
                    }
                }
                else
                {
                    List<uint> extractedFDIDs = new();

                    // Extract tiles by FDID
                    minimapFDIDs = minimapFDIDs.Where(chunk => chunk.fileDataId != 0).ToArray();
                    foreach (var minimap in minimapFDIDs)
                    {
                        if (!tactRepo.RootFile.ContainsFileId(minimap.fileDataId))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Skipping non-existent minimap " + minimap.fileDataId + " for tile " + minimap.x + "_" + minimap.y);
                            Console.ResetColor();
                            continue;
                        }

                        var minimapStream = tactRepo.RootFile.OpenFile(minimap.fileDataId, tactRepo);
                        if (minimapStream == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Unable to extract minimap " + minimap.fileDataId + " for tile " + minimap.x + "_" + minimap.y);
                            Console.ResetColor();
                            continue;
                        }

                        extractedFDIDs.Add(minimap.fileDataId);

                        var minimapName = "map" + minimap.x.ToString().PadLeft(2, '0') + "_" + minimap.y.ToString().PadLeft(2, '0') + ".blp";
                        var minimapPath = Path.Combine(outdir, "world", "minimaps", map.Directory, minimapName);

                        Directory.CreateDirectory(Path.GetDirectoryName(minimapPath));

                        using (var fileStream = File.Create(minimapPath))
                        {
                            minimapStream.CopyTo(fileStream);
                        }
                    }

                    for (var x = 0; x < 64; x++)
                    {
                        for (var y = 0; y < 64; y++)
                        {
                            string tileName = "world/minimaps/" + map.Directory + "/map" + x.ToString().PadLeft(2, '0') + "_" + y.ToString().PadLeft(2, '0') + ".blp";
                            if (ListfileReverse.TryGetValue(tileName, out var fdid))
                            {
                                if (extractedFDIDs.Contains(fdid))
                                    continue;

                                if(!tactRepo.RootFile.ContainsFileId(fdid))
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("Skipping non-existent minimap " + fdid + " for tile " + x + "_" + y);
                                    Console.ResetColor();
                                    continue;
                                }

                                var minimapStream = tactRepo.RootFile.OpenFile(fdid, tactRepo);
                                if (minimapStream == null)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Unable to extract minimap " + fdid + " for tile " + x + "_" + y);
                                    Console.ResetColor();
                                    continue;
                                }

                                Console.WriteLine("Extracted non-WDT referenced minimap " + fdid + " for tile " + x + "_" + y);
                                extractedFDIDs.Add(fdid);

                                var minimapName = "map" + x.ToString().PadLeft(2, '0') + "_" + y.ToString().PadLeft(2, '0') + ".blp";
                                var minimapPath = Path.Combine(outdir, "world", "minimaps", map.Directory, minimapName);

                                Directory.CreateDirectory(Path.GetDirectoryName(minimapPath));

                                using (var fileStream = File.Create(minimapPath))
                                {
                                    minimapStream.CopyTo(fileStream);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

using CASCLib;
using DBCD.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace WoWTools.MinimapExtract
{
    class Program
    {
        public static Dictionary<int, string> Listfile = new();
        public static CASCHandler cascHandler;
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Required arguments: wowProduct outdir (wowPath)");
            }

            var wowProduct = args[0];
            var outdir = args[1];

            string wowPath = null;
            if (args.Length > 2)
            {
                wowPath = args[2];
            }

            var mapFilter = "";
            if (args.Length == 4)
            {
                mapFilter = args[3];
            }

            CASCConfig.LoadFlags &= ~(LoadFlags.Download | LoadFlags.Install);
            CASCConfig.ValidateData = false;
            CASCConfig.ThrowOnFileNotFound = false;

            if (wowPath == null)
            {
                Console.WriteLine("Initializing CASC from web for program " + wowProduct);
                cascHandler = CASCHandler.OpenOnlineStorage(wowProduct, "eu");
            }
            else
            {
                wowPath = wowPath.Replace("_retail_", "").Replace("_ptr_", "");
                Console.WriteLine("Initializing CASC from local disk with basedir " + wowPath + " and program " + wowProduct);
                cascHandler = CASCHandler.OpenLocalStorage(wowPath, wowProduct);
            }

            var splitName = cascHandler.Config.BuildName.Replace("WOW-", "").Split("patch");
            var buildName = splitName[1].Split("_")[0] + "." + splitName[0];

            cascHandler.Root.SetFlags(LocaleFlags.enUS);

            var availableFDIDs = new HashSet<int>();
            
            if (cascHandler.Root is WowTVFSRootHandler wtrh)
                availableFDIDs = wtrh.RootEntries.Keys.ToHashSet();
            else if (cascHandler.Root is WowRootHandler wrh)
                availableFDIDs = wrh.RootEntries.Keys.ToHashSet();

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
                }

                KeyService.LoadKeys();
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
                    var fdid = int.Parse(splitLine[0]);

                    if (!splitLine[1].StartsWith("world"))
                        continue;

                    if (!availableFDIDs.Contains(fdid))
                        continue;
                    
                    if (splitLine[1].StartsWith("world/minimaps") || splitLine[1].EndsWith(".wdt"))
                        Listfile[fdid] = splitLine[1];
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

            // This will currently crash on partially encrypted DBs with unknown keys due to https://github.com/wowdev/TACT.Net/issues/12
            var mapdb = dbcd.Load("Map");

            // Run through map db
            Console.WriteLine("Extracting tiles..");

            foreach (dynamic map in mapdb.Values)
            {
                if (!string.IsNullOrEmpty(mapFilter) && map.Directory != mapFilter)
                    continue;

                Console.WriteLine(map.Directory);

                // Load WDT
                Stream wdtStream;

                try
                {
                    if (map.WdtFileDataID == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Skipping map " + map.Directory + " with no WDT!");
                        Console.ResetColor();
                        continue;
                    }

                    wdtStream = cascHandler.OpenFile((int)map.WdtFileDataID);
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Older build detected: map record has no wdtFileDataID property");
                    Console.ResetColor();
                    wdtStream = cascHandler.OpenFile("world/maps/" + map.Directory + "/" + map.Directory + ".wdt");
                }

                if (wdtStream == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Skipping map " + map.Directory + " with unshipped WDT!");
                    Console.ResetColor();
                    continue;
                }

                var minimapFDIDs = WDT.FileDataIdsFromWDT(wdtStream);
                if (minimapFDIDs.Count() == 0)
                {
                    // Pre-MAID build, extract by filename
                    for (var x = 0; x < 64; x++)
                    {
                        for (var y = 0; y < 64; y++)
                        {
                            string tileName = "world/minimaps/" + map.Directory + "/map" + x.ToString().PadLeft(2, '0') + "_" + y.ToString().PadLeft(2, '0') + ".blp";
                            var minimapStream = cascHandler.OpenFile(tileName);
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
                    // Extract tiles by FDID
                    minimapFDIDs = minimapFDIDs.Where(chunk => chunk.fileDataId != 0).ToArray();
                    foreach (var minimap in minimapFDIDs)
                    {
                        var minimapStream = cascHandler.OpenFile((int)minimap.fileDataId);
                        if (minimapStream == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Unable to extract minimap " + minimap.fileDataId + " for tile " + minimap.x + "_" + minimap.y);
                            Console.ResetColor();
                            continue;
                        }

                        var minimapName = "map" + minimap.x.ToString().PadLeft(2, '0') + "_" + minimap.y.ToString().PadLeft(2, '0') + ".blp";
                        var minimapPath = Path.Combine(outdir, "world", "minimaps", map.Directory, minimapName);

                        Directory.CreateDirectory(Path.GetDirectoryName(minimapPath));

                        using (var fileStream = File.Create(minimapPath))
                        {
                            minimapStream.CopyTo(fileStream);
                        }
                    }
                }
            }

            // Append any filenames from listfile for additional non-WDT referenced minimaps?
        }
    }
}

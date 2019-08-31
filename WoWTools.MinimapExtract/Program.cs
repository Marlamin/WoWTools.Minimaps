using DBCD.Providers;
using System;
using System.IO;
using System.Linq;
using TACT.Net;
using TACT.Net.Configs;

namespace WoWTools.MinimapExtract
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                throw new ArgumentException("Required arguments: cdndir, buildconfig, cdnconfig, outdir");
            }

            var cdndir = args[0];
            var bc = args[1];
            var cdnc = args[2];
            var outdir = args[3];

            var mapFilter = "";
            if (args.Length == 5)
            {
                mapFilter = args[4];
            }

            // Open storage for specific build
            TACTRepo tactRepo = new TACTRepo(cdndir)
            {
                ConfigContainer = new ConfigContainer("wowt", Locale.US)
            };

            Console.WriteLine("Loading configs..");
            tactRepo.ConfigContainer.OpenConfigs(tactRepo.BaseDirectory, bc, cdnc);

            Console.WriteLine("Loading indices..");
            tactRepo.IndexContainer = new TACT.Net.Indices.IndexContainer();
            tactRepo.IndexContainer.Open(tactRepo.BaseDirectory);

            Console.WriteLine("Loading encoding..");
            tactRepo.EncodingFile = new TACT.Net.Encoding.EncodingFile(tactRepo.BaseDirectory, tactRepo.ConfigContainer.EncodingEKey);

            Console.WriteLine("Looking up root..");
            tactRepo.EncodingFile.TryGetCKeyEntry(tactRepo.ConfigContainer.RootCKey, out var rootCEntry);

            Console.WriteLine("Loading root..");
            tactRepo.RootFile = new TACT.Net.Root.RootFile(tactRepo.BaseDirectory, rootCEntry.EKey);

            // Set up DBCD
            var dbdProvider = new GithubDBDProvider();
            var dbcProvider = new TACTDBCProvider(tactRepo);
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
                if (map.WdtFileDataID == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Skipping map " + map.Directory + " with no WDT!");
                    Console.ResetColor();
                    continue;
                }

                var wdtStream = tactRepo.RootFile.OpenFile((uint)map.WdtFileDataID, tactRepo);
                if (wdtStream == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Skipping map " + map.Directory + " with unshipped WDT!");
                    Console.ResetColor();
                    continue;
                }

                var minimapFDIDs = WDT.FileDataIdsFromWDT(wdtStream);
                minimapFDIDs = minimapFDIDs.Where(chunk => chunk.fileDataId != 0).ToArray();

                // Extract tiles
                foreach (var minimap in minimapFDIDs)
                {
                    var minimapStream = tactRepo.RootFile.OpenFile(minimap.fileDataId, tactRepo);
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

            // Append any filenames from listfile for additional non-WDT referenced minimaps?
        }
    }
}

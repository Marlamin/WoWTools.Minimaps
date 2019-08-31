using DBCD.Providers;
using System;
using System.IO;
using TACT.Net;
using TACT.Net.Configs;

namespace WoWTools.MinimapExtract
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = Path.Combine("E:", "cdn");
            var bc = "24cfe3b7a917e4b05c8a180ef8a13ae3";
            var cdnc = "ba42282b283c46f43b96dc4c3465f321";

            // Open storage for specific build
            TACTRepo tactRepo = new TACTRepo(dir)
            {
                ConfigContainer = new ConfigContainer("wowt", Locale.US)
            };

            Console.WriteLine("Loading configs..");
            tactRepo.ConfigContainer.OpenConfigs(dir, bc, cdnc);

            Console.WriteLine("Loading indices..");
            tactRepo.IndexContainer = new TACT.Net.Indices.IndexContainer();
            tactRepo.IndexContainer.Open(dir);

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

            // Run through maps/WDTs, generate filenames

            // Append any filenames from listfile for additional non-WDT referenced minimaps?
        }
    }
}

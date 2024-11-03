using DBDefsLib;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TACT.Net;

namespace WoWTools.MinimapTool
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if(args.Length < 2)
            {
                PrintHelp();
                return;
            }

            var mode = args[0];
            switch (mode)
            {
                case "generate":
                    var submode = args[1];
                    switch (submode.ToLower())
                    {
                        case "tact":
                            // Example for my setup: generate tact \\martin-nas\Raid2024\tpr\wow \\martin-nas\main\Minimaps\Work\Raw 1.15.4.57134 "D:\\Projects\\BuildBackup\\bin\\Debug\\net7.0"
                            if (args.Length < 4)
                            {
                                Console.WriteLine("Missing arguments for TACT mode");
                                PrintHelp();
                                return;
                            }

                            var repoPath = args[2];
                            var baseoutdir = args[3];

                            var buildFilter = "all";
                            if (args.Length >= 5)
                                buildFilter = args[4];

                            string buildBackupPath = "";
                            if (args.Length == 6)
                                buildBackupPath = args[5];

                            await ProcessTACT(repoPath, baseoutdir, buildFilter, buildBackupPath);
                            break;
                        case "raw":
                            // Example for my setup: generate raw M:\Minimaps\RawTiles \\martin-nas\main\Minimaps\Work\Raw
                            if (args.Length < 4)
                            {
                                Console.WriteLine("Missing arguments for RAW mode");
                                PrintHelp();
                                return;
                            }

                            var inputFolder = args[2];
                            var outdir = args[3];

                            await ProcessRaw(inputFolder, outdir);
                            break;
                        default:
                            Console.WriteLine("Unknown sub mode: " + mode);
                            PrintHelp();
                            break;
                    }
                    break;
                default:
                    Console.WriteLine("Unknown mode: " + mode);
                    PrintHelp();
                    break;
            }
           
        }

        private static async Task ProcessRaw(string inputFolder, string baseoutdir)
        {
            var lastRootKey = "";

            RawProcessor.Start(baseoutdir);

            foreach (var directory in Directory.GetDirectories(inputFolder))
            {
                // Example: M:\Minimaps\RawTiles\0.5.3.3368\World\Minimaps etc
                var build = Path.GetFileName(directory);

                var splitBuild = build.Split('.');
                if (splitBuild.Length != 4)
                {
                    Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [Raw] Build " + build + " is not a valid formatted build. Skipping folder.");
                    continue;
                }

                var buildObj = new Build(build);

                var buildRootKey = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(buildObj.ToString()))).ToLower();
                Console.WriteLine(buildObj.ToString() + " (" + buildRootKey + ")");

                if (lastRootKey != "" && buildRootKey != lastRootKey)
                    RawProcessor.TryLoadVersionManifest(lastRootKey);

                try
                {
                    if (!Directory.Exists(Path.Combine(baseoutdir, buildRootKey, "maps")))
                        RawProcessor.ProcessBuild(directory, buildObj);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to process build " + buildObj.build + ": " + e.ToString());
                    Console.WriteLine(e.StackTrace);
                    Console.ResetColor();
                }

                lastRootKey = buildRootKey;
            }
        }

        private static async Task ProcessTACT(string repoPath, string baseoutdir, string buildFilter, string buildBackupPath)
        {
            var http = new HttpClient();
            var response = await http.GetAsync("https://wago.tools/api/builds");
            var json = await response.Content.ReadAsStringAsync();
            var wagoBuilds = JsonSerializer.Deserialize<Dictionary<string, List<WagoBuild>>>(json);

            var fullrun = true;

            var builds = new List<OurBuild>();
            foreach (var product in wagoBuilds)
            {
                foreach (var build in product.Value)
                {
                    if (buildFilter != "all" && build.version != buildFilter)
                        continue;

                    builds.Add(new OurBuild
                    {
                        product = product.Key,
                        build = new Build(build.version),
                        build_config = build.build_config,
                        cdn_config = build.cdn_config
                    });
                }
            }

            TACTProcessor.Start(baseoutdir, repoPath);
            var cdnConfigs = new List<string>();

            if (fullrun)
            {
                builds = builds.OrderBy(x => x.build).ToList();

                foreach (var build in builds)
                {
                    var cdnConfigPath = TACTProcessor.MakeCDNPath(repoPath, "config", build.cdn_config);
                    if (!File.Exists(cdnConfigPath))
                    {
                        Console.WriteLine("CDN CONFIG " + build.cdn_config + " MISSING");
                        continue;
                    }

                    if (!cdnConfigs.Contains(build.cdn_config))
                        cdnConfigs.Add(build.cdn_config);
                }
            }
            else
            {
                builds = new();
                builds.Add(new OurBuild()
                {
                    build = new Build("11.0.7.57361"),
                    build_config = "99b7ec78e8f0a68acf25146e4dbea7a9",
                    cdn_config = "220046cb50c6bef1112bf09ea8ef2aff",
                    product = "wowt"

                });

                cdnConfigs.Add("220046cb50c6bef1112bf09ea8ef2aff");

                if (!File.Exists(builds[0].build_config) || !File.Exists(builds[0].cdn_config))
                {
                    if (string.IsNullOrEmpty(buildBackupPath))
                    {
                        Console.WriteLine("BuildBackup path not provided, can not download files");
                        return;
                    }

                    Console.WriteLine("Missing config files for build " + builds[0].build);
                    var bbProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            WorkingDirectory = buildBackupPath,
                            FileName = Path.Combine(buildBackupPath, "BuildBackup.exe"),
                            Arguments = $"forcebuild wow {builds[0].build_config} {builds[0].cdn_config}",
                            UseShellExecute = false
                        }
                    };

                    bbProcess.Start();
                    bbProcess.WaitForExit();

                    if (!File.Exists(builds[0].build_config) || !File.Exists(builds[0].cdn_config))
                    {
                        Console.WriteLine("Failed to download config files for build " + builds[0].build);
                        return;
                    }
                }
            }

            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [TACT] Warming up indices..");
            TACTProcessor.WarmUpIndexes(cdnConfigs);

            var lastRootKey = "";
            foreach (var build in builds)
            {
                var buildConfigPath = TACTProcessor.MakeCDNPath(repoPath, "config", build.build_config);
                var cdnConfigPath = TACTProcessor.MakeCDNPath(repoPath, "config", build.cdn_config);

                if (!File.Exists(buildConfigPath) || !File.Exists(cdnConfigPath))
                {
                    if (string.IsNullOrEmpty(buildBackupPath))
                    {
                        Console.WriteLine("BuildBackup path not provided, can not download files");
                        return;
                    }

                    Console.WriteLine("Missing config files for build " + build.build);
                    var bbProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            WorkingDirectory = buildBackupPath,
                            FileName = Path.Combine(buildBackupPath, "BuildBackup.exe"),
                            Arguments = $"forcebuild wow {build.build_config} {build.cdn_config}",
                            UseShellExecute = false
                        }
                    };

                    bbProcess.Start();
                    bbProcess.WaitForExit();

                    if (!File.Exists(buildConfigPath) || !File.Exists(cdnConfigPath))
                    {
                        Console.WriteLine("Failed to download config files for build " + build.build);
                        continue;
                    }
                }

                var buildRootKey = GetRootKeyFromConfig(buildConfigPath);
                Console.WriteLine(build.build + " (" + buildRootKey + ")");

                if (lastRootKey != "" && buildRootKey != lastRootKey)
                    TACTProcessor.TryLoadVersionManifest(lastRootKey);

                try
                {
                    if (!Directory.Exists(Path.Combine(baseoutdir, buildRootKey, "maps")))
                        TACTProcessor.ProcessBuild(build.build_config, build.cdn_config, build.product, build.build);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to process build " + build.build + ": " + e.ToString());
                    Console.WriteLine(e.StackTrace);
                    Console.ResetColor();
                }

                lastRootKey = buildRootKey;
            }
        }

        private static string GetRootKeyFromConfig(string path)
        {
            var config = File.ReadAllText(path);
            var rootKey = config.Split("\n").First(x => x.Contains("root")).Split(" = ")[1];
            return rootKey;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Required arguments: <mode> <mode-specific arguments>");
            Console.WriteLine("Modes:");
            Console.WriteLine("  generate");
            Console.WriteLine("     TACT <inputdir> <outdir> (buildfilter) (buildbackupdir)>");
            Console.WriteLine("          inputdir: Path to the TACT repository (e.g. tpr/wow). Local installations are not supported.");
            Console.WriteLine("          outdir: Path to the output directory.");
            Console.WriteLine("          buildfilter: Optional filter for builds to process. If not provided or 'all', all builds will be processed.");
            Console.WriteLine("          buildbackupdir: Optional path to the directory containing BuildBackup.exe to download missing data.");
            Console.WriteLine("     RAW  <inputdir> <outdir>");
            Console.WriteLine("          inputdir: Path to the input folder (structured as <inputdir>/<build formatted as x.x.x.xxxxx>/World/Minimaps).");
            Console.WriteLine("          outdir: Path to the output directory (generated folder will be MD5 of specified build).");
        }
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

    class OurBuild
    {
        public string product { get; set; }
        public Build build { get; set; }
        public string build_config { get; set; }
        public string cdn_config { get; set; }
    }
}

using DBDefsLib;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WoWTools.MinimapTool
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 2)
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
                        case "purge":
                            // TODO: Check all builds in order for any duplicate maps/data that got backfilled in and clean that up
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
                    RawProcessor.TryLoadVersionManifest(lastRootKey, true);

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

                    var buildObj = new Build(build.version);

                    builds.Add(new OurBuild
                    {
                        product = product.Key,
                        build = buildObj,
                        build_config = build.build_config,
                        cdn_config = build.cdn_config
                    });
                }
            }

            builds.Add(new OurBuild()
            {
                product = "wow_beta",
                build = new Build("6.0.1.18125"),
                build_config = "806f4fd265de05a9b328310fcc42eed0",
                cdn_config = "b225a5d105393ef582066157f7dd34fc"
            });

            builds.Add(new OurBuild()
            {
                product = "wow_beta",
                build = new Build("6.0.1.18156"),
                build_config = "8468c48e74b9d437b94eb95fbf2c4dbd",
                cdn_config = "19742c0a2341a0dd29dcf92e39e81e8e"
            });


            builds.Add(new OurBuild()
            {
                product = "wow_beta",
                build = new Build("6.0.1.18164"),
                build_config = "d0ef88a73642ac58d3129fefad33ec69",
                cdn_config = "8decbf9979d0667d7e96f0189fb1bcbc"
            });

            builds.Add(new OurBuild()
            {
                product = "wow_beta",
                build = new Build("6.0.1.18179"),
                build_config = "21c40fcbe3a2181df39c42c9d3190564",
                cdn_config = "441f39cca8ee0b3b94a8101608e2ba0e"
            });

            builds.Add(new OurBuild()
            {
                product = "wow_beta",
                build = new Build("6.0.1.18297"),
                build_config = "eb2c81a44bcb438aee7bb150ddf87012",
                cdn_config = "441f39cca8ee0b3b94a8101608e2ba0e"
            });

            builds.Add(new OurBuild()
            {
                product = "wow_beta",
                build = new Build("6.0.1.18322"),
                build_config = "81693b4699edf1a78c110608b8e8264f",
                cdn_config = "070913e104dcb1dc02c67344b078563c"
            });

            builds.Add(new OurBuild()
            {
                product = "wowxptr",
                build = new Build("11.1.0.58221"),
                build_config = "5e87ddc0854f3027f6eaeb37ce25022c",
                cdn_config = "df7dce36f1b12973c0bd7a5ec7ad46a7"
            });

            TACTProcessor.Start(baseoutdir, repoPath);
            var cdnConfigs = new List<string>();

            if (fullrun)
            {
                builds = builds.OrderBy(x => x.build).ToList();

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

                var bbRan = false;

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
                    TACTProcessor.TryLoadVersionManifest(lastRootKey, true);

                try
                {
                    if (buildRootKey != lastRootKey)
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

using DBDefsLib;
using System.Diagnostics;
using System.Text.Json;

namespace WoWTools.MinimapTool
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException("Required arguments: wowtprdir outdir (buildbackupdir)");

            var repoPath = args[0];
            var baseoutdir = args[1];

            string buildBackupPath = "";

            if(args.Length > 2)
                buildBackupPath = args[2];

            //var mapFilter = "";
            //if (args.Length == 3)
            //    mapFilter = args[2];

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
                    if(string.IsNullOrEmpty(buildBackupPath))
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
                catch(Exception e)
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

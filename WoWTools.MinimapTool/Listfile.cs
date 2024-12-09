namespace WoWTools.MinimapTool
{
    public class Listfile
    {
        public static Dictionary<uint, string> FDIDMap = [];
        public static Dictionary<string, uint> NameToFDIDMap = [];
        public static List<string> ListfileMaps = [];

        public static void Load()
        {
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [TACT] Loading listfile..");
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
                    using (var s = w.GetStreamAsync("https://github.com/wowdev/wow-listfile/releases/latest/download/community-listfile-withcapitals.csv").Result)
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
                    var filename = splitLine[1].ToLowerInvariant();

                    if (!filename.StartsWith("world"))
                        continue;

                    if (filename.StartsWith("world/minimaps") || filename.EndsWith(".wdt"))
                    {
                        FDIDMap[fdid] = filename;
                        NameToFDIDMap[filename] = fdid;

                        var mapNameWithCaps = splitLine[1].Split("/")[2];
                        if(!ListfileMaps.Contains(mapNameWithCaps) && mapNameWithCaps.ToLowerInvariant() != "wmo")
                            ListfileMaps.Add(mapNameWithCaps);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred retrieving/loading listfile: " + e.Message);
            }
        }
    }
}

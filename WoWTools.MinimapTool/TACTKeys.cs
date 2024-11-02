using System.Globalization;

namespace WoWTools.MinimapTool
{
    public class TACTKeys
    {
        public static void Load()
        {
            Console.WriteLine("[" + DateTime.UtcNow.ToString() + "] [TACT] Loading TACT keys..");
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
        }
    }
}

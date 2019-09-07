using System;
using System.IO;

namespace WoWTools.MinimapTranslate
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = args[0];
            if (!Directory.Exists(dir))
            {
                throw new DirectoryNotFoundException("Could not find directory!");
            }

            if(!File.Exists(Path.Combine(dir, "textures", "Minimap", "md5translate.trs")))
            {
                throw new Exception("Could not find minimaptranslate.trs");
            }

            if(!Directory.Exists(Path.Combine(dir, "World", "Minimaps")))
            {
                Directory.CreateDirectory(Path.Combine(dir, "World", "Minimaps"));
            }

            foreach(var line in File.ReadAllLines(Path.Combine(dir, "textures", "Minimap", "md5translate.trs")))
            {
                if(line.Substring(0, 3) == "dir")
                {
                    // Directory
                    var targetDir = line.Remove(0, 5);

                    if (targetDir.Substring(0, 3).ToLower() == "wmo")
                        continue;

                    if (targetDir.Contains("\\")){
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Directory " + targetDir + " has subdirectories, skipping..");
                        Console.ResetColor();
                        continue;
                    }

                    if(!Directory.Exists(Path.Combine(dir, "World", "Minimaps", targetDir)))
                    {
                        Directory.CreateDirectory(Path.Combine(dir, "World", "Minimaps", targetDir));
                    }

                    Console.WriteLine(targetDir);
                }
                else
                {
                    if (line.Length == 0)
                        continue;

                    if(line.Substring(0, 4).ToLower() == "wmo\\")
                        continue;

                    var expl = line.Split('\t');
                    var targetFile = expl[0];
                    var sourceFile = expl[1];

                    Console.WriteLine(sourceFile + " => " + targetFile);

                    if (targetFile.StartsWith("Kalimdor\\Tanaris\\"))
                        continue;

                    if (!File.Exists(Path.Combine(dir, "World", "Minimaps", targetFile)))
                    {
                        File.Copy(Path.Combine(dir, "textures", "Minimap", sourceFile), Path.Combine(dir, "World", "Minimaps", targetFile));
                    }
                }
            }
        }
    }
}

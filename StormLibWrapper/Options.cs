using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.Extensions.Configuration;
using MPQToTACT.Helpers;

namespace MPQToTACT
{
    public class Options
    {
        [Option('d', "dir", Required = true, HelpText = "Input WoW Directory")]
        public string WoWDirectory { get; set; }

        [Option('o', "out", Required = true, HelpText = "Output directory for the TACT repo")]
        public string OutputFolder { get; set; }

        [Option('n', "name", Required = true, HelpText = "TACT Build Name e.g. WOW-18125patch6.0.1_Beta")]
        public string BuildName { get; set; }        

        [Option('t', "temp", HelpText = "Temporary directory used to extracted files")]
        public string TempDirectory { get; set; } = "temp";

        public HashSet<string> ExcludedDirectories { get; set; }

        public HashSet<string> ExcludedExtensions { get; set; }

        public void LoadConfig()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false)
                .Build();
        }
    }
}

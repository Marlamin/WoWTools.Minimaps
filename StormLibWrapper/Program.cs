using System;
using System.IO;
using System.Text.RegularExpressions;
using CommandLine;
using MPQToTACT.Helpers;
using MPQToTACT.ListFiles;
using MPQToTACT.Readers;

namespace MPQToTACT
{
    class Program
    {
        static void Main(string[] args)
        {
            using var parser = new Parser(s =>
            {
                s.HelpWriter = Console.Error;
                s.CaseInsensitiveEnumValues = true;
                s.AutoVersion = false;
            });

            var result = parser.ParseArguments<Options>(args);
            if (result.Tag == ParserResultType.Parsed)
                result.WithParsed(Run);
        }

        private static void Run(Options options)
        {       
            Clean(options);

            options.LoadConfig();

            // load the readers
            var dirReader = new DirectoryReader(options);
            var mpqReader = new MPQReader(options, dirReader.PatchArchives);

            // cleanup
            Clean(options);
        }

        #region Helpers

        /// <summary>
        /// Deleted all files from the temp and output directories
        /// </summary>
        /// <param name="output"></param>
        private static void Clean(Options options)
        {
            DeleteDirectory(options.TempDirectory);
            Directory.CreateDirectory(options.TempDirectory);
        }

        private static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
                return;

            Log.WriteLine($"Deleting {path}");

            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception ex)
            when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // handling of windows bug for deletion of large folders
                System.Threading.Thread.Sleep(50);
                Directory.Delete(path, true);
            }
        }

        #endregion
    }
}

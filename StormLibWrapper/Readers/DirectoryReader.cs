using MPQToTACT.Helpers;

namespace MPQToTACT.Readers
{
    public class DirectoryReader
    {
        private const StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

        public readonly string BaseDirectory;
        public readonly Options Options;

        public readonly List<string> DataArchives;
        public readonly List<string> BaseArchives;
        public readonly List<string> PatchArchives;

        public DirectoryReader(Options options)
        {
            DataArchives = new List<string>(0x100);
            BaseArchives = new List<string>(0x40);
            PatchArchives = new List<string>(0x40);

            Options = options;
            BaseDirectory = options.WoWDirectory;

            PopulateCollections();
        }

        #region Helpers

        /// <summary>
        /// Finds all MPQ files in BaseDirectory and allocates them to the appropiate collection
        /// </summary>
        private void PopulateCollections()
        {
            var files = Directory.EnumerateFiles(BaseDirectory, "*.mpq", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);

                // skip installation tomes and backups
                if (filename.Contains("tome", Comparison) ||
                    filename.Contains("backup", Comparison))
                    continue;

                // filter into the right collection
                if (filename.StartsWith("wow-update", Comparison) || filename.StartsWith("patch", Comparison))
                    PatchArchives.Add(file);
                else if (filename.StartsWith("base", Comparison))
                    BaseArchives.Add(file);
                else
                    DataArchives.Add(file);
            }

            PatchArchives.TrimExcess();
            BaseArchives.TrimExcess();
            DataArchives.TrimExcess();

            // sort them
            PatchArchives.Sort(MPQSorter.Sort);
            BaseArchives.Sort(MPQSorter.Sort);
            DataArchives.Sort(MPQSorter.Sort);
        }

        private bool HasDirectory(string path)
        {
            return Options.ExcludedDirectories.Overlaps(path.Split(Path.DirectorySeparatorChar));
        }

        private bool HasExtension(string path)
        {
            return Options.ExcludedExtensions.Contains(Path.GetExtension(path) ?? "");
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MPQToTACT.Helpers;
using TACT.Net.FileLookup;

namespace MPQToTACT.ListFiles
{
    public abstract class BaseListFileLookup : IFileLookup
    {
        public const string ListFilePath = "listfile.csv";

        public bool IsLoaded { get; private set; }
        protected Dictionary<string, uint> FileLookup { get; }

        public BaseListFileLookup()
        {
            FileLookup = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        }

        public virtual void Open()
        {
            uint id;
            string name;

            // load Id-Name map
            foreach (var file in File.ReadAllLines(ListFilePath))
            {
                var commaIndex = file.IndexOf(';');
                if (commaIndex == -1)
                    continue;

                id = uint.Parse(file.Substring(0, commaIndex));
                name = file[(commaIndex + 1)..];

                // unique-ify unnamed
                if (name == "")
                    FileLookup.Add("UNNAMED_" + id, id);
                else
                    FileLookup.Add(name.WoWNormalise(), id);
            }

            Log.WriteLine($"Loaded {FileLookup.Count} Ids");

            IsLoaded = true;
        }

        /// <summary>
        /// Exports an updated CSV
        /// </summary>
        public virtual void Close()
        {
            Log.WriteLine("Exporting Listfiles");

            using var csv = new StreamWriter(ListFilePath);
            foreach (var lookup in FileLookup.OrderBy(x => x.Value))
                csv.WriteLine(lookup.Value + ";" + lookup.Key);
        }

        public abstract uint GetOrCreateFileId(string filename);
    }
}

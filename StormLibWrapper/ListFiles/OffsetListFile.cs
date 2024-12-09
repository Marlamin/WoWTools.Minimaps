using System;
using System.Linq;
using MPQToTACT.Helpers;

namespace MPQToTACT.ListFiles
{
    /// <summary>
    /// This file lookup uses a base value as the minimum filedata id. If this is below the current `listfile.csv` max value, then that is used.
    /// </summary>
    /// <remarks>
    /// The idea is to offset the offical Blizzard Ids with a large number so we can differentiate offical from unoffical.<br/>
    /// FYI new filenames are allocated in alphabetical order by MPQReader to give some semblance of logic and conformity
    /// </remarks>
    class OffsetListFile : BaseListFileLookup
    {
        private uint CurrentId;

        public OffsetListFile(uint startId)
        {
            CurrentId = startId;
        }

        public override void Open()
        {
            base.Open();

            CurrentId = Math.Max(CurrentId, FileLookup.Values.Max());
            Log.WriteLine($"FileDataIds starting from {CurrentId}");
        }

        public override uint GetOrCreateFileId(string filename)
        {
            // check the filename exists
            if (!FileLookup.TryGetValue(filename, out var id))
            {
                id = ++CurrentId;
                FileLookup.Add(filename, id);
                return id;
            }

            return id;
        }
    }
}

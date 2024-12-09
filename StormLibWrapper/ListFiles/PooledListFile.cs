using System;
using System.Collections.Generic;
using System.Linq;
using MPQToTACT.Helpers;

namespace MPQToTACT.ListFiles
{
    /// <summary>
    /// This file lookup uses `listfile.csv` as a base and only uses unassigned Ids for new files.
    /// </summary>
    /// <remarks>
    /// The idea is to fill in the Blizzard gaps with genuine files from old game versions whilst avoiding future collisions<br/>
    /// The listfile used is a dump of the Classic WoW FileData DB2 file so *should* be adequate for this<br/>
    /// FYI new filenames are allocated in alphabetical order by MPQReader to give some semblance of logic and conformity
    /// </remarks>
    class PooledListFile : BaseListFileLookup
    {
        private Queue<uint> _unusedIds;

        public PooledListFile()
        {
            _unusedIds = new Queue<uint>();
        }

        public override void Open()
        {
            base.Open();
            LoadUnusedIDs();
        }

        public override uint GetOrCreateFileId(string filename)
        {
            // check the filename exists
            if (!FileLookup.TryGetValue(filename, out var id))
            {
                // attempt to take an id from the pool
                if (_unusedIds.Count > 0)
                {
                    id = _unusedIds.Dequeue();
                    FileLookup.Add(filename, id);
                    return id;
                }

                // TODO verify the best way of handling this
                throw new Exception("Out of unused Ids - SEND HELP!");
            }

            return id;
        }

        private void LoadUnusedIDs()
        {
            var idRange = Enumerable.Range(1, (int)FileLookup.Values.Max())
                                    .Select(x => (uint)x)
                                    .Except(FileLookup.Values);

            _unusedIds = new Queue<uint>(idRange);

            Log.WriteLine($"Found {_unusedIds.Count} unused Ids");
        }
    }
}

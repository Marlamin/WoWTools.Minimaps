using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPQToTACT.Helpers;

namespace MPQToTACT.ListFiles
{
    /// <summary>
    /// This file lookup uses negative Ids for unoffical filenames.
    /// </summary>
    /// <remarks>
    /// The idea is to offset the offical Blizzard Ids will be positive whilst unoffical will be negative.<br/>
    /// NOTE: since TACT.Net uses uints for Ids these will look funky.<br/>
    /// FYI new filenames are allocated in alphabetical order by MPQReader to give some semblance of logic and conformity
    /// </remarks>
    class NegatedListFile : BaseListFileLookup
    {
        private int CurrentId;

        public override void Open()
        {
            base.Open();

            var maxId = FileLookup.Values.Max();
            CurrentId = Math.Min(-1, unchecked((int)maxId));

            Log.WriteLine($"FileDataIds starting from {CurrentId}");
        }

        public override uint GetOrCreateFileId(string filename)
        {
            // check the filename exists
            if (!FileLookup.TryGetValue(filename, out var id))
            {
                id = unchecked((uint)--CurrentId);
                FileLookup.Add(filename, id);
                return id;
            }

            return id;
        }
    }
}

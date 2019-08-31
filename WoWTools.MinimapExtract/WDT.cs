using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// By justMaku, from https://github.com/justMaku/wow_minimap_compiler/blob/master/WDT.cs

namespace WoWTools.MinimapExtract
{
    class WDT
    {
        public struct WDTFileDataId
        {
            public UInt32 y;
            public UInt32 x;
            public UInt32 fileDataId;
        }

        static public WDTFileDataId[] FileDataIdsFromWDT(Stream stream)
        {
            var reader = new BinaryReader(stream);

            long position = 0;

            var minimapChunks = new List<WDTFileDataId>();

            while (position < stream.Length)
            {
                stream.Position = position;

                var chunkName = new string(reader.ReadChars(4).Reverse().ToArray());
                var chunkSize = reader.ReadUInt32();

                if (chunkName == "MAID")
                {
                    for (uint y = 0; y < 64; y++)
                    {
                        for (uint x = 0; x < 64; x++)
                        {
                            stream.Position += 7 * 4;
                            UInt32 minimapFileId = reader.ReadUInt32();
                            minimapChunks.Add(new WDTFileDataId { x = x, y = y, fileDataId = minimapFileId });
                        }
                    }

                    return minimapChunks.ToArray();
                }
                else
                {
                    position = stream.Position + chunkSize;
                }
            }

            return minimapChunks.ToArray();
        }
    }
}
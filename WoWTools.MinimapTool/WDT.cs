// By justMaku, from https://github.com/justMaku/wow_minimap_compiler/blob/master/WDT.cs

namespace WoWTools.MinimapTool
{
    class WDT
    {
        public struct WDTFileDataId
        {
            public sbyte y;
            public sbyte x;
            public uint fileDataId;
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
                if (chunkName == "\0\0\0\0")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("WDT file is encrypted!");
                    Console.ResetColor();
                    return minimapChunks.ToArray();
                }

                var chunkSize = reader.ReadUInt32();

                if (chunkName == "MAID")
                {
                    for (sbyte y = 0; y < 64; y++)
                    {
                        for (sbyte x = 0; x < 64; x++)
                        {
                            stream.Position += 7 * 4;
                            minimapChunks.Add(new WDTFileDataId { x = x, y = y, fileDataId = reader.ReadUInt32() });
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
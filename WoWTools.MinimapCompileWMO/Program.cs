using SereniaBLPLib;
using System;
using System.Drawing;
using System.IO;

namespace WoWTools.MinimapCompileWMO
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                throw new Exception("Not enough arguments, need indir, outpng, res (256 or 512 or 1024)");
            }

            var min_x = 64;
            var min_y = 64;

            var indir = args[0];
            var outpng = args[1];
            var blpRes = int.Parse(args[2]);

            if (blpRes != 1024 && blpRes != 512 && blpRes != 256)
            {
                Console.WriteLine("Unsupported BLP source resolution!");
            }

            for (var cur_x = 0; cur_x < 64; cur_x++)
            {
                for (var cur_y = 0; cur_y < 64; cur_y++)
                {
                    var tile = Path.Combine(indir, "map" + cur_x.ToString().PadLeft(2, '0') + "_" + cur_y.ToString().PadLeft(2, '0') + ".blp");
                    if (File.Exists(tile))
                    {
                        if (cur_x < min_x) { min_x = cur_x; }
                        if (cur_y < min_y) { min_y = cur_y; }
                    }
                }
            }

            var canvas = NetVips.Image.Black(1, 1);

            for (var cur_x = 0; cur_x < 64; cur_x++)
            {
                for (var cur_y = 0; cur_y < 64; cur_y++)
                {
                    using (var stream = new MemoryStream())
                    {
                        var tile = Path.Combine(indir, "map" + cur_x.ToString().PadLeft(2, '0') + "_" + cur_y.ToString().PadLeft(2, '0') + ".blp");
                        if (File.Exists(tile))
                        {
                            new BlpFile(File.OpenRead(tile)).GetBitmap(0).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            var image = NetVips.Image.NewFromBuffer(stream.ToArray());

                            if (image.Width != blpRes)
                            {
                                if (blpRes == 512 && image.Width == 256)
                                {
                                    Console.WriteLine("Upscaling tile " + cur_x + "x" + cur_y + " to 512..");
                                    image = image.Resize(2, NetVips.Enums.Kernel.Nearest);
                                }
                                else if (blpRes == 256 && image.Width == 512)
                                {
                                    image = image.Resize(0.5, NetVips.Enums.Kernel.Nearest);
                                }
                            }

                            canvas = canvas.Insert(image, (cur_x - min_x) * blpRes, (cur_y - min_y) * blpRes, true);
                        }
                    }
                }
            }
            canvas.WriteToFile(outpng);
        }
    }
}

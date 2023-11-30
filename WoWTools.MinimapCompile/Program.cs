using SereniaBLPLib;
using System;
using System.Drawing;
using System.IO;

namespace WoWTools.MinimapCompile
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                throw new Exception("Not enough arguments, need indir, outpng, (res (256 or 512 or 1024))");
            }

            var min_x = 64;
            var min_y = 64;

            var indir = args[0];
            var outpng = args[1];

            var blpRes = 512;
            if(args.Length == 3)
                blpRes = int.Parse(args[2]);

            if (blpRes != 1024 && blpRes != 512 && blpRes != 256)
            {
                Console.WriteLine("Unsupported BLP source resolution!");
            }

            var numMinimaps = 0;
            for (var cur_x = 0; cur_x < 64; cur_x++)
            {
                for (var cur_y = 0; cur_y < 64; cur_y++)
                {
                    var tile = Path.Combine(indir, "map" + cur_x.ToString().PadLeft(2, '0') + "_" + cur_y.ToString().PadLeft(2, '0') + ".blp");
                    if (File.Exists(tile))
                    {
                        numMinimaps++;
                        if (cur_x < min_x) { min_x = cur_x; }
                        if (cur_y < min_y) { min_y = cur_y; }
                    }
                }
            }

            if(numMinimaps == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No minimaps found in directory, make sure to specify a directory that has map_xx_yy.blp files.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine("Compiling map, this may take a while based on map size.");

            var canvas = NetVips.Image.Black(1, 1);

            var progress = 0;
            var prevProgress = 0;
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
                                    Console.WriteLine("Downscaling tile " + cur_x + "x" + cur_y + " to 256..");
                                    image = image.Resize(0.5, NetVips.Enums.Kernel.Nearest);
                                }
                            }

                            canvas = canvas.Insert(image, (cur_x - min_x) * blpRes, (cur_y - min_y) * blpRes, true);

                            progress++;
                        }
                    }

                    if(progress % 100 == 0)
                    {
                        if(progress != prevProgress)
                        {
                            Console.Write("\rReading tile: " + progress + "/" + numMinimaps);
                            prevProgress = progress;
                        }
                    }
                    else if(progress == numMinimaps && progress != prevProgress)
                    {
                        Console.WriteLine("\rReading tile: " + progress + "/" + numMinimaps);
                        prevProgress = progress;
                    }
                }
            }

            Console.Write("Writing tiles to output file (this may take a while)..");
            canvas.WriteToFile(outpng);
            Console.WriteLine("done, saved compiled image to " + outpng);
        }
    }
}

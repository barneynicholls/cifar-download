using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Drawing;
using System.IO;
using System.Net;

namespace Cifar10Download
{
    internal static class Program
    {
        //CIFAR-10 data
#pragma warning disable S1075 // URIs should not be hardcoded
        private static readonly string cifarUrl = "http://www.cs.toronto.edu/~kriz/cifar-10-binary.tar.gz";
#pragma warning restore S1075 // URIs should not be hardcoded

        //CIFAR-10 labels
        private static readonly string[] classNames = new string[] { "airplane", "automobile", "bird", "cat", "deer", "dog", "frog", "horse", "ship", "truck" };

        private static void Main()
        {
            const string destDSFolder = @"c:\cifar-10";

            DownloadExtractFile(destDSFolder);

            Console.WriteLine("============================================================================");
            Console.WriteLine($"CIFAR-10 is successfully extracted on your machine at: '{destDSFolder}'!");
            Console.WriteLine("Press any key to continue...!");
            Console.ReadKey();
        }

        private static void DownloadExtractFile(string destDSFolder)
        {
            var temp = Path.Combine(destDSFolder, "temp");
            var datafile = Path.Combine(temp, "cifar-10-binary.tar.gz");
            var training_data = Path.Combine(destDSFolder, "training_data");
            var test_data = Path.Combine(destDSFolder, "test_data");

            FileInfo file = new FileInfo(datafile);

            DownloadCifarRawData(file);

            UnzipCifarRawData(file);

            ExtractImages(temp, training_data, test_data);

            Directory.Delete(temp, true);
        }

        private static void ExtractImages(string temp, string training_data, string test_data)
        {
            int imageCounter = 1;

            for (int i = 1; i <= 5; i++)
            {
                Console.WriteLine($"Reading file 'data_batch_{i}.bin'...");
                var batch = File.ReadAllBytes(Path.Combine(temp, "cifar-10-batches-bin", $"data_batch_{i}.bin"));
                ExtractandSave(batch, training_data, ref imageCounter);
            }

            Console.WriteLine($"Reading file 'test_batch.bin'...");
            var testBatch = File.ReadAllBytes(Path.Combine(temp, "cifar-10-batches-bin", "test_batch.bin"));
            ExtractandSave(testBatch, test_data, ref imageCounter);
        }

        private static void UnzipCifarRawData(FileInfo file)
        {
            using (Stream inStream = File.OpenRead(file.FullName))
            {
                Stream gzipStream = new GZipInputStream(inStream);

                TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);

                Console.WriteLine($"Uncompressing file '{file.FullName}'...");
                tarArchive.ExtractContents(file.DirectoryName);
                tarArchive.Close();

                gzipStream.Close();
            }
        }

        private static void DownloadCifarRawData(FileInfo datafile)
        {
            if (datafile.Exists)
            {
                return;
            }

            datafile.Directory.Create();

            using (var client = new WebClient())
            {
                Console.WriteLine($"Downloading file '{cifarUrl}'...");

                client.DownloadFile(cifarUrl, datafile.FullName);
            }
        }

        private static void ExtractandSave(byte[] batch, string destImgFolder, ref int imgCounter)
        {
            Console.WriteLine($"Extracting images to '{destImgFolder}'...");

            const int nStep = 3073;

            for (int i = 0; i < batch.Length; i += nStep)
            {
                var l = (int)batch[i];
                var img = new ArraySegment<byte>(batch, i + 1, nStep - 1).ToArray();

                var reshaped = Reshape(3, 32, 32, img);
                var image = ArrayToImg(reshaped);

                var currentFolder = Path.Combine(destImgFolder, classNames[l]);
                Directory.CreateDirectory(currentFolder);

                image.Save(Path.Combine(currentFolder, $"{imgCounter++}.png"));
            }
        }

        private static Bitmap ArrayToImg(int[][][] imgData)
        {
            int width = imgData[0][0].Length;
            int height = imgData[0].Length;

            Bitmap bmp = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var ch1 = imgData[0][y][x];
                    var ch2 = imgData[1][y][x];
                    var ch3 = imgData[2][y][x];
                    var col = Color.FromArgb(ch1, ch2, ch3);
                    bmp.SetPixel(x, y, col);
                }
            }

            return bmp;
        }

        private static int[][][] Reshape(int channel, int height, int width, byte[] img)
        {
            var data = new int[channel][][];
            int counter = 0;
            for (int c = 0; c < channel; c++)
            {
                data[c] = new int[height][];
                for (int y = 0; y < height; y++)
                {
                    data[c][y] = new int[width];
                    for (int x = 0; x < width; x++)
                    {
                        data[c][y][x] = img[counter];
                        counter++;
                    }
                }
            }

            return data;
        }
    }
}

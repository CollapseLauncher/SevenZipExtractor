using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Linq;
using SevenZipExtractor;

namespace Testing
{
    public class Testing
    {
        static CancellationTokenSource tokenSource = new CancellationTokenSource();
        public static void Main(string[] args)
        {
            string ZipPath = @"C:\Users\neon-nyan\AppData\LocalLow\CollapseLauncher\GameFolder\Hi3SEA\Games\BH3_v5.6.0_4a51c6223034.7z";
            string DLLPath = @"C:\Program Files\7-Zip\7z.dll";
            ArchiveFile archive = new ArchiveFile(new FileStream(ZipPath, FileMode.Open, FileAccess.Read), SevenZipFormat.SevenZip, DLLPath);
            IList<Entry> FileEntries = archive.Entries;
            ulong TotalFileSize = (ulong)FileEntries.Sum(x => (decimal)x.Size);
            ulong TotalPackedFileSize = (ulong)FileEntries.Sum(x => (decimal)x.PackedSize);

            string _dirPath = @"C:\myGit\test";
            string _outPath;

            Task.Run(() => ReadKey());

            archive.ExtractProgress += Archive_ExtractProgress;
            using (archive)
            {
                try
                {
                    archive.Extract(entry =>
                    {
                        _outPath = Path.Combine(_dirPath, entry.FileName);
                        return _outPath; // where to put this particular file
                    }, tokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Cancelled!");
                }
            }
        }

        static void ReadKey()
        {
            while (true)
            {
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.P:
                        Console.WriteLine($"Cancelling...");
                        tokenSource.Cancel();
                        break;
                }
            }
        }

        internal static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string SummarizeSizeSimple(double value, int decimalPlaces = 2)
        {
            byte mag = (byte)Math.Log(value, 1000);

            return $"{Math.Round(value / (1L << (mag * 10)), decimalPlaces)} {SizeSuffixes[mag]}";
        }

        private static void Archive_ExtractProgress(object? sender, ArchiveFile.ExtractProgressProp e)
        {
            Console.Write($"\r{Math.Round(e.PercentProgress, 2)}\t(Current Read: {e.Read})\t{SummarizeSizeSimple(e.Speed)}/s");
        }
    }
}
using System;
// ReSharper disable UnusedMember.Global

namespace SevenZipExtractor.Event
{
    public struct ExtractProgressProp(
        ulong  read,
        ulong  totalRead,
        ulong  totalSize,
        double totalSecond,
        int    count,
        int    totalCount)
    {
        public int      Count           { get; set; }         = count;
        public int      TotalCount      { get; set; }         = totalCount;
        public ulong    Read            { get; private set; } = read;
        public ulong    TotalRead       { get; }              = totalRead;
        public ulong    TotalSize       { get; }              = totalSize;
        public double   Speed           { get; }              = (ulong)(totalRead / totalSecond);
        public double   PercentProgress => TotalRead / (double)TotalSize * 100;
        public TimeSpan TimeLeft        => TimeSpan.FromSeconds((TotalSize - TotalRead) / Speed);
    }
}
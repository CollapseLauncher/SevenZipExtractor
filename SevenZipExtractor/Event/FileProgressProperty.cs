namespace SevenZipExtractor.Event
{
    internal struct FileProgressProperty
    {
        public ulong StartRead;
        public ulong EndRead;
        public int   Count;
    }
}

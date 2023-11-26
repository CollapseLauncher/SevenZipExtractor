using System;

namespace SevenZipExtractor
{
    public sealed class SevenZipException : Exception
    {
        public SevenZipException(string message) : base(message)
        {
        }

        public SevenZipException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
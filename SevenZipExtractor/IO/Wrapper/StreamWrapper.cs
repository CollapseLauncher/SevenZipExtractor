using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace SevenZipExtractor.IO.Wrapper
{
    internal class StreamWrapper : IDisposable
    {
        protected Stream BaseStream;

        protected StreamWrapper(Stream baseStream)
        {
            BaseStream = baseStream;
        }

        ~StreamWrapper() => Dispose();

        public void Dispose()
        {
            BaseStream.Dispose();
            GC.SuppressFinalize(this);
        }

        public virtual unsafe void Seek(long offset, SeekOrigin seekOrigin, long* newPosition)
        {
            long pos = BaseStream.Seek(offset, seekOrigin);
            if (newPosition != null)
            {
                *newPosition = pos;
            }
        }
    }
}
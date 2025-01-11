using System;
using System.IO;
using System.Threading;

namespace SevenZipExtractor.IO.Wrapper
{
    internal unsafe class StreamWrapper : IDisposable
    {
        protected Stream            BaseStream;
        protected CancellationToken CancelToken;

        protected StreamWrapper(Stream baseStream, CancellationToken cancelToken)
        {
            BaseStream  = baseStream;
            CancelToken = cancelToken;
        }

        ~StreamWrapper() => Dispose();

        public virtual void Dispose()
        {
            BaseStream.Dispose();
            GC.SuppressFinalize(this);
        }

        public virtual void Seek(long offset, SeekOrigin seekOrigin, ulong* newPosition)
        {
            ulong pos = (ulong)BaseStream.Seek(offset, seekOrigin);
            if (newPosition != null)
            {
                *newPosition = pos;
            }
        }
    }
}
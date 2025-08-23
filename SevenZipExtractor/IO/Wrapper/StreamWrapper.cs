using System.IO;
using System.Threading;

namespace SevenZipExtractor.IO.Wrapper
{
    internal unsafe class StreamWrapper
    {
        protected Stream            BaseStream;
        protected CancellationToken CancelToken;

        protected StreamWrapper(Stream baseStream, CancellationToken cancelToken)
        {
            BaseStream    = baseStream;
            CancelToken   = cancelToken;
        }

        public virtual void Seek(long offset, SeekOrigin seekOrigin, long* newPosition)
        {
            long pos = BaseStream.Seek(offset, seekOrigin);
            if (newPosition != null)
            {
                *newPosition = pos;
            }
        }
    }
}
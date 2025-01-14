using SevenZipExtractor.Interface;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.IO.Wrapper
{
    [GeneratedComClass]
    internal sealed unsafe partial class OutStreamWrapper : StreamWrapper, IOutStream
    {
        internal OutStreamWrapper(Stream baseStream, CancellationToken cancelToken) : base(baseStream, cancelToken)
        {
        }

        public int SetSize(long newSize)
        {
            lock (BaseStream)
            {
                BaseStream.SetLength(newSize);
            }

            return 0;
        }

        public int Write(byte[] data, uint size, uint* processedSize)
        {
            CancelToken.ThrowIfCancellationRequested();

            int sizeAsInt = (int)size;

            lock (BaseStream)
            {
                BaseStream.Write(data, 0, sizeAsInt);
            #if DEBUG
                Debug.Assert(data.Length == sizeAsInt);
            #endif
            }

            if (processedSize != null)
            {
                *processedSize = size;
            }
            return 0;
        }
    }
}

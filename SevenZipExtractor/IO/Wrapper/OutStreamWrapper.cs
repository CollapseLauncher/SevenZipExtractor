using SevenZipExtractor.Interface;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.IO.Wrapper
{
    [GeneratedComClass]
    internal partial class OutStreamWrapper(Stream baseStream, CancellationToken cancellationToken)
        : StreamWrapper(baseStream), IOutStream
    {
        public int SetSize(long newSize)
        {
            lock (BaseStream)
            {
                BaseStream.SetLength(newSize);
            }

            return 0;
        }

        public unsafe int Write(byte[] data, uint size, uint* processedSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int sizeAsInt = (int)size;

            lock (BaseStream)
            {
                BaseStream.Write(data, 0, sizeAsInt);
            #if DEBUG
                Debug.Assert(data.Length == sizeAsInt);
            #endif
            }

            *processedSize = size;
            return 0;
        }
    }
}

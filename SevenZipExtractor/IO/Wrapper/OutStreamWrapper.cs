using SevenZipExtractor.Interface;
using System;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.IO.Wrapper
{
    [GeneratedComClass]
    internal sealed partial class OutStreamWrapper : StreamWrapper, IOutStream
    {
        internal OutStreamWrapper(Stream baseStream, CancellationToken cancelToken) : base(baseStream, cancelToken)
        {
        }

        public void SetSize(long newSize)
        {
            BaseStream.SetLength(newSize);
        }

        public unsafe void Write(void* data, int size, int* processedSize)
        {
            CancelToken.ThrowIfCancellationRequested();
            BaseStream.Write(new ReadOnlySpan<byte>(data, size));

            if (processedSize != null)
            {
                *processedSize = size;
            }
        }
    }
}

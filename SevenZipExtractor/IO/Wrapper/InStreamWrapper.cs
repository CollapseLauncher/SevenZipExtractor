using SevenZipExtractor.Interface;
using System;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.IO.Wrapper
{
    [GeneratedComClass]
    internal sealed partial class InStreamWrapper : StreamWrapper, IInStream
    {
        internal InStreamWrapper(Stream baseStream, CancellationToken cancelToken) : base(baseStream, cancelToken)
        {
        }

        public unsafe void Read(void* data, int size, int* processedSize)
        {
            CancelToken.ThrowIfCancellationRequested();
            int read = BaseStream.Read(new Span<byte>(data, size));

            if (processedSize != null)
            {
                *processedSize = read;
            }
        }
    }
}

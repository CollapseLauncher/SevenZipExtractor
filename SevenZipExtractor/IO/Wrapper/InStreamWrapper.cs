using SevenZipExtractor.Interface;
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

        public uint Read(byte[] data, uint size)
        {
            CancelToken.ThrowIfCancellationRequested();
            return (uint)BaseStream.Read(data, 0, (int)size);
        }
    }
}

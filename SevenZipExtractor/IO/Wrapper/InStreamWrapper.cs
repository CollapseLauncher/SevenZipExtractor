using SevenZipExtractor.Interface;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.IO.Wrapper
{
    [GeneratedComClass]
    internal partial class InStreamWrapper(Stream baseStream) : StreamWrapper(baseStream), IInStream
    {
        public uint Read(byte[] data, uint size) => (uint)BaseStream.Read(data, 0, (int)size);
    }
}

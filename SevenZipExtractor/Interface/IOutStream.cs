using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.Interface
{
    [Guid(Constants.IID_IOutStream)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal unsafe partial interface IOutStream : ISequentialOutStream
    {
        void Seek(
            long       offset,
            SeekOrigin seekOrigin,
            long*      newPosition);

        void SetSize(long newSize);
    }
}

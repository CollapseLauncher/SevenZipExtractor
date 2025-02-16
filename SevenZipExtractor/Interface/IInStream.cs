using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.Interface
{
    [Guid(Constants.IID_IInStream)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal unsafe partial interface IInStream : ISequentialInStream
    {
        void Seek(
            long       offset,
            SeekOrigin seekOrigin,
            ulong*     newPosition);
    }
}

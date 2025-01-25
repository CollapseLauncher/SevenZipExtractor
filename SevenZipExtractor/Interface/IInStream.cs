using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.Interface
{
    [Guid("23170F69-40C1-278A-0000-000300030000")]
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

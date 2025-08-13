using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.Interface
{
    [Guid(Constants.IID_ISequentialOutStream)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface ISequentialOutStream
    {
        // 2025-08-12: Same reasoning as in ISequentialInStream.
        unsafe void Write(
            void* data,
            int   size,
            int*  processedSize);
    }
}

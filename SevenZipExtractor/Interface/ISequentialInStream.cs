using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.Interface
{
    [Guid(Constants.IID_ISequentialInStream)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface ISequentialInStream
    {
        // 2025-08-12: Actually, the int must be an uint.
        //             But due to hot-path casting and small buffer sizes, we use int here
        //             and basically the value will never be overflowed.
        unsafe void Read(
            void* data,
            int   size,
            int*  processedSize);
    }
}

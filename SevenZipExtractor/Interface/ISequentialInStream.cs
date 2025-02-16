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
        uint Read(
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint                                                               size);
    }
}

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.Interface
{
    [Guid(Constants.IID_ISequentialOutStream)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal unsafe partial interface ISequentialOutStream
    {
        [PreserveSig]
        int Write(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint                                                              size,
            uint*                                                             processedSize); // ref uint processedSize
    }
}

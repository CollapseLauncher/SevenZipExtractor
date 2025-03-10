using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.Interface
{
    [Guid(Constants.IID_IArchiveOpenCallback)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal unsafe partial interface IArchiveOpenCallback
    {
        // ref ulong replaced with IntPtr because handlers ofter pass null value
        // read actual value with Marshal.ReadInt64
        void SetTotal(
            ulong* files, // [In] ref ulong files, can use 'ulong* files' but it is unsafe
            ulong* bytes); // [In] ref ulong bytes

        void SetCompleted(
            ulong* files, // [In] ref ulong files
            ulong* bytes); // [In] ref ulong bytes
    }
}

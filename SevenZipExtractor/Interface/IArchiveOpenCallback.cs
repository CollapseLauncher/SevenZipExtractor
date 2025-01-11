using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.Interface
{
    [Guid("23170F69-40C1-278A-0000-000600100000")]
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

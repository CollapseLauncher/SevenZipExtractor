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
        void SetTotal(
            ulong* files,
            ulong* bytes);

        void SetCompleted(
            ulong* files,
            ulong* bytes);
    }
}

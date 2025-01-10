using SevenZipExtractor.Enum;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable CommentTypo

namespace SevenZipExtractor.Interface
{
    [Guid("23170F69-40C1-278A-0000-000600300000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface IArchiveOpenVolumeCallback
    {
        void GetProperty(
            ItemPropId propID, // PROPID
            IntPtr     value); // PROPVARIANT

        [PreserveSig]
        int GetStream(
            [MarshalAs(UnmanagedType.LPWStr)]        string    name,
            [MarshalAs(UnmanagedType.Interface)] out IInStream inStream);
    }
}

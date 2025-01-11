using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.Interface
{
    [Guid("23170F69-40C1-278A-0000-000500100000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface ICryptoGetTextPassword
    {
        [PreserveSig]
        int CryptoGetTextPassword([MarshalAs(UnmanagedType.BStr)] out string? password);
    }
}

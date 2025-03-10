using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.Interface
{
    [Guid(Constants.IID_ICryptoGetTextPassword)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface ICryptoGetTextPassword
    {
        [PreserveSig]
        int CryptoGetTextPassword([MarshalAs(UnmanagedType.BStr)] out string? password);
    }
}

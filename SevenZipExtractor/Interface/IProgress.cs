using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SevenZipExtractor.Interface
{
    [Guid(Constants.IID_IProgress)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface IProgress
    {
        void SetTotal(ulong total);

        unsafe void SetCompleted(ulong* completeValue);
    }
}

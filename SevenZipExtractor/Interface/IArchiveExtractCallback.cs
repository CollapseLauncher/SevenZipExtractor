using SevenZipExtractor.Enum;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedMember.Global

namespace SevenZipExtractor.Interface
{
    [Guid("23170F69-40C1-278A-0000-000600200000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface IArchiveExtractCallback //: IProgress
    {
        void SetTotal(ulong total);
        void SetCompleted(in ulong completeValue);

        [PreserveSig]
        int GetStream(
            uint index,
            [MarshalAs(UnmanagedType.Interface)] out ISequentialOutStream? outStream,
            AskMode askExtractMode);
        // GetStream OUT: S_OK - OK, S_FALSE - keep this file

        void PrepareOperation(AskMode askExtractMode);
        void SetOperationResult(OperationResult resultEOperationResult);
    }
}
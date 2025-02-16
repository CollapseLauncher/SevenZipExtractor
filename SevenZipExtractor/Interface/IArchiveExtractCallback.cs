using SevenZipExtractor.Enum;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedMember.Global

namespace SevenZipExtractor.Interface
{
    [Guid(Constants.IID_IArchiveExtractCallback)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal unsafe partial interface IArchiveExtractCallback //: IProgress
    {
        void SetTotal(ulong total);

        void SetCompleted(ulong* completeValue);

        [PreserveSig]
        int GetStream(
            uint                                                           index,
            [MarshalAs(UnmanagedType.Interface)] out ISequentialOutStream? outStream,
            AskMode                                                        askExtractMode);
        // GetStream OUT: S_OK - OK, S_FALSE - keep this file

        void PrepareOperation(AskMode askExtractMode);

        void SetOperationResult(OperationResult resultEOperationResult);
    }
}
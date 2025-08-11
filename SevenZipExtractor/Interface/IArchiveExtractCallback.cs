using SevenZipExtractor.Enum;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantUnsafeContext

namespace SevenZipExtractor.Interface
{
    [Guid(Constants.IID_IArchiveExtractCallback)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal unsafe partial interface IArchiveExtractCallback : IProgress
    {
        void GetStream(
            uint                                                           index,
            [MarshalAs(UnmanagedType.Interface)] out ISequentialOutStream? outStream,
            AskMode                                                        askExtractMode);

        void PrepareOperation(AskMode askExtractMode);

        void SetOperationResult(OperationResult resultEOperationResult);
    }
}
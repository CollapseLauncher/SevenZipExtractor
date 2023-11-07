using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SevenZipExtractor
{
    [Guid("23170F69-40C1-278A-0000-000600200000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface IArchiveExtractCallback //: IProgress
    {
        void SetTotal(ulong total);
        void SetCompleted(ref ulong completeValue);

        [PreserveSig]
        int GetStream(
            uint index,
            out ISequentialOutStream outStream,
            AskMode askExtractMode);
        // GetStream OUT: S_OK - OK, S_FALSE - skeep this file

        void PrepareOperation(AskMode askExtractMode);
        void SetOperationResult(OperationResult resultEOperationResult);
    }
}
using SevenZipExtractor.Enum;
using SevenZipExtractor.Interface;
using SevenZipExtractor.IO.Wrapper;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.IO.Callback
{
    [GeneratedComClass]
    internal sealed partial class ArchiveStreamCallback(
        uint              fileNumber,
        Stream            stream,
        CancellationToken cancellationToken)
        : IArchiveExtractCallback
    {
        public void SetTotal(ulong total)
        {
        }

        public void SetCompleted(in ulong completeValue)
        {
        }

        public int GetStream(uint index, out ISequentialOutStream? outStream, AskMode askExtractMode)
        {
            if (index != fileNumber || askExtractMode != AskMode.kExtract)
            {
                outStream = null;
                return 0;
            }

            outStream = new OutStreamWrapper(stream, cancellationToken);
            return 0;
        }

        public void PrepareOperation(AskMode askExtractMode)
        {
        }

        public void SetOperationResult(OperationResult resultEOperationResult)
        {
        }
    }
}
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace SevenZipExtractor
{
    [GeneratedComClass]
    internal sealed partial class ArchiveStreamCallback : IArchiveExtractCallback
    {
        private readonly CancellationToken cancellationToken;
        private readonly uint fileNumber;
        private readonly Stream stream;

        public ArchiveStreamCallback(uint fileNumber, Stream stream, CancellationToken cancellationToken)
        {
            this.fileNumber = fileNumber;
            this.stream = stream;
            this.cancellationToken = cancellationToken;
        }

        public void SetTotal(ulong total)
        {
        }

        public void SetCompleted(in ulong completeValue)
        {
        }

        public int GetStream(uint index, out ISequentialOutStream outStream, AskMode askExtractMode)
        {
            if ((index != this.fileNumber) || (askExtractMode != AskMode.kExtract))
            {
                outStream = null;
                return 0;
            }

            outStream = new OutStreamWrapper(this.stream, this.cancellationToken);
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
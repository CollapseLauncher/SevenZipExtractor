using SevenZipExtractor.Enum;
using SevenZipExtractor.Interface;
using SevenZipExtractor.IO.Wrapper;
using System;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.IO.Callback
{
    [GeneratedComClass]
    internal partial class ArchiveStreamCallback(
        uint              fileNumber,
        Stream            stream,
        bool              disposeStream,
        CancellationToken cancellationToken
        ) : StreamCallbackBase, IDisposable
    {
        public override int GetStream(uint index, out ISequentialOutStream? outStream, AskMode askExtractMode)
        {
            if (index != fileNumber || askExtractMode != AskMode.kExtract)
            {
                outStream = null;
                return 0;
            }

            outStream = new OutStreamWrapper(stream, cancellationToken);
            return 0;
        }

        public void Dispose()
        {
            if (disposeStream)
            {
                stream.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
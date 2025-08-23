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
        public override void GetStream(uint index, out ISequentialOutStream? outStream, AskMode askExtractMode)
        {
            if (index != fileNumber || askExtractMode != AskMode.Extract)
            {
                outStream = null;
                return;
            }

            outStream = new OutStreamWrapper(stream, cancellationToken);
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
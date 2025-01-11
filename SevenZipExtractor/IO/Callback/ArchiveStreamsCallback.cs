using SevenZipExtractor.Enum;
using SevenZipExtractor.Interface;
using SevenZipExtractor.IO.Wrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.IO.Callback
{
    [GeneratedComClass]
    internal sealed partial class ArchiveStreamsCallback(
        List<Func<Stream>?> streams,
        DateTime[]          streamTimestamps,
        bool                preserveTimestamp,
        CancellationToken   cancellationToken
        ) : StreamCallbackBase
    {
        public static ArchiveStreamsCallback Create(Func<Entry, string?> getOutputPath, List<Entry> entries, bool overwrite, bool preserveTimestamp, int outputBufferSize, CancellationToken token)
        {
            FileStreamOptions fileStreamOptions = new()
            {
                Mode       = FileMode.Create,
                Access     = FileAccess.Write,
                Share      = FileShare.Write,
                BufferSize = outputBufferSize
            };

            List<Func<Stream>?> outStreamDelegates  = [];
            DateTime[]          outStreamTimestamps = GC.AllocateUninitializedArray<DateTime>(entries.Count);

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];

                string? outputPath = getOutputPath(entry);

                if (string.IsNullOrEmpty(outputPath) ||
                    string.IsNullOrWhiteSpace(outputPath))
                {
                    outStreamDelegates.Add(null);
                    continue;
                }

                if (entry.IsFolder)
                {
                    Directory.CreateDirectory(outputPath);
                    outStreamDelegates.Add(null);
                    continue;
                }

                // Always unassign read-only attribute from file
                FileInfo fileInfo = new FileInfo(outputPath);
                switch (fileInfo.Exists)
                {
                    case true when !overwrite:
                        outStreamDelegates.Add(null);
                        continue;
                    case true:
                        fileInfo.IsReadOnly = false;
                        break;
                }

                if (!(fileInfo.Directory?.Exists ?? true))
                {
                    fileInfo.Directory?.Create();
                }

                outStreamDelegates.Add(() => fileInfo.Open(fileStreamOptions));
                outStreamTimestamps[i] = entry.LastWriteTime;
            }

            return new ArchiveStreamsCallback(outStreamDelegates, outStreamTimestamps, preserveTimestamp, token);
        }

        public override int GetStream(uint index, out ISequentialOutStream? outStream, AskMode askExtractMode)
        {
            if (askExtractMode != AskMode.kExtract)
            {
                outStream = null;
                return 0;
            }

            if (streams == null)
            {
                outStream = null;
                return 0;
            }

            Func<Stream>? streamFunc    = streams[(int)index];
            Stream?       currentStream = streamFunc?.Invoke();

            if (streamFunc == null || currentStream == null)
            {
                outStream = null;
                return 0;
            }

            DateTime dateTime = streamTimestamps[(int)index];

            ProgressProperty.Count = 0;
            StatusProperty.Name    = currentStream is FileStream asFs ? asFs.Name : "";
            UpdateStatus(StatusProperty);

            outStream = new OutStreamWrapper(currentStream, dateTime, preserveTimestamp, cancellationToken);
            return 0;
        }
    }
}
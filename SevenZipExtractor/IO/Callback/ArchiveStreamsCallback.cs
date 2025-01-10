using SevenZipExtractor.Enum;
using SevenZipExtractor.Event;
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
        List<Func<Stream>> streams,
        CancellationToken  cancellationToken)
        : IArchiveExtractCallback
    {
        private FileProgressProperty _progressProperty = new()
        {
            Count     = 0,
            StartRead = 0,
            EndRead   = 0
        };

        private FileStatusProperty _statusProperty = new()
        {
            Name = string.Empty
        };

        private Stream? _currentStream;

        public event EventHandler<FileProgressProperty>? ReadProgress;
        public event EventHandler<FileStatusProperty>?   ReadStatus;

        private void UpdateProgress(FileProgressProperty e)
        {
            ReadProgress?.Invoke(this, e);
        }

        private void UpdateStatus(FileStatusProperty e)
        {
            ReadStatus?.Invoke(this, e);
        }

        public static ArchiveStreamsCallback Create(Func<Entry, string?> getOutputPath, List<Entry> entries, bool overwrite, int outputBufferSize, CancellationToken token)
        {
            FileStreamOptions fileStreamOptions = new()
            {
                Mode       = overwrite ? FileMode.Create : FileMode.Open,
                Access     = FileAccess.Write,
                Share      = FileShare.Write,
                BufferSize = outputBufferSize
            };

            List<Func<Stream>> outStreamDelegates = [];
            foreach (Entry entry in entries)
            {
                string? outputPath = getOutputPath(entry);

                if (string.IsNullOrEmpty(outputPath) ||
                    string.IsNullOrWhiteSpace(outputPath))
                {
                    continue;
                }

                if (entry.IsFolder)
                {
                    Directory.CreateDirectory(outputPath);
                    continue;
                }

                // Always unassign read-only attribute from file
                FileInfo fileInfo = new FileInfo(outputPath);
                if (fileInfo.Exists)
                {
                    fileInfo.IsReadOnly = false;
                }

                if (!(fileInfo.Directory?.Exists ?? true))
                {
                    fileInfo.Directory?.Create();
                }

                outStreamDelegates.Add(() => fileInfo.Open(fileStreamOptions));
            }

            return new ArchiveStreamsCallback(outStreamDelegates, token);
        }

        public void SetTotal(ulong total)
        {
            _progressProperty.EndRead = total;
            UpdateProgress(_progressProperty);
        }

        public void SetCompleted(in ulong completeValue)
        {
            _progressProperty.StartRead = completeValue;
            UpdateProgress(_progressProperty);
        }

        public int GetStream(uint index, out ISequentialOutStream? outStream, AskMode askExtractMode)
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

            Func<Stream> streamFunc = streams[(int)index];

            if (streamFunc == null)
            {
                outStream = null;
                return 0;
            }

            _currentStream          = streamFunc();
            _progressProperty.Count = 0;
            _statusProperty.Name    = _currentStream is FileStream asFs ? asFs.Name : "";
            UpdateStatus(_statusProperty);

            outStream = new OutStreamWrapper(_currentStream, cancellationToken);
            return 0;
        }

        public void PrepareOperation(AskMode askExtractMode)
        {
        }

        public void SetOperationResult(OperationResult resultEOperationResult)
        {
            _currentStream?.Dispose();
        }
    }
}
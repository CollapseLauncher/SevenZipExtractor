using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace SevenZipExtractor
{
    internal struct FileProgressProperty
    {
        public ulong StartRead;
        public ulong EndRead;
        public int Count;
    }

    internal struct FileStatusProperty
    {
        public string Name;
    }

    [GeneratedComClass]
    internal sealed partial class ArchiveStreamsCallback : IArchiveExtractCallback
    {
        private readonly List<Func<FileStream>> streams;
        private readonly CancellationToken cancellationToken;

        private FileProgressProperty ProgressProperty;
        private FileStatusProperty StatusProperty;
        private FileStream currentStream;

        public event EventHandler<FileProgressProperty> ReadProgress;
        public event EventHandler<FileStatusProperty> ReadStatus;

        private void UpdateProgress(FileProgressProperty e) => ReadProgress?.Invoke(this, e);
        private void UpdateStatus(FileStatusProperty e) => ReadStatus?.Invoke(this, e);

        public ArchiveStreamsCallback(List<Func<FileStream>> streams, CancellationToken cancellationToken)
        {
            this.streams = streams;
            this.cancellationToken = cancellationToken;
            this.ProgressProperty = new FileProgressProperty
            {
                Count = 0,
                StartRead = 0,
                EndRead = 0
            };
            this.StatusProperty = new FileStatusProperty
            {
                Name = string.Empty
            };
        }

        public void SetTotal(ulong total)
        {
            this.ProgressProperty.EndRead = total;
            UpdateProgress(this.ProgressProperty);
        }

        public void SetCompleted(in ulong completeValue)
        {
            this.ProgressProperty.StartRead = completeValue;
            UpdateProgress(this.ProgressProperty);
        }

        public int GetStream(uint index, out ISequentialOutStream outStream, AskMode askExtractMode)
        {
            if (askExtractMode != AskMode.kExtract)
            {
                outStream = null;
                return 0;
            }

            if (this.streams == null)
            {
                outStream = null;
                return 0;
            }

            Func<FileStream> streamFunc = this.streams[(int)index];

            if (streamFunc == null)
            {
                outStream = null;
                return 0;
            }

            currentStream = streamFunc();
            this.ProgressProperty.Count = 0;
            this.StatusProperty.Name = currentStream.Name;
            UpdateStatus(this.StatusProperty);

            outStream = new OutStreamWrapper(currentStream, this.cancellationToken);
            return 0;
        }

        public void PrepareOperation(AskMode askExtractMode)
        {
        }

        public void SetOperationResult(OperationResult resultEOperationResult)
        {
            currentStream?.Dispose();
        }
    }
}
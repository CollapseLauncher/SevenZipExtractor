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
        private readonly List<FileStream> streams;
        private readonly CancellationToken cancellationToken;

        public event EventHandler<FileProgressProperty> ReadProgress;
        public event EventHandler<FileStatusProperty> ReadStatus;
        private void UpdateProgress(FileProgressProperty e) => ReadProgress?.Invoke(this, e);
        private void UpdateStatus(FileStatusProperty e) => ReadStatus?.Invoke(this, e);

        private ulong TotalSize = 0;
        private ulong TotalRead = 0;
        private string CurrentName = "";
        private int Count = 0;

        public ArchiveStreamsCallback(List<FileStream> streams, CancellationToken cancellationToken)
        {
            this.streams = streams;
            this.cancellationToken = cancellationToken;
        }

        public void SetTotal(ulong total)
        {
            TotalSize = total;
            UpdateProgress(new FileProgressProperty { StartRead = TotalRead, EndRead = TotalSize, Count = Count });
        }

        public void SetCompleted(in ulong completeValue)
        {
            TotalRead = completeValue;
            UpdateProgress(new FileProgressProperty { StartRead = TotalRead, EndRead = TotalSize, Count = Count });
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

            FileStream stream = this.streams[(int)index];

            if (stream == null)
            {
                outStream = null;
                return 0;
            }
            else
            {
                CurrentName = stream.Name;
                Count++;
                UpdateStatus(new FileStatusProperty { Name = CurrentName });
            }

            outStream = new OutStreamWrapper(stream, this.cancellationToken);
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
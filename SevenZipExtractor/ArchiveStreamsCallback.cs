using System.Collections.Generic;
using System.IO;
using System;

namespace SevenZipExtractor
{
    public class FileProgressProperty
    {
        public ulong StartRead { get; set; }
        public ulong EndRead { get; set; }
    }
    public class FileStatusProperty
    {
        public string Name { get; set; }
    }

    internal class ArchiveStreamsCallback : IArchiveExtractCallback
    {
        private readonly IList<CancellableFileStream> streams;

        public event EventHandler<FileProgressProperty> ReadProgress;
        public event EventHandler<FileStatusProperty> ReadStatus;
        private void UpdateProgress(FileProgressProperty e) => ReadProgress?.Invoke(this, e);
        private void UpdateStatus(FileStatusProperty e) => ReadStatus?.Invoke(this, e);

        private ulong TotalSize = 0;
        private ulong TotalRead = 0;
        private string CurrentName = "";

        public ArchiveStreamsCallback(IList<CancellableFileStream> streams) 
        {
            this.streams = streams;
        }

        public void SetTotal(ulong total)
        {
            TotalSize = total;
            UpdateProgress(new FileProgressProperty { StartRead = TotalRead, EndRead = TotalSize } );
        }

        public void SetCompleted(ref ulong completeValue)
        {
            TotalRead = completeValue;
            UpdateProgress(new FileProgressProperty { StartRead = TotalRead, EndRead = TotalSize });
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

            CancellableFileStream stream = this.streams[(int) index];

            if (stream == null)
            {
                outStream = null;
                return 0;
            }
            else
            {
                CurrentName = stream.Name;
                UpdateStatus(new FileStatusProperty { Name = CurrentName });
            }

            outStream = new OutStreamWrapper(stream);

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
using SevenZipExtractor.Enum;
using SevenZipExtractor.Event;
using SevenZipExtractor.Interface;
using System;

namespace SevenZipExtractor.IO.Callback
{
    internal abstract unsafe class StreamCallbackBase : IArchiveExtractCallback, ICryptoGetTextPassword
    {
        [CLSCompliant(false)]
        protected string? ArchivePassword;
        protected FileProgressProperty ProgressProperty = new()
        {
            Count     = 0,
            StartRead = 0,
            EndRead   = 0
        };

        protected FileStatusProperty StatusProperty = new()
        {
            Name = string.Empty
        };

        public event EventHandler<FileProgressProperty>? ReadProgress;
        public event EventHandler<FileStatusProperty>?   ReadStatus;

        protected void UpdateProgress(FileProgressProperty e)
            => ReadProgress?.Invoke(this, e);

        protected void UpdateStatus(FileStatusProperty e)
            => ReadStatus?.Invoke(this, e);

        public virtual void SetTotal(ulong total)
        {
            ProgressProperty.EndRead = total;
            UpdateProgress(ProgressProperty);
        }

        public virtual void SetCompleted(ulong* completeValue)
        {
            ProgressProperty.StartRead = *completeValue;
            UpdateProgress(ProgressProperty);
        }

        public abstract int GetStream(uint index, out ISequentialOutStream? outStream, AskMode askExtractMode);

        public virtual void PrepareOperation(AskMode askExtractMode)
        {
        }

        public virtual void SetOperationResult(OperationResult resultEOperationResult)
        {
        }

        [CLSCompliant(false)]
        internal void SetArchivePassword(string? password)
            => ArchivePassword = password;

        public int CryptoGetTextPassword(out string? password)
        {
            password = ArchivePassword;
            return 0;
        }
    }
}

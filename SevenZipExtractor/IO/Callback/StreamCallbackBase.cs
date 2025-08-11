using SevenZipExtractor.Enum;
using SevenZipExtractor.Event;
using SevenZipExtractor.Interface;
using System;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart

namespace SevenZipExtractor.IO.Callback
{
    [GeneratedComClass]
    internal abstract partial class StreamCallbackBase : IArchiveExtractCallback, ICryptoGetTextPassword
    {
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

        public virtual unsafe void SetCompleted(ulong* completeValue)
        {
            ProgressProperty.StartRead = *completeValue;
            UpdateProgress(ProgressProperty);
        }

        public abstract void GetStream(uint index, out ISequentialOutStream? outStream, AskMode askExtractMode);

        public virtual void PrepareOperation(AskMode askExtractMode)
        {
        }

        public virtual void SetOperationResult(OperationResult resultEOperationResult)
        {
        }

        internal void SetArchivePassword(string? password)
            => ArchivePassword = password;

        public int CryptoGetTextPassword(out string? password)
        {
            password = ArchivePassword;
            return 0;
        }
    }
}

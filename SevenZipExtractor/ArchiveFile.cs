using SevenZipExtractor.Enum;
using SevenZipExtractor.Event;
using SevenZipExtractor.Format;
using SevenZipExtractor.Interface;
using SevenZipExtractor.IO.Callback;
using SevenZipExtractor.IO.Wrapper;
using SevenZipExtractor.Unmanaged;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery

namespace SevenZipExtractor
{
    public sealed class ArchiveFile : IDisposable
    {
        private const    int             DefaultOutBufferSize = 4 << 10;
        private readonly IInArchive?     _archive;
        private readonly InStreamWrapper _archiveStream;
        private          ulong           _lastSize;
        private          Stopwatch       _extractProgressStopwatch = Stopwatch.StartNew();

        public event EventHandler<ExtractProgressProp>? ExtractProgress;

        public List<Entry> Entries { get; }
        public int Count { get; }

        public ArchiveFile(string archiveFilePath) :
            this(File.Open(archiveFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
        }

        public ArchiveFile(Stream archiveStream, SevenZipFormat format = SevenZipFormat.Undefined)
        {
            ArgumentNullException.ThrowIfNull(archiveStream, nameof(archiveStream));

            if (format == SevenZipFormat.Undefined)
            {
                if (!archiveStream.CanSeek)
                {
                    throw new InvalidOperationException("Cannot guess the format due to archiveStream is not seekable");
                }

                if (!GuessFormatFromSignature(archiveStream, out format))
                {
                    throw new FormatException("Unable to guess the format automatically");
                }
            }

            _archive       = NativeMethods.CreateInArchive(FormatIdentity.GuidMapping[format]);
            _archiveStream = new InStreamWrapper(archiveStream, default);
            Entries        = GetEntriesInner(_archive, _archiveStream);
            Count          = Entries.Select(x => x.IsFolder ? 0 : 1).Sum();
        }

        ~ArchiveFile()
        {
            Dispose();
        }

        public static ArchiveFile Create(string archiveFilePath)
            => new(archiveFilePath);

        public static ArchiveFile Create(Stream archiveStream, SevenZipFormat format = SevenZipFormat.Undefined)
            => new(archiveStream, format);

        public void Extract(string outputFolder, bool overwrite = true, CancellationToken token = default)
            => Extract(entry => GetEntryPathInner(entry, outputFolder), overwrite, true, DefaultOutBufferSize, token);

        public void Extract(string outputFolder, bool overwrite, int outputBufferSize, CancellationToken token = default)
            => Extract(entry => GetEntryPathInner(entry, outputFolder), overwrite, true, outputBufferSize, token);

        public void Extract(string outputFolder, bool overwrite, bool preserveTimestamp, int outputBufferSize, CancellationToken token = default)
            => Extract(entry => GetEntryPathInner(entry, outputFolder), overwrite, preserveTimestamp, outputBufferSize, token);

        public void Extract(Func<Entry, string?> getOutputPath, CancellationToken token = default)
            => Extract(getOutputPath, true, true, DefaultOutBufferSize, token);

        public void Extract(Func<Entry, string?> getOutputPath, bool overwrite, CancellationToken token = default)
            => Extract(getOutputPath, overwrite, true, DefaultOutBufferSize, token);

        public void Extract(Func<Entry, string?> getOutputPath, bool overwrite, bool preserveTimestamp = true, CancellationToken token = default)
            => Extract(getOutputPath, overwrite, preserveTimestamp, DefaultOutBufferSize, token);

        public void Extract(Func<Entry, string?> getOutputPath, bool overwrite, bool preserveTimestamp, int outputBufferSize, CancellationToken token = default)
        {
            ArchiveStreamsCallback? streamCallback = null;
            outputBufferSize = Math.Max(DefaultOutBufferSize, outputBufferSize);

            try
            {
                streamCallback              =  ArchiveStreamsCallback.Create(getOutputPath, Entries, overwrite, preserveTimestamp, outputBufferSize, token);
                streamCallback.ReadProgress += StreamCallback_ReadProperty;

                _lastSize                 = 0;
                _extractProgressStopwatch = Stopwatch.StartNew();
                _archive?.Extract(null, 0xFFFFFFFF, 0, streamCallback);
            }
            finally
            {
                _extractProgressStopwatch.Stop();
                if (streamCallback != null)
                {
                    streamCallback.ReadProgress -= StreamCallback_ReadProperty;
                }
            }
        }

        public Task ExtractAsync(string outputFolder, bool overwrite = true, CancellationToken token = default)
            => ExtractAsync(entry => GetEntryPathInner(entry, outputFolder), overwrite, true, DefaultOutBufferSize, token);

        public Task ExtractAsync(string outputFolder, bool overwrite, int outputBufferSize, CancellationToken token = default)
            => ExtractAsync(entry => GetEntryPathInner(entry, outputFolder), overwrite, true, outputBufferSize, token);

        public Task ExtractAsync(string outputFolder, bool overwrite, bool preserveTimestamp, int outputBufferSize, CancellationToken token = default)
            => ExtractAsync(entry => GetEntryPathInner(entry, outputFolder), overwrite, preserveTimestamp, outputBufferSize, token);

        public Task ExtractAsync(Func<Entry, string?> getOutputPath, CancellationToken token = default)
            => ExtractAsync(getOutputPath, true, true, DefaultOutBufferSize, token);

        public Task ExtractAsync(Func<Entry, string?> getOutputPath, bool overwrite, CancellationToken token = default)
            => ExtractAsync(getOutputPath, overwrite, true, DefaultOutBufferSize, token);

        public Task ExtractAsync(Func<Entry, string?> getOutputPath, bool overwrite, int outputBufferSize, CancellationToken token = default)
            => ExtractAsync(getOutputPath, overwrite, true, outputBufferSize, token);

        public Task ExtractAsync(Func<Entry, string?> getOutputPath, bool overwrite, bool preserveTimestamp, int outputBufferSize, CancellationToken token = default)
            => Task.Factory.StartNew(
                () => Extract(getOutputPath, overwrite, preserveTimestamp, outputBufferSize, token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        private static string GetEntryPathInner(Entry entry, string outputFolder)
            => Path.Combine(outputFolder, entry.FileName ?? string.Empty);

        private static List<Entry> GetEntriesInner(IInArchive? archive, IInStream archiveStream)
        {
            if (archive == null)
            {
                throw new InvalidOperationException("Archive is not initialized");
            }

            List<Entry> entries  = [];
            const ulong checkPos = 32 * 1024;
            int         hResult  = archive.Open(archiveStream, checkPos, null);

            if (hResult != 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }

            uint itemsCount = archive.GetNumberOfItems();
            for (uint index = 0; index < itemsCount; index++)
            {
                entries.Add(Entry.Create(archive, index));
            }

            return entries;
        }

        private ulong GetLastSize(ulong input)
        {
            if (_lastSize > input)
            {
                _lastSize = input;
            }

            ulong a = input - _lastSize;
            _lastSize = input;
            return a;
        }

        private void UpdateProgress(ExtractProgressProp e)
            => ExtractProgress?.Invoke(this, e);

        private void StreamCallback_ReadProperty(object? sender, FileProgressProperty e)
            => UpdateProgress(new ExtractProgressProp(GetLastSize(e.StartRead),
                                                   e.StartRead, e.EndRead,
                                                   _extractProgressStopwatch.Elapsed.TotalSeconds, e.Count,
                                                   Count));

        private static int SearchMaxSignatureLength()
            => FormatIdentity.Signatures.Values.Select(GetSignatureLength).Prepend(0).Max();

        private static int GetSignatureLength(FormatProperties format)
        {
            int len = 0;
            if (format.SignatureOffsets != null)
            {
                len += format.SignatureOffsets.Max();
            }

            len += format.SignatureData.Length;
            return len;
        }

        private static bool GuessFormatFromSignature(Stream stream, out SevenZipFormat format)
        {
            int maxLenSignature = SearchMaxSignatureLength();
            format = SevenZipFormat.Undefined;

            if (!stream.CanSeek)
            {
                throw new InvalidOperationException("Stream must be seekable to detect the format properly!");
            }

            if (maxLenSignature > stream.Length)
            {
                maxLenSignature = (int)stream.Length;
            }

            byte[] archiveFileSignature = ArrayPool<byte>.Shared.Rent(maxLenSignature);
            try
            {
                int bytesRead = stream.ReadAtLeast(archiveFileSignature.AsSpan(0, maxLenSignature), maxLenSignature, false);
                stream.Position -= bytesRead;

                if (bytesRead != maxLenSignature)
                {
                    return false;
                }

                foreach (KeyValuePair<SevenZipFormat, FormatProperties> pair in FormatIdentity.Signatures)
                {
                    int[] offsets = pair.Value.SignatureOffsets;
                    foreach (int offset in offsets)
                    {
                        if (maxLenSignature < offset + pair.Value.SignatureData.Length)
                        {
                            continue;
                        }

                        if (!archiveFileSignature.AsSpan(offset, pair.Value.SignatureData.Length)
                                                 .SequenceEqual(pair.Value.SignatureData))
                        {
                            continue;
                        }

                        format = pair.Key;
                        return true;
                    }
                }

                format = SevenZipFormat.Undefined;
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(archiveFileSignature);
            }
        }

        public void Dispose()
        {
            _archiveStream.Dispose();
            _archive?.Close();

            GC.SuppressFinalize(this);
        }
    }
}
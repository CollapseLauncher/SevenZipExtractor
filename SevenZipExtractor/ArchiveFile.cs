using SevenZipExtractor.Enum;
using SevenZipExtractor.Event;
using SevenZipExtractor.Format;
using SevenZipExtractor.Interface;
using SevenZipExtractor.IO.Callback;
using SevenZipExtractor.IO.Wrapper;
using SevenZipExtractor.Unmanaged;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;

namespace SevenZipExtractor
{
    public sealed class ArchiveFile : IDisposable
    {
        private const    int              DefaultOutBufferSize = 4 << 10;
        private readonly IInArchive?      _archive;
        private readonly InStreamWrapper  _archiveStream;
        private          ulong            _lastSize;
        private          Stopwatch        _extractProgressStopwatch = Stopwatch.StartNew();

        public event EventHandler<ExtractProgressProp>? ExtractProgress;

        public List<Entry> Entries { get; }
        public int Count { get; }

        public ArchiveFile(string archiveFilePath) :
            this(File.Open(archiveFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        { }

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
            _archiveStream = new InStreamWrapper(archiveStream);
            Entries        = GetEntriesInner();
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
            => Extract(entry => GetEntryPathInner(entry, outputFolder), overwrite, DefaultOutBufferSize, token);

        public void Extract(string outputFolder, bool overwrite, int outputBufferSize, CancellationToken token = default)
            => Extract(entry => GetEntryPathInner(entry, outputFolder), overwrite, outputBufferSize, token);

        private string GetEntryPathInner(Entry entry, string outputFolder)
            => Path.Combine(outputFolder, entry.FileName ?? string.Empty);

        public void Extract(Func<Entry, string?> getOutputPath, CancellationToken token = default)
            => Extract(getOutputPath, true, DefaultOutBufferSize, token);

        public void Extract(Func<Entry, string?> getOutputPath, bool overwrite, CancellationToken token = default)
            => Extract(getOutputPath, overwrite, DefaultOutBufferSize, token);

        public void Extract(Func<Entry, string?> getOutputPath, bool overwrite, int outputBufferSize, CancellationToken token = default)
        {
            ArchiveStreamsCallback? streamCallback = null;
            outputBufferSize = Math.Max(DefaultOutBufferSize, outputBufferSize);

            try
            {
                streamCallback              =  ArchiveStreamsCallback.Create(getOutputPath, Entries, overwrite, outputBufferSize, token);
                streamCallback.ReadProgress += StreamCallback_ReadProperty;

                _extractProgressStopwatch = Stopwatch.StartNew();
                _archive?.Extract([], 0xFFFFFFFF, 0, streamCallback);
                token.ThrowIfCancellationRequested();
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
            => ExtractAsync(entry => GetEntryPathInner(entry, outputFolder), overwrite, DefaultOutBufferSize, token);

        public Task ExtractAsync(string outputFolder, bool overwrite, int outputBufferSize, CancellationToken token = default)
            => ExtractAsync(entry => GetEntryPathInner(entry, outputFolder), overwrite, outputBufferSize, token);

        public Task ExtractAsync(Func<Entry, string?> getOutputPath, bool overwrite, CancellationToken token = default)
            => Task.Factory.StartNew(() => Extract(getOutputPath, overwrite, DefaultOutBufferSize, token), token);

        public Task ExtractAsync(Func<Entry, string?> getOutputPath, bool overwrite, int outputBufferSize, CancellationToken token = default)
            => Task.Factory.StartNew(() => Extract(getOutputPath, overwrite, outputBufferSize, token), token);

        private List<Entry> GetEntriesInner()
        {
            List<Entry> entries = [];
            const ulong checkPos = 32 * 1024;
            int open = _archive?.Open(_archiveStream, checkPos, null) ?? 0;

            if (open != 0)
            {
                throw new InvalidOperationException("Unable to get entries from the archive stream");
            }

            uint itemsCount = _archive?.GetNumberOfItems() ?? 0;

            for (uint fileIndex = 0; fileIndex < itemsCount; fileIndex++)
            {
                string? fileName = GetProperty<string>(fileIndex, ItemPropId.kpidPath);
                bool isFolder = GetProperty<bool>(fileIndex, ItemPropId.kpidIsFolder);
                bool isEncrypted = GetProperty<bool>(fileIndex, ItemPropId.kpidEncrypted);
                ulong size = GetProperty<ulong>(fileIndex, ItemPropId.kpidSize);
                ulong packedSize = GetProperty<ulong>(fileIndex, ItemPropId.kpidPackedSize);
                DateTime creationTime = GetProperty<DateTime>(fileIndex, ItemPropId.kpidCreationTime);
                DateTime lastWriteTime = GetProperty<DateTime>(fileIndex, ItemPropId.kpidLastWriteTime);
                DateTime lastAccessTime = GetProperty<DateTime>(fileIndex, ItemPropId.kpidLastAccessTime);
                uint crc = GetProperty<uint>(fileIndex, ItemPropId.kpidCRC);
                uint attributes = GetProperty<uint>(fileIndex, ItemPropId.kpidAttributes);
                string? comment = GetProperty<string>(fileIndex, ItemPropId.kpidComment);
                string? hostOs = GetProperty<string>(fileIndex, ItemPropId.kpidHostOS);
                string? method = GetProperty<string>(fileIndex, ItemPropId.kpidMethod);

                bool isSplitBefore = GetProperty<bool>(fileIndex, ItemPropId.kpidSplitBefore);
                bool isSplitAfter = GetProperty<bool>(fileIndex, ItemPropId.kpidSplitAfter);

                entries.Add(new Entry(_archive, fileIndex)
                {
                    FileName = fileName,
                    IsFolder = isFolder,
                    IsEncrypted = isEncrypted,
                    Size = size,
                    PackedSize = packedSize,
                    CreationTime = creationTime,
                    LastWriteTime = lastWriteTime,
                    LastAccessTime = lastAccessTime,
                    Crc = crc,
                    Attributes = attributes,
                    Comment = comment,
                    HostOs = hostOs,
                    Method = method,
                    IsSplitBefore = isSplitBefore,
                    IsSplitAfter = isSplitAfter
                });
            }

            return entries;
        }
        private T? GetProperty<T>(uint fileIndex, ItemPropId name)
        {
            ComVariant propVariant = ComVariant.Null;
            _archive?.GetProperty(fileIndex, name, ref propVariant);

            return propVariant.VarType switch
                   {
                       VarEnum.VT_FILETIME => (T)(object)DateTime.FromFileTime(propVariant.GetRawDataRef<long>()),
                       _ => propVariant.As<T>()
                   };
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
            =>
            UpdateProgress(new ExtractProgressProp(GetLastSize(e.StartRead),
                                                   e.StartRead, e.EndRead,
                                                   _extractProgressStopwatch.Elapsed.TotalSeconds, e.Count,
                                                   Count));

        

        private int SearchMaxSignatureLength()
            => FormatIdentity.Signatures.Values.Select(GetSignatureLength).Prepend(0).Max();

        private int GetSignatureLength(FormatProperties format)
        {
            int len = 0;
            if (format.SignatureOffsets != null)
            {
                len += format.SignatureOffsets.Max();
            }

            len += format.SignatureData.Length;
            return len;
        }

        private bool GuessFormatFromSignature(Stream stream, out SevenZipFormat format)
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

            Span<byte> archiveFileSignature = new byte[maxLenSignature];
            int        bytesRead            = stream.ReadAtLeast(archiveFileSignature, maxLenSignature, false);

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

                    if (!archiveFileSignature.Slice(offset, pair.Value.SignatureData.Length)
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

        public void Dispose()
        {
            _archiveStream?.Dispose();
            _archive?.Close();

            GC.SuppressFinalize(this);
        }
    }
}
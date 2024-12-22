using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace SevenZipExtractor
{
    public struct ExtractProgressProp
    {
        public ExtractProgressProp(ulong Read, ulong TotalRead, ulong TotalSize, double TotalSecond, int Count, int TotalCount)
        {
            this.Read = Read;
            this.TotalRead = TotalRead;
            this.TotalSize = TotalSize;
            this.Speed = (ulong)(TotalRead / TotalSecond);
            this.Count = Count;
            this.TotalCount = TotalCount;
        }
        public int Count { get; set; }
        public int TotalCount { get; set; }
        public ulong Read { get; private set; }
        public ulong TotalRead { get; private set; }
        public ulong TotalSize { get; private set; }
        public ulong Speed { get; private set; }
        public double PercentProgress => (TotalRead / (double)TotalSize) * 100;
        public TimeSpan TimeLeft => TimeSpan.FromSeconds((TotalSize - TotalRead) / (double)Speed);
    }

    public sealed class ArchiveFile : IDisposable
    {
#nullable enable
        private readonly IInArchive? archive;
        private readonly InStreamWrapper? archiveStream;
        private List<Entry?>? entries;
        private int TotalCount;
        private ulong LastSize;

        public event EventHandler<ExtractProgressProp>? ExtractProgress;
        public void UpdateProgress(ExtractProgressProp e) => ExtractProgress?.Invoke(this, e);

        public ArchiveFile(string? archiveFilePath)
        {
            if (string.IsNullOrEmpty(archiveFilePath))
                throw new ArgumentNullException(nameof(archiveFilePath));

            if (!this.GuessFormatFromSignature(archiveFilePath, out SevenZipFormat? format))
                throw new SevenZipException(Path.GetFileName(archiveFilePath) + " is not a known archive type");

            this.archive = SevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format ?? SevenZipFormat.Undefined]);
            this.archiveStream = new InStreamWrapper(File.OpenRead(archiveFilePath));
        }

        public ArchiveFile(Stream archiveStream, SevenZipFormat? format = null)
        {
            if (archiveStream == null)
            {
                throw new SevenZipException("archiveStream is null");
            }

            if (format == null)
            {
                if (!this.GuessFormatFromSignature(archiveStream, out format))
                    throw new SevenZipException("Unable to guess format automatically");
            }

            this.archive = SevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format ?? SevenZipFormat.Undefined]);
            this.archiveStream = new InStreamWrapper(archiveStream);
            this.TotalCount = Entries.Sum(x => x?.IsFolder ?? false ? 0 : 1);
        }

        public void Extract(string outputFolder, bool overwrite = false)
        {
            this.Extract(entry =>
            {
                string fileName = Path.Combine(outputFolder, entry?.FileName ?? string.Empty);

                if (entry == null) return null;
                if (entry.IsFolder) return fileName;
                if (!File.Exists(fileName) || overwrite) return fileName;

                return null;
            });
        }

        public void Extract(Func<Entry?, string?> getOutputPath, CancellationToken Token = new CancellationToken())
        {
            List<Func<FileStream>?> fileStreams = new List<Func<FileStream>?>();
            ArchiveStreamsCallback? streamCallback = null;

            try
            {
                FileStreamOptions fileStreamOptions = new FileStreamOptions()
                {
                    Mode = FileMode.Create,
                    Access = FileAccess.Write,
                    Share = FileShare.ReadWrite,
                    BufferSize = 1 << 20
                };

                foreach (Entry? entry in Entries)
                {
                    string? outputPath = getOutputPath(entry);

                    if (string.IsNullOrEmpty(outputPath) || string.IsNullOrWhiteSpace(outputPath)) // getOutputPath = null or empty means SKIP
                    {
                        fileStreams.Add(null);
                        continue;
                    }

                    if (entry?.IsFolder ?? false)
                    {
                        Directory.CreateDirectory(outputPath);
                        fileStreams.Add(null);
                        continue;
                    }

                    // Always unassign read-only attribute from file
                    FileInfo fileInfo = new FileInfo(outputPath);
                    if (fileInfo.Exists)
                    {
                        fileInfo.IsReadOnly = false;
                    }

                    fileInfo.Directory?.Create();

                    fileStreams.Add(() => fileInfo.Open(fileStreamOptions));
                }

                ExtractProgressStopwatch = Stopwatch.StartNew();
                streamCallback = new ArchiveStreamsCallback(fileStreams, Token);
                streamCallback.ReadProgress += StreamCallback_ReadProperty;

                this.archive?.Extract(null, 0xFFFFFFFF, 0, streamCallback);
                Token.ThrowIfCancellationRequested();
            }
            finally
            {
                ExtractProgressStopwatch.Stop();
                fileStreams.Clear();

                if (streamCallback != null)
                    streamCallback.ReadProgress -= StreamCallback_ReadProperty;
            }
        }

        private ulong GetLastSize(ulong input)
        {
            if (LastSize > input)
                LastSize = input;

            ulong a = input - LastSize;
            LastSize = input;
            return a;
        }

        Stopwatch ExtractProgressStopwatch = Stopwatch.StartNew();
        private void StreamCallback_ReadProperty(object? sender, FileProgressProperty e)
        {
            UpdateProgress(new ExtractProgressProp(GetLastSize(e.StartRead),
                e.StartRead, e.EndRead, ExtractProgressStopwatch.Elapsed.TotalSeconds, e.Count, TotalCount));
        }

        public List<Entry?> Entries
        {
            get
            {
                if (this.entries != null)
                {
                    return this.entries;
                }

                ulong checkPos = 32 * 1024;
                int open = this.archive?.Open(this.archiveStream, checkPos, null) ?? 0;

                if (open != 0)
                {
                    throw new SevenZipException("Unable to open archive");
                }

                uint itemsCount = this.archive?.GetNumberOfItems() ?? 0;

                this.entries = new List<Entry?>();

                for (uint fileIndex = 0; fileIndex < itemsCount; fileIndex++)
                {
                    string? fileName = this.GetProperty<string>(fileIndex, ItemPropId.kpidPath);
                    bool isFolder = this.GetProperty<bool>(fileIndex, ItemPropId.kpidIsFolder);
                    bool isEncrypted = this.GetProperty<bool>(fileIndex, ItemPropId.kpidEncrypted);
                    ulong size = this.GetProperty<ulong>(fileIndex, ItemPropId.kpidSize);
                    ulong packedSize = this.GetProperty<ulong>(fileIndex, ItemPropId.kpidPackedSize);
                    DateTime creationTime = this.GetProperty<DateTime>(fileIndex, ItemPropId.kpidCreationTime);
                    DateTime lastWriteTime = this.GetProperty<DateTime>(fileIndex, ItemPropId.kpidLastWriteTime);
                    DateTime lastAccessTime = this.GetProperty<DateTime>(fileIndex, ItemPropId.kpidLastAccessTime);
                    uint crc = this.GetProperty<uint>(fileIndex, ItemPropId.kpidCRC);
                    uint attributes = this.GetProperty<uint>(fileIndex, ItemPropId.kpidAttributes);
                    string? comment = this.GetProperty<string>(fileIndex, ItemPropId.kpidComment);
                    string? hostOS = this.GetProperty<string>(fileIndex, ItemPropId.kpidHostOS);
                    string? method = this.GetProperty<string>(fileIndex, ItemPropId.kpidMethod);

                    bool isSplitBefore = this.GetProperty<bool>(fileIndex, ItemPropId.kpidSplitBefore);
                    bool isSplitAfter = this.GetProperty<bool>(fileIndex, ItemPropId.kpidSplitAfter);

                    this.entries.Add(new Entry(this.archive, fileIndex)
                    {
                        FileName = fileName,
                        IsFolder = isFolder,
                        IsEncrypted = isEncrypted,
                        Size = size,
                        PackedSize = packedSize,
                        CreationTime = creationTime,
                        LastWriteTime = lastWriteTime,
                        LastAccessTime = lastAccessTime,
                        CRC = crc,
                        Attributes = attributes,
                        Comment = comment,
                        HostOS = hostOS,
                        Method = method,
                        IsSplitBefore = isSplitBefore,
                        IsSplitAfter = isSplitAfter
                    });
                }

                return this.entries;
            }
        }

        private T? GetProperty<T>(uint fileIndex, ItemPropId name)
        {
            PropVariant propVariant = new PropVariant();
            this.archive?.GetProperty(fileIndex, name, ref propVariant);
            T? obj = propVariant.GetObjectAndClear<T>();
            if (obj == null) return default;
            return obj;
        }

        private bool GuessFormatFromSignature(string filePath, out SevenZipFormat? format)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return GuessFormatFromSignature(fileStream, out format);
            }
        }

        private int SearchMaxSignatureLength()
        {
            int maxLen = 0;
            foreach (FormatProperties format in Formats.FileSignatures.Values)
            {
                int len = GetSignatureLength(format);
                if (len > maxLen) maxLen = len;
            }

            return maxLen;
        }

        private int GetSignatureLength(FormatProperties format)
        {
            int len = 0;
            if (format.SignatureOffsets != null)
                len += format.SignatureOffsets.Max();

            len += format.SignatureData.Length;
            return len;
        }

        private bool GuessFormatFromSignature(Stream stream, out SevenZipFormat? format)
        {
            int maxLenSignature = SearchMaxSignatureLength();

            if (!stream.CanSeek)
                throw new SevenZipException("Stream must be seekable to detect the format properly!");

            if (maxLenSignature > stream.Length)
                maxLenSignature = (int)stream.Length;

            Span<byte> archiveFileSignature = new byte[maxLenSignature];
            int bytesRead = stream.ReadAtLeast(archiveFileSignature, maxLenSignature, false);

            stream.Position -= bytesRead;

            if (bytesRead != maxLenSignature)
            {
                format = SevenZipFormat.Undefined;
                return false;
            }

            foreach (KeyValuePair<SevenZipFormat, FormatProperties> pair in Formats.FileSignatures)
            {
                int[] offsets = pair.Value.SignatureOffsets ?? [0];
                foreach (int offset in offsets)
                {
                    if (maxLenSignature < offset + pair.Value.SignatureData.Length) continue;
                    if (archiveFileSignature.Slice(offset, pair.Value.SignatureData.Length).SequenceEqual(pair.Value.SignatureData))
                    {
                        format = pair.Key;
                        return true;
                    }
                }
            }

            format = SevenZipFormat.Undefined;
            return false;
        }

        ~ArchiveFile() => this.Dispose();

        public void Dispose()
        {
            this.archiveStream?.Dispose();
            this.archive?.Close();

            GC.SuppressFinalize(this);
        }
    }
}

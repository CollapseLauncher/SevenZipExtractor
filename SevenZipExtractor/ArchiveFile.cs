using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        public TimeSpan TimeLeft => TimeSpan.FromSeconds((TotalSize - TotalRead) / Speed);
    }

    public sealed class ArchiveFile : IDisposable
    {
        private readonly IInArchive archive;
        private readonly InStreamWrapper archiveStream;
        private List<Entry> entries;
        private int TotalCount;

        public event EventHandler<ExtractProgressProp> ExtractProgress;
        public void UpdateProgress(ExtractProgressProp e) => ExtractProgress?.Invoke(this, e);

        public ArchiveFile(string archiveFilePath, string libraryFilePath = null)
        {
            SevenZipFormat? format;
            string extension = Path.GetExtension(archiveFilePath);

            if (!this.GuessFormatFromExtension(extension, out format) || !this.GuessFormatFromSignature(archiveFilePath, out format))
                throw new SevenZipException(Path.GetFileName(archiveFilePath) + " is not a known archive type");

            this.archive = SevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format.Value]);
            this.archiveStream = new InStreamWrapper(File.OpenRead(archiveFilePath));
        }

        public ArchiveFile(Stream archiveStream, SevenZipFormat? format = null, string libraryFilePath = null)
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

            this.archive = SevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format.Value]);
            this.archiveStream = new InStreamWrapper(archiveStream);
            this.TotalCount = Entries.Sum(x => x.IsFolder ? 0 : 1);
        }

        public void Extract(string outputFolder, bool overwrite = false)
        {
            this.Extract(entry =>
            {
                string fileName = Path.Combine(outputFolder, entry.FileName);

                if (entry.IsFolder)
                {
                    return fileName;
                }

                if (!File.Exists(fileName) || overwrite)
                {
                    return fileName;
                }

                return null;
            });
        }

        public void Extract(Func<Entry, string> getOutputPath, CancellationToken Token = new CancellationToken())
        {
            List<FileStream> fileStreams = new List<FileStream>();
            ArchiveStreamsCallback streamCallback = null;

            try
            {
                foreach (Entry entry in Entries)
                {
                    string outputPath = getOutputPath(entry);

                    if (outputPath == null) // getOutputPath = null means SKIP
                    {
                        fileStreams.Add(null);
                        continue;
                    }

                    if (entry.IsFolder)
                    {
                        Directory.CreateDirectory(outputPath);
                        fileStreams.Add(null);
                        continue;
                    }

                    string directoryName = Path.GetDirectoryName(outputPath);

                    if (!string.IsNullOrWhiteSpace(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    fileStreams.Add(File.Create(outputPath));
                }

                ExtractProgressStopwatch = Stopwatch.StartNew();
                streamCallback = new ArchiveStreamsCallback(fileStreams, Token);
                streamCallback.ReadProgress += StreamCallback_ReadProperty;

                this.archive.Extract(null, 0xFFFFFFFF, 0, streamCallback);
                Token.ThrowIfCancellationRequested();
            }
            catch (Exception) { throw; }
            finally
            {
                ExtractProgressStopwatch.Stop();
                fileStreams.ForEach(x => x?.Dispose());
                fileStreams.Clear();
                streamCallback.ReadProgress -= StreamCallback_ReadProperty;
            }
        }

        ulong LastSize = 0;

        private ulong GetLastSize(ulong input)
        {
            if (LastSize > input)
                LastSize = input;

            ulong a = input - LastSize;
            LastSize = input;
            return a;
        }

        Stopwatch ExtractProgressStopwatch = Stopwatch.StartNew();
        private void StreamCallback_ReadProperty(object sender, FileProgressProperty e)
        {
            UpdateProgress(new ExtractProgressProp(GetLastSize(e.StartRead),
                e.StartRead, e.EndRead, ExtractProgressStopwatch.Elapsed.TotalSeconds, e.Count, TotalCount));
        }

        public List<Entry> Entries
        {
            get
            {
                if (this.entries != null)
                {
                    return this.entries;
                }

                ulong checkPos = 32 * 1024;
                int open = this.archive.Open(this.archiveStream, checkPos, null);

                if (open != 0)
                {
                    throw new SevenZipException("Unable to open archive");
                }

                uint itemsCount = this.archive.GetNumberOfItems();

                this.entries = new List<Entry>();

                for (uint fileIndex = 0; fileIndex < itemsCount; fileIndex++)
                {
                    string fileName = this.GetProperty<string>(fileIndex, ItemPropId.kpidPath);
                    bool isFolder = this.GetProperty<bool>(fileIndex, ItemPropId.kpidIsFolder);
                    bool isEncrypted = this.GetProperty<bool>(fileIndex, ItemPropId.kpidEncrypted);
                    ulong size = this.GetProperty<ulong>(fileIndex, ItemPropId.kpidSize);
                    ulong packedSize = this.GetProperty<ulong>(fileIndex, ItemPropId.kpidPackedSize);
                    DateTime creationTime = GetDateTime(fileIndex, ItemPropId.kpidCreationTime);
                    DateTime lastWriteTime = GetDateTime(fileIndex, ItemPropId.kpidLastWriteTime);
                    DateTime lastAccessTime = GetDateTime(fileIndex, ItemPropId.kpidLastAccessTime);
                    uint crc = this.GetPropertySafe<uint>(fileIndex, ItemPropId.kpidCRC);
                    uint attributes = this.GetPropertySafe<uint>(fileIndex, ItemPropId.kpidAttributes);
                    string comment = this.GetPropertySafe<string>(fileIndex, ItemPropId.kpidComment);
                    string hostOS = this.GetPropertySafe<string>(fileIndex, ItemPropId.kpidHostOS);
                    string method = this.GetPropertySafe<string>(fileIndex, ItemPropId.kpidMethod);

                    bool isSplitBefore = this.GetPropertySafe<bool>(fileIndex, ItemPropId.kpidSplitBefore);
                    bool isSplitAfter = this.GetPropertySafe<bool>(fileIndex, ItemPropId.kpidSplitAfter);

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

        private T GetPropertySafe<T>(uint fileIndex, ItemPropId name)
        {
            try
            {
                return this.GetProperty<T>(fileIndex, name);
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }

        private DateTime GetDateTime(uint fileIndex, ItemPropId name)
        {
            PropVariant propVariant = new PropVariant();
            this.archive.GetProperty(fileIndex, name, ref propVariant);

            return DateTime.FromFileTime(propVariant.longValue);
        }

        private T GetProperty<T>(uint fileIndex, ItemPropId name)
        {
            PropVariant propVariant = new PropVariant();
            this.archive.GetProperty(fileIndex, name, ref propVariant);
            object value = propVariant.GetObject();

            if (propVariant.VarType == VarEnum.VT_EMPTY)
            {
                propVariant.Clear();
                return default;
            }

            propVariant.Clear();
            if (value == null)
            {
                return default;
            }

            Type type = typeof(T);
            bool isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            Type underlyingType = isNullable ? Nullable.GetUnderlyingType(type) : type;

            string valueString = value.ToString();
            T result = (T)Convert.ChangeType(valueString, underlyingType);

            return result;
        }

        private bool GuessFormatFromExtension(string fileExtension, out SevenZipFormat? format)
        {
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                format = SevenZipFormat.Undefined;
                return false;
            }

            fileExtension = fileExtension.TrimStart('.').Trim().ToLowerInvariant();

            if (fileExtension.Equals("rar"))
            {
                // 7z has different GUID for Pre-RAR5 and RAR5, but they have both same extension (.rar)
                // If it is [0x52 0x61 0x72 0x21 0x1A 0x07 0x01 0x00] then file is RAR5 otherwise RAR.
                // https://www.rarlab.com/technote.htm

                // We are unable to guess right format just by looking at extension and have to check signature

                format = SevenZipFormat.Undefined;
                return false;
            }

            if (!Formats.ExtensionFormatMapping.ContainsKey(fileExtension))
            {
                format = SevenZipFormat.Undefined;
                return false;
            }

            format = Formats.ExtensionFormatMapping[fileExtension];
            return true;
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
                int offset = 0;
                int i = 0;
                if (pair.Value.SignatureOffsets == null)
                {
                    if (archiveFileSignature.Slice(0, pair.Value.SignatureData.Length).SequenceEqual(pair.Value.SignatureData))
                    {
                        format = pair.Key;
                        return true;
                    }

                    continue;
                }

                while (i < pair.Value.SignatureOffsets.Length)
                {
                    offset = pair.Value.SignatureOffsets[i];
                    if (archiveFileSignature.Slice(offset, pair.Value.SignatureData.Length).SequenceEqual(pair.Value.SignatureData))
                    {
                        format = pair.Key;
                        return true;
                    }

                    i++;
                }
            }

            format = SevenZipFormat.Undefined;
            return false;
        }

        ~ArchiveFile() => this.Dispose();

        public void Dispose()
        {
            this.archiveStream?.Dispose();

            if (this.archive != null)
                this.archive.Close();

            GC.SuppressFinalize(this);
        }
    }
}

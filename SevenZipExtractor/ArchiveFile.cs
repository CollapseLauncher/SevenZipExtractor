using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SevenZipExtractor
{
    public class ArchiveFile : IDisposable
    {
        public class ExtractProgressProp
        {
            public ExtractProgressProp(ulong Read, ulong TotalRead, ulong TotalSize, double TotalSecond)
            {
                this.Read = Read;
                this.TotalRead = TotalRead;
                this.TotalSize = TotalSize;
                this.Speed = (ulong)(TotalRead / TotalSecond);
            }

            public ulong Read { get; private set; }
            public ulong TotalRead { get; private set; }
            public ulong TotalSize { get; private set; }
            public ulong Speed { get; private set; }
            public double PercentProgress => (TotalRead / (double)TotalSize) * 100;
            public TimeSpan TimeLeft => TimeSpan.FromSeconds((TotalSize - TotalRead) / Speed);
        }

        private SevenZipHandle sevenZipHandle;
        private readonly IInArchive archive;
        private readonly InStreamWrapper archiveStream;
        private IList<Entry> entries;

        private string libraryFilePath;
        public event EventHandler<ExtractProgressProp> ExtractProgress;
        public void UpdateProgress(ExtractProgressProp e) => ExtractProgress?.Invoke(this, e);

        public ArchiveFile(string archiveFilePath, string libraryFilePath = null)
        {

            this.libraryFilePath = libraryFilePath;

            this.InitializeAndValidateLibrary();

            if (!File.Exists(archiveFilePath))
            {
                throw new SevenZipException("Archive file not found");
            }

            SevenZipFormat format;
            string extension = Path.GetExtension(archiveFilePath);

            if (this.GuessFormatFromExtension(extension, out format))
            {
                // great
            }
            else if (this.GuessFormatFromSignature(archiveFilePath, out format))
            {
                // success
            }
            else
            {
                throw new SevenZipException(Path.GetFileName(archiveFilePath) + " is not a known archive type");
            }

            this.archive = this.sevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format]);
            this.archiveStream = new InStreamWrapper(File.OpenRead(archiveFilePath));
        }

        public ArchiveFile(Stream archiveStream, SevenZipFormat? format = null, string libraryFilePath = null)
        {
            this.libraryFilePath = libraryFilePath;

            this.InitializeAndValidateLibrary();

            if (archiveStream == null)
            {
                throw new SevenZipException("archiveStream is null");
            }

            if (format == null)
            {
                SevenZipFormat guessedFormat;

                if (this.GuessFormatFromSignature(archiveStream, out guessedFormat))
                {
                    format = guessedFormat;
                }
                else
                {
                    throw new SevenZipException("Unable to guess format automatically");
                }
            }

            this.archive = this.sevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format.Value]);
            this.archiveStream = new InStreamWrapper(archiveStream);
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
            IList<CancellableFileStream> fileStreams = new List<CancellableFileStream>();
            ArchiveStreamsCallback streamCallback;

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

                    fileStreams.Add(new CancellableFileStream(File.Create(outputPath), Token));
                }

                streamCallback = new ArchiveStreamsCallback(fileStreams);
                ExtractProgressStopwatch = Stopwatch.StartNew();
                streamCallback.ReadProgress += StreamCallback_ReadProperty;

                this.archive.Extract(null, 0xFFFFFFFF, 0, streamCallback);
                streamCallback.ReadProgress -= StreamCallback_ReadProperty;
            }
            finally
            {
                foreach (CancellableFileStream stream in fileStreams)
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
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
        private void StreamCallback_ReadProperty(object sender, FileProgressProperty e) =>
            UpdateProgress(new ExtractProgressProp(GetLastSize(e.StartRead),
                e.StartRead, e.EndRead, ExtractProgressStopwatch.Elapsed.TotalSeconds));

        public IList<Entry> Entries
        {
            get
            {
                if (this.entries != null)
                {
                    return this.entries;
                }

                ulong checkPos = 32 * 1024;
                int open = this.archive.Open(this.archiveStream, ref checkPos, null);

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
                    DateTime creationTime = this.GetPropertySafe<DateTime>(fileIndex, ItemPropId.kpidCreationTime);
                    DateTime lastWriteTime = this.GetPropertySafe<DateTime>(fileIndex, ItemPropId.kpidLastWriteTime);
                    DateTime lastAccessTime = this.GetPropertySafe<DateTime>(fileIndex, ItemPropId.kpidLastAccessTime);
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
                return default(T);
            }
        }

        private T GetProperty<T>(uint fileIndex, ItemPropId name)
        {
            PropVariant propVariant = new PropVariant();
            this.archive.GetProperty(fileIndex, name, ref propVariant);
            object value = propVariant.GetObject();

            if (propVariant.VarType == VarEnum.VT_EMPTY)
            {
                propVariant.Clear();
                return default(T);
            }

            propVariant.Clear();

            if (value == null)
            {
                return default(T);
            }

            Type type = typeof(T);
            bool isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            Type underlyingType = isNullable ? Nullable.GetUnderlyingType(type) : type;

            T result = (T)Convert.ChangeType(value.ToString(), underlyingType);

            return result;
        }

        private void InitializeAndValidateLibrary()
        {
            if (string.IsNullOrWhiteSpace(this.libraryFilePath))
            {
                string currentArchitecture = IntPtr.Size == 4 ? "x86" : "x64"; // magic check

                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z-" + currentArchitecture + ".dll")))
                {
                    this.libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z-" + currentArchitecture + ".dll");
                }
                else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "7z-" + currentArchitecture + ".dll")))
                {
                    this.libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "7z-" + currentArchitecture + ".dll");
                }
                else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", currentArchitecture, "7z.dll")))
                {
                    this.libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", currentArchitecture, "7z.dll");
                }
                else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, currentArchitecture, "7z.dll")))
                {
                    this.libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, currentArchitecture, "7z.dll");
                }
                else if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.dll")))
                {
                    this.libraryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.dll");
                }
            }

            if (string.IsNullOrWhiteSpace(this.libraryFilePath))
            {
                throw new SevenZipException("libraryFilePath not set");
            }

            if (!File.Exists(this.libraryFilePath))
            {
                throw new SevenZipException("7z.dll not found");
            }

            try
            {
                this.sevenZipHandle = new SevenZipHandle(this.libraryFilePath);
            }
            catch (Exception e)
            {
                throw new SevenZipException("Unable to initialize SevenZipHandle", e);
            }
        }

        private bool GuessFormatFromExtension(string fileExtension, out SevenZipFormat format)
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


        private bool GuessFormatFromSignature(string filePath, out SevenZipFormat format)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return GuessFormatFromSignature(fileStream, out format);
            }
        }

        private bool GuessFormatFromSignature(Stream stream, out SevenZipFormat format)
        {
            int longestSignature = Formats.FileSignatures.Values.OrderByDescending(v => v.Length).First().Length;

            byte[] archiveFileSignature = new byte[longestSignature];
            int bytesRead = stream.Read(archiveFileSignature, 0, longestSignature);

            stream.Position -= bytesRead; // go back o beginning

            if (bytesRead != longestSignature)
            {
                format = SevenZipFormat.Undefined;
                return false;
            }

            foreach (KeyValuePair<SevenZipFormat, byte[]> pair in Formats.FileSignatures)
            {
                if (archiveFileSignature.Take(pair.Value.Length).SequenceEqual(pair.Value))
                {
                    format = pair.Key;
                    return true;
                }
            }

            format = SevenZipFormat.Undefined;
            return false;
        }

        ~ArchiveFile()
        {
            this.Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (this.archiveStream != null)
            {
                this.archiveStream.Dispose();
            }

            if (this.archive != null)
            {
                Marshal.ReleaseComObject(this.archive);
            }

            if (this.sevenZipHandle != null)
            {
                this.sevenZipHandle.Dispose();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

using SevenZipExtractor.Enum;
using SevenZipExtractor.Interface;
using SevenZipExtractor.IO.Callback;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SevenZipExtractor
{
    public sealed class Entry
    {
        private readonly IInArchive? _archive;
        private readonly uint        _index;
        private readonly ArchiveFile _parent;

        internal Entry(IInArchive? archive, uint index, ArchiveFile parent)
        {
            _archive = archive;
            _index   = index;
            _parent  = parent;
        }

        /// <summary>
        /// Name of the file with its relative path within the archive
        /// </summary>
        public string? FileName { get; internal set; }
        /// <summary>
        /// True if entry is a folder, false if it is a file
        /// </summary>
        public bool IsFolder { get; internal set; }
        /// <summary>
        /// Original entry size
        /// </summary>
        public ulong Size { get; internal set; }
        /// <summary>
        /// Entry size in an archived state
        /// </summary>
        public ulong PackedSize { get; internal set; }

        /// <summary>
        /// Date and time of the file (entry) creation
        /// </summary>
        public DateTime CreationTime { get; internal set; }

        /// <summary>
        /// Date and time of the last change of the file (entry)
        /// </summary>
        public DateTime LastWriteTime { get; internal set; }

        /// <summary>
        /// Date and time of the last access of the file (entry)
        /// </summary>
        public DateTime LastAccessTime { get; internal set; }

        /// <summary>
        /// CRC hash of the entry
        /// </summary>
        public uint Crc { get; internal set; }

        /// <summary>
        /// Attributes of the entry
        /// </summary>
        public uint Attributes { get; internal set; }

        /// <summary>
        /// True if entry is encrypted, otherwise false
        /// </summary>
        public bool IsEncrypted { get; internal set; }

        /// <summary>
        /// Comment of the entry
        /// </summary>
        public string? Comment { get; internal set; }

        /// <summary>
        /// Compression method of the entry
        /// </summary>
        public string? Method { get; internal set; }

        /// <summary>
        /// Host operating system of the entry
        /// </summary>
        public string? HostOs { get; internal set; }

        /// <summary>
        /// True if there are parts of this file in previous split archive parts
        /// </summary>
        public bool IsSplitBefore { get; set; }

        /// <summary>
        /// True if there are parts of this file in next split archive parts
        /// </summary>
        public bool IsSplitAfter { get; set; }

        /// <summary>
        /// True if the entry is packed inside a solid block
        /// </summary>
        public bool IsSolid { get; set; }

        public override string ToString() => $"{(IsFolder ? "Folder" : "File")}: {FileName}";

        internal static Entry Create(IInArchive archive, uint index, ArchiveFile parent)
        {
            Entry entry = new(archive, index, parent)
            {
                IsFolder       = GetUnmanagedProperty<bool>(archive, index, ItemPropId.IsFolder),
                IsEncrypted    = GetUnmanagedProperty<bool>(archive, index, ItemPropId.Encrypted),
                Size           = GetUnmanagedProperty<ulong>(archive, index, ItemPropId.Size),
                PackedSize     = GetUnmanagedProperty<ulong>(archive, index, ItemPropId.PackedSize),
                Crc            = GetUnmanagedProperty<uint>(archive, index, ItemPropId.CRC),
                Attributes     = GetUnmanagedProperty<uint>(archive, index, ItemPropId.Attributes),
                IsSplitBefore  = GetUnmanagedProperty<bool>(archive, index, ItemPropId.SplitBefore),
                IsSplitAfter   = GetUnmanagedProperty<bool>(archive, index, ItemPropId.SplitAfter),
                IsSolid        = GetUnmanagedProperty<bool>(archive, index, ItemPropId.Solid),
                CreationTime   = DateTime.FromFileTime(GetUnmanagedProperty<long>(archive, index, ItemPropId.CreationTime)),
                LastWriteTime  = DateTime.FromFileTime(GetUnmanagedProperty<long>(archive, index, ItemPropId.LastWriteTime)),
                LastAccessTime = DateTime.FromFileTime(GetUnmanagedProperty<long>(archive, index, ItemPropId.LastAccessTime)),
                FileName       = GetStringProperty(archive, index, ItemPropId.Path),
                Comment        = GetStringProperty(archive, index, ItemPropId.Comment),
                HostOs         = GetStringProperty(archive, index, ItemPropId.HostOS),
                Method         = GetStringProperty(archive, index, ItemPropId.Method)
            };

            return entry;
        }

        private static unsafe T GetUnmanagedProperty<T>(IInArchive archive, uint fileIndex, ItemPropId name)
            where T : unmanaged
        {
            Unsafe.SkipInit(out ComVariant propVariant);

            try
            {
                archive.GetProperty(fileIndex, name, (ComVariant*)Unsafe.AsPointer(ref propVariant));
                ref T data = ref propVariant.GetRawDataRef<T>();
                return data; // Return and copy the value from ref
            }
            finally
            {
                propVariant.Dispose();
            }
        }

        private static unsafe string? GetStringProperty(IInArchive archive, uint fileIndex, ItemPropId name)
        {
            Unsafe.SkipInit(out ComVariant propVariant);

            try
            {
                archive.GetProperty(fileIndex, name, (ComVariant*)Unsafe.AsPointer(ref propVariant));
                ref byte data = ref propVariant.GetRawDataRef<byte>();
                if (Unsafe.IsNullRef(ref data) || data == '\0')
                {
                    return null;
                }

                void** ptr        = (void**)Unsafe.AsPointer(ref data);
                int    byteLength = *((int*)*ptr - 1) / 2;

                return byteLength <= 0 ? null : new string((char*)*ptr, 0, byteLength);
            }
            finally
            {
                propVariant.Dispose();
            }
        }

        /// <summary>
        /// Extract this specific entry of the file. Use <seealso cref="ArchiveFile.Extract"/> instead if you want to extract the whole archive.
        /// Extracting specific entry one-by-one might be slower for some formats, especially with Solid-block enabled archives.
        /// </summary>
        /// <param name="fileName">Path where the file will be extracted.</param>
        /// <param name="preserveTimestamp">Preserve the timestamp of the file.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public void Extract(string fileName, bool preserveTimestamp = true, CancellationToken token = default)
        {
            if (IsFolder)
            {
                Directory.CreateDirectory(fileName);
                return;
            }

            string? directoryName = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using FileStream fileStream = File.Create(fileName);
            Extract(fileStream, preserveTimestamp, true, token);
        }

        /// <summary>
        /// Extract this specific entry of the file asynchronously. Use <seealso cref="ArchiveFile.ExtractAsync"/> instead if you want to extract the whole archive.<br/>
        /// Extracting specific entry one-by-one might be slower for some formats, especially with Solid-block enabled archives.
        /// </summary>
        /// <param name="fileName">Path where the file will be extracted.</param>
        /// <param name="preserveTimestamp">Preserve the timestamp of the file.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public Task ExtractAsync(string fileName, bool preserveTimestamp = true, CancellationToken token = default)
            => Task.Factory.StartNew(
                () => Extract(fileName, preserveTimestamp, token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        /// <summary>
        /// Extract this specific entry of the file. Use <seealso cref="ArchiveFile.Extract"/> instead if you want to extract the whole archive.<br/>
        /// Extracting specific entry one-by-one might be slower for some formats, especially with Solid-block enabled archives.
        /// </summary>
        /// <param name="stream">Output stream where the data of the file will be written into.</param>
        /// <param name="isDispose">Dispose the stream after extraction is completed.</param>
        /// <param name="preserveTimestamp">Preserve the timestamp of the file.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public void Extract(Stream stream, bool preserveTimestamp, bool isDispose = true, CancellationToken token = default)
        {
            using (ArchiveStreamCallback callback = new(_index, stream, isDispose, token))
            {
                callback.SetArchivePassword(_parent.ArchivePassword);
                _archive?.Extract(in _index, 1, 0, callback);
            }

            if (stream is FileStream fileStream)
            {
                File.SetLastWriteTime(fileStream.Name, LastWriteTime);
            }
        }

        /// <summary>
        /// Extract this specific entry of the file asynchronously. Use <seealso cref="ArchiveFile.ExtractAsync"/> instead if you want to extract the whole archive.<br/>
        /// Extracting specific entry one-by-one might be slower for some formats, especially with Solid-block enabled archives.
        /// </summary>
        /// <param name="stream">Output stream where the data of the file will be written into.</param>
        /// <param name="isDispose">Dispose the stream after extraction is completed.</param>
        /// <param name="preserveTimestamp">Preserve the timestamp of the file.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public Task ExtractAsync(Stream stream, bool preserveTimestamp, bool isDispose = true, CancellationToken token = default)
            => Task.Factory.StartNew(
                () => Extract(stream, preserveTimestamp, isDispose, token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
    }
}

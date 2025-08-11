using SevenZipExtractor.Enum;
using SevenZipExtractor.Interface;
using SevenZipExtractor.IO.Callback;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
                CreationTime   = GetProperty<DateTime>(archive, index, ItemPropId.CreationTime),
                LastWriteTime  = GetProperty<DateTime>(archive, index, ItemPropId.LastWriteTime),
                LastAccessTime = GetProperty<DateTime>(archive, index, ItemPropId.LastAccessTime),
                FileName       = GetProperty<string>(archive, index, ItemPropId.Path),
                Comment        = GetProperty<string>(archive, index, ItemPropId.Comment),
                HostOs         = GetProperty<string>(archive, index, ItemPropId.HostOS),
                Method         = GetProperty<string>(archive, index, ItemPropId.Method)
            };

            return entry;
        }

        private static unsafe T GetUnmanagedProperty<T>(IInArchive archive, uint fileIndex, ItemPropId name)
            where T : unmanaged
        {
            ComVariant propVariant = ComVariant.Null;
            archive.GetProperty(fileIndex, name, &propVariant);
            return propVariant.GetRawDataRef<T>();
        }

        private static unsafe T? GetProperty<T>(IInArchive archive, uint fileIndex, ItemPropId name)
        {
            ComVariant propVariant = ComVariant.Null;
            archive.GetProperty(fileIndex, name, &propVariant);

            return propVariant.VarType switch
                   {
                       VarEnum.VT_FILETIME => (T)(object)DateTime.FromFileTime(propVariant.GetRawDataRef<long>()),
                       _ => propVariant.As<T>()
                   };
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
        public ConfiguredTaskAwaitable ExtractAsync(string fileName, bool preserveTimestamp = true, CancellationToken token = default)
            => Task.Factory.StartNew(
                () => Extract(fileName, preserveTimestamp, token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).ConfigureAwait(false);

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
                _archive?.Extract([_index], 1, 0, callback);
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
        public ConfiguredTaskAwaitable ExtractAsync(Stream stream, bool preserveTimestamp, bool isDispose = true, CancellationToken token = default)
            => Task.Factory.StartNew(
                () => Extract(stream, preserveTimestamp, isDispose, token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).ConfigureAwait(false);
    }
}

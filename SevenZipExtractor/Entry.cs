using SevenZipExtractor.Enum;
using SevenZipExtractor.Interface;
using SevenZipExtractor.IO.Callback;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace SevenZipExtractor
{
    public sealed class Entry
    {
        private readonly IInArchive? _archive;
        private readonly uint        _index;

        internal Entry(IInArchive? archive, uint index)
        {
            _archive = archive;
            _index = index;
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
        /// Entry size in a archived state
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

        internal static Entry Create(IInArchive archive, uint index)
        {
            Entry entry = new Entry(archive, index)
            {
                IsFolder       = GetUnmanagedProperty<bool>(archive, index, ItemPropId.kpidIsFolder),
                IsEncrypted    = GetUnmanagedProperty<bool>(archive, index, ItemPropId.kpidEncrypted),
                Size           = GetUnmanagedProperty<ulong>(archive, index, ItemPropId.kpidSize),
                PackedSize     = GetUnmanagedProperty<ulong>(archive, index, ItemPropId.kpidPackedSize),
                Crc            = GetUnmanagedProperty<uint>(archive, index, ItemPropId.kpidCRC),
                Attributes     = GetUnmanagedProperty<uint>(archive, index, ItemPropId.kpidAttributes),
                IsSplitBefore  = GetUnmanagedProperty<bool>(archive, index, ItemPropId.kpidSplitBefore),
                IsSplitAfter   = GetUnmanagedProperty<bool>(archive, index, ItemPropId.kpidSplitAfter),
                IsSolid        = GetUnmanagedProperty<bool>(archive, index, ItemPropId.kpidSolid),
                CreationTime   = GetProperty<DateTime>(archive, index, ItemPropId.kpidCreationTime),
                LastWriteTime  = GetProperty<DateTime>(archive, index, ItemPropId.kpidLastWriteTime),
                LastAccessTime = GetProperty<DateTime>(archive, index, ItemPropId.kpidLastAccessTime),
                FileName       = GetProperty<string>(archive, index, ItemPropId.kpidPath),
                Comment        = GetProperty<string>(archive, index, ItemPropId.kpidComment),
                HostOs         = GetProperty<string>(archive, index, ItemPropId.kpidHostOS),
                Method         = GetProperty<string>(archive, index, ItemPropId.kpidMethod)
            };

            return entry;
        }

        private static unsafe T GetUnmanagedProperty<T>(IInArchive archive, uint fileIndex, ItemPropId name)
            where               T : unmanaged
        {
            ComVariant propVariant = ComVariant.Null;
            archive?.GetProperty(fileIndex, name, &propVariant);
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

        public void Extract(string fileName, bool preserveTimestamp = true, CancellationToken cancellationToken = default)
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
            Extract(fileStream, preserveTimestamp, cancellationToken);
        }

        public void Extract(Stream stream, bool preserveTimestamp, CancellationToken cancellationToken)
        {
            _archive?.Extract([_index], 1, 0, new ArchiveStreamCallback(_index, stream, LastWriteTime, preserveTimestamp, cancellationToken));
        }
    }
}

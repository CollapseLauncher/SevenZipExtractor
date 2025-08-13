// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
namespace SevenZipExtractor.Enum
{
    internal enum ItemPropId : uint
    {
        NoProperty = 0,

        HandlerItemIndex = 2,
        Path,
        Name,
        Extension,
        IsFolder,
        Size,
        PackedSize,
        Attributes,
        CreationTime,
        LastAccessTime,
        LastWriteTime,
        Solid,
        Commented,
        Encrypted,
        SplitBefore,
        SplitAfter,
        DictionarySize,
        CRC,
        Type,
        IsAnti,
        Method,
        HostOS,
        FileSystem,
        User,
        Group,
        Block,
        Comment,
        Position,
        Prefix,

        TotalSize = 0x1100,
        FreeSpace,
        ClusterSize,
        VolumeName,

        LocalName = 0x1200,
        Provider,

        UserDefined = 0x10000
    }
}

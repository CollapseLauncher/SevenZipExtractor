using SevenZipExtractor.Enum;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable CommentTypo

namespace SevenZipExtractor.Interface
{
    [Guid(Constants.IID_IInArchive)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface IInArchive
    {
        void Open(
            IInStream                                                  stream,
            in                                   ulong                 maxCheckStartPosition,
            [MarshalAs(UnmanagedType.Interface)] IArchiveOpenCallback? openArchiveCallback);

        void Close();
        void GetNumberOfItems(out uint count);

        void GetProperty(
            uint           index,
            ItemPropId     propID,
            out ComVariant value);

        // indices must be sorted 
        // numItems = 0xFFFFFFFF means all files
        // testMode != 0 means "test files operation"
        void Extract(
            in uint                                                      indices,
            uint                                                         numItems,
            int                                                          testMode,
            [MarshalAs(UnmanagedType.Interface)] IArchiveExtractCallback extractCallback);

        void GetArchiveProperty(
            ItemPropId     propID,
            out ComVariant value);

        uint GetNumberOfProperties();

        void GetPropertyInfo(
            uint                                       index,
            [MarshalAs(UnmanagedType.BStr)] string     name,
            out                             ItemPropId propID,
            out                             ushort     varType);

        uint GetNumberOfArchiveProperties();

        void GetArchivePropertyInfo(
            uint                                       index,
            [MarshalAs(UnmanagedType.BStr)] string     name,
            out                             ItemPropId propID,
            out                             ushort     varType);
    }
}

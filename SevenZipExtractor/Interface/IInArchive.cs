using SevenZipExtractor.Enum;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable CommentTypo

namespace SevenZipExtractor.Interface
{
    [Guid("23170F69-40C1-278A-0000-000600600000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal unsafe partial interface IInArchive
    {
        [PreserveSig]
        int Open(
            IInStream                                                  stream,
            in                                   ulong                 maxCheckStartPosition,
            [MarshalAs(UnmanagedType.Interface)] IArchiveOpenCallback? openArchiveCallback);

        void Close();
        uint GetNumberOfItems();

        void GetProperty(
            uint        index,
            ItemPropId  propID, // PROPID
            ComVariant* value); // PROPVARIANT

        // indices must be sorted 
        // numItems = 0xFFFFFFFF means all files
        // testMode != 0 means "test files operation"
        [PreserveSig]
        int Extract(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[]? indices, //[In] ref uint indices,
            uint                                                               numItems,
            int                                                                testMode,
            [MarshalAs(UnmanagedType.Interface)] IArchiveExtractCallback       extractCallback);

        void GetArchiveProperty(
            uint        propID, // PROPID
            ComVariant* value); // COMVARIANT

        uint GetNumberOfProperties();

        void GetPropertyInfo(
            uint                                           index,
            [MarshalAs(UnmanagedType.BStr)] out string     name,
            out                                 ItemPropId propID, // PROPID
            out                                 ushort     varType); //VARTYPE

        uint GetNumberOfArchiveProperties();

        void GetArchivePropertyInfo(
            uint                                   index,
            [MarshalAs(UnmanagedType.BStr)] string name,
            ref                             uint   propID, // PROPID
            ref                             ushort varType); //VARTYPE
    }
}

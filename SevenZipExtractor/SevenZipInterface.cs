using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace SevenZipExtractor
{
    internal enum ItemPropId : uint
    {
        kpidNoProperty = 0,

        kpidHandlerItemIndex = 2,
        kpidPath,
        kpidName,
        kpidExtension,
        kpidIsFolder,
        kpidSize,
        kpidPackedSize,
        kpidAttributes,
        kpidCreationTime,
        kpidLastAccessTime,
        kpidLastWriteTime,
        kpidSolid,
        kpidCommented,
        kpidEncrypted,
        kpidSplitBefore,
        kpidSplitAfter,
        kpidDictionarySize,
        kpidCRC,
        kpidType,
        kpidIsAnti,
        kpidMethod,
        kpidHostOS,
        kpidFileSystem,
        kpidUser,
        kpidGroup,
        kpidBlock,
        kpidComment,
        kpidPosition,
        kpidPrefix,

        kpidTotalSize = 0x1100,
        kpidFreeSpace,
        kpidClusterSize,
        kpidVolumeName,

        kpidLocalName = 0x1200,
        kpidProvider,

        kpidUserDefined = 0x10000
    }

    internal enum ArchivePropId : uint
    {
        kName = 0,
        kClassID,
        kExtension,
        kAddExtension,
        kUpdate,
        kKeepName,
        kStartSignature,
        kFinishSignature,
        kAssociate
    }

    internal enum AskMode : int
    {
        kExtract = 0,
        kTest,
        kSkip
    }

    internal enum OperationResult : int
    {
        kOK = 0,
        kUnSupportedMethod,
        kDataError,
        kCRCError
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PropArray
    {
        uint length;
        IntPtr pointerValues;
    }

    //
    // Summary:
    //     Represents the number of 100-nanosecond intervals since January 1, 1601. This
    //     structure is a 64-bit value.
    internal struct FILETIME
    {
        //
        // Summary:
        //     Specifies the high 32 bits of the FILETIME.
        public int dwHighDateTime;
        //
        // Summary:
        //     Specifies the low 32 bits of the FILETIME.
        public int dwLowDateTime;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal partial struct PropVariant
    {
        // Local P/Invoke
        [DllImport("ole32.dll", EntryPoint = "PropVariantClear", ExactSpelling = true)]
        private static extern unsafe int PropVariantClearInvoke(PropVariant* pvar_native);

        private static unsafe int PropVariantClear(ref PropVariant pvar)
        {
            fixed (PropVariant* pvar_native = &pvar)
            {
                int retVal = PropVariantClearInvoke(pvar_native);
                return retVal;
            }
        }

        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr pointerValue;
        [FieldOffset(8)] public byte byteValue;
        [FieldOffset(8)] public long longValue;
        [FieldOffset(8)] public FILETIME fileTime;
        [FieldOffset(8)] public PropArray propArray;

        public VarEnum VarType => (VarEnum)this.vt;

        public void Clear()
        {
            switch (this.VarType)
            {
                case VarEnum.VT_EMPTY:
                    break;

                case VarEnum.VT_NULL:
                case VarEnum.VT_I2:
                case VarEnum.VT_I4:
                case VarEnum.VT_R4:
                case VarEnum.VT_R8:
                case VarEnum.VT_CY:
                case VarEnum.VT_DATE:
                case VarEnum.VT_ERROR:
                case VarEnum.VT_BOOL:
                //case VarEnum.VT_DECIMAL:
                case VarEnum.VT_I1:
                case VarEnum.VT_UI1:
                case VarEnum.VT_UI2:
                case VarEnum.VT_UI4:
                case VarEnum.VT_I8:
                case VarEnum.VT_UI8:
                case VarEnum.VT_INT:
                case VarEnum.VT_UINT:
                case VarEnum.VT_HRESULT:
                case VarEnum.VT_FILETIME:
                    this.vt = 0;
                    break;

                default:
                    PropVariantClear(ref this);
                    break;
            }
        }

        private string BSTRPtrToString(nint ptr)
        {
            string returnVal = Marshal.PtrToStringBSTR(ptr);
            Marshal.FreeBSTR(ptr);
            return returnVal;
        }

        private object UnknownVarEnumToObj(PropVariant strct)
        {
            GCHandle PropHandle = GCHandle.Alloc(strct, GCHandleType.Pinned);
            try
            {
                nint variant = PropHandle.AddrOfPinnedObject();
                return Marshal.GetObjectForNativeVariant<object>(variant);
            }
            finally
            {
                PropHandle.Free();
            }
        }

        public object GetObject() => this.VarType switch
        {
            VarEnum.VT_EMPTY => null,
            VarEnum.VT_FILETIME => DateTime.FromFileTime(this.longValue),
            VarEnum.VT_BSTR => BSTRPtrToString(this.pointerValue),
            VarEnum.VT_BOOL => byteValue == 0xFF,
            VarEnum.VT_UI8 => (ulong)longValue,
            VarEnum.VT_UI4 => (uint)longValue,
            VarEnum.VT_UI2 => (ushort)longValue,
            VarEnum.VT_UI1 => byteValue,
            VarEnum.VT_I8 => longValue,
            VarEnum.VT_I4 => (int)longValue,
            VarEnum.VT_I2 => (short)longValue,
            VarEnum.VT_I1 => (sbyte)byteValue,
            _ => UnknownVarEnumToObj(this)
        };
    }

    [Guid("23170F69-40C1-278A-0000-000600100000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface IArchiveOpenCallback
    {
        // ref ulong replaced with IntPtr because handlers ofter pass null value
        // read actual value with Marshal.ReadInt64
        unsafe void SetTotal(
            ulong* files, // [In] ref ulong files, can use 'ulong* files' but it is unsafe
            IntPtr bytes); // [In] ref ulong bytes

        unsafe void SetCompleted(
            ulong* files, // [In] ref ulong files
            IntPtr bytes); // [In] ref ulong bytes
    }

    [Guid("23170F69-40C1-278A-0000-000600300000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface IArchiveOpenVolumeCallback
    {
        void GetProperty(
            ItemPropId propID, // PROPID
            IntPtr value); // PROPVARIANT

        [PreserveSig]
        int GetStream(
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            [MarshalAs(UnmanagedType.Interface)] out IInStream inStream);
    }

    [Guid("23170F69-40C1-278A-0000-000300010000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface ISequentialInStream
    {
        uint Read(
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size);

        /*
        Out: if size != 0, return_value = S_OK and (*processedSize == 0),
          then there are no more bytes in stream.
        if (size > 0) && there are bytes in stream, 
        this function must read at least 1 byte.
        This function is allowed to read less than number of remaining bytes in stream.
        You must call Read function in loop, if you need exact amount of data
        */
    }

    [Guid("23170F69-40C1-278A-0000-000300020000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface ISequentialOutStream
    {
        [PreserveSig]
        int Write(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size,
            IntPtr processedSize); // ref uint processedSize
        /*
        if (size > 0) this function must write at least 1 byte.
        This function is allowed to write less than "size".
        You must call Write function in loop, if you need to write exact amount of data
        */
    }

    [Guid("23170F69-40C1-278A-0000-000300030000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface IInStream //: ISequentialInStream
    {
        uint Read(
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size);

        void Seek(
            long offset,
            uint seekOrigin,
            IntPtr newPosition); // ref long newPosition
    }

    [Guid("23170F69-40C1-278A-0000-000300040000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface IOutStream //: ISequentialOutStream
    {
        [PreserveSig]
        int Write(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size,
            IntPtr processedSize); // ref uint processedSize

        void Seek(
            long offset,
            uint seekOrigin,
            IntPtr newPosition); // ref long newPosition

        [PreserveSig]
        int SetSize(long newSize);
    }

    [Guid("23170F69-40C1-278A-0000-000600600000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedComInterface]
    internal partial interface IInArchive
    {
        [PreserveSig]
        int Open(
            IInStream stream,
            in ulong maxCheckStartPosition,
            [MarshalAs(UnmanagedType.Interface)] IArchiveOpenCallback openArchiveCallback);

        void Close();
        uint GetNumberOfItems();

        void GetProperty(
            uint index,
            ItemPropId propID, // PROPID
            ref PropVariant value); // PROPVARIANT

        // indices must be sorted 
        // numItems = 0xFFFFFFFF means all files
        // testMode != 0 means "test files operation"
        [PreserveSig]
        int Extract(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] indices, //[In] ref uint indices,
            uint numItems,
            int testMode,
            [MarshalAs(UnmanagedType.Interface)] IArchiveExtractCallback extractCallback);

        void GetArchiveProperty(
            uint propID, // PROPID
            ref PropVariant value); // PROPVARIANT

        uint GetNumberOfProperties();

        void GetPropertyInfo(
            uint index,
            [MarshalAs(UnmanagedType.BStr)] out string name,
            out ItemPropId propID, // PROPID
            out ushort varType); //VARTYPE

        uint GetNumberOfArchiveProperties();

        void GetArchivePropertyInfo(
            uint index,
            [MarshalAs(UnmanagedType.BStr)] string name,
            ref uint propID, // PROPID
            ref ushort varType); //VARTYPE
    }

    internal class StreamWrapper : IDisposable
    {
        protected Stream BaseStream;

        protected StreamWrapper(Stream baseStream)
        {
            this.BaseStream = baseStream;
        }

        ~StreamWrapper() => Dispose();

        public void Dispose() => this.BaseStream.Dispose();

        public virtual void Seek(long offset, uint seekOrigin, IntPtr newPosition)
        {
            long Position = this.BaseStream.Seek(offset, (SeekOrigin)seekOrigin);
            if (newPosition != IntPtr.Zero) Marshal.WriteInt64(newPosition, Position);
        }
    }

    [GeneratedComClass]
    internal partial class InStreamWrapper : StreamWrapper, ISequentialInStream, IInStream
    {
        public InStreamWrapper(Stream baseStream) : base(baseStream)
        {
        }

        public uint Read(byte[] data, uint size) => (uint)this.BaseStream.Read(data);
    }

    [GeneratedComClass]
    internal partial class OutStreamWrapper : StreamWrapper, ISequentialOutStream, IOutStream
    {
        private readonly CancellationToken cancellationToken;

        public OutStreamWrapper(Stream baseStream, CancellationToken cancellationToken) : base(baseStream)
        {
            this.cancellationToken = cancellationToken;
        }

        public int SetSize(long newSize)
        {
            this.BaseStream.SetLength(newSize);
            return 0;
        }

        public int Write(byte[] data, uint size, IntPtr processedSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.BaseStream.Write(data);
            if (processedSize != IntPtr.Zero) Marshal.WriteInt64(processedSize, (int)size);
            return 0;
        }
    }
}
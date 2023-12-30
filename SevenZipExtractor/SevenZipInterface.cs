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

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    internal struct PropArray
    {
        internal uint length;
        internal IntPtr pointerValues;
    }

    // Size was previously 16 bytes but due to x64 build, it should've been 24 bytes
    // This also means that we can use decimal value since it's a 16 bytes-long floating number.
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 24)]
    internal partial struct PropVariant
    {
        // Local P/Invoke
        [DllImport("ole32.dll", EntryPoint = "PropVariantClear", ExactSpelling = true, SetLastError = true)]
        private static extern unsafe int PropVariantClearInvoke(nint ptr);

        [FieldOffset(0)] internal ushort vt;
        [FieldOffset(8)] internal IntPtr pointer;
        [FieldOffset(8)] internal byte byteValue;
        [FieldOffset(8)] internal sbyte sbyteValue;
        [FieldOffset(8)] internal ushort ushortValue;
        [FieldOffset(8)] internal short shortValue;
        [FieldOffset(8)] internal uint uintValue;
        [FieldOffset(8)] internal int intValue;
        [FieldOffset(8)] internal ulong ulongValue;
        [FieldOffset(8)] internal long longValue;
        [FieldOffset(8)] internal float floatValue;
        [FieldOffset(8)] internal double doubleValue;
        [FieldOffset(8)] internal PropArray propArray;

        public VarEnum VarType => (VarEnum)this.vt;

        private unsafe string PtrToStringBSTR(nint ptr)
        {
            string returnVal = Marshal.PtrToStringBSTR(ptr);
            Marshal.FreeBSTR(ptr);
            return returnVal;
        }

        private unsafe string PtrToStringUTF8(nint ptr)
        {
            string returnVal = new string((sbyte*)ptr);
            Marshal.FreeBSTR(ptr);
            return returnVal;
        }

        internal unsafe string PtrToStringUnicode(nint ptr)
        {
            string returnVal = new string((char*)ptr);
            Marshal.FreeBSTR(ptr);
            return returnVal;
        }

#nullable enable
        internal T? GetObjectAndClear<T>()
        {
            object? obj = GetObjectAndClear();
            if (obj == null) return default;
            return (T?)obj;
        }

        internal object? GetObjectAndClear()
        {
            GCHandle gcHandle = GCHandle.Alloc(this, GCHandleType.Pinned);
            try
            {
                object? returnObject = this.VarType switch
                {
                    VarEnum.VT_EMPTY => null,
                    VarEnum.VT_FILETIME => this.longValue == 0 ? default : DateTime.FromFileTime(this.longValue),
                    VarEnum.VT_DATE => this.longValue == 0 ? default : DateTime.FromFileTime(this.longValue),
                    VarEnum.VT_BSTR => PtrToStringBSTR(this.pointer),
                    VarEnum.VT_LPSTR => PtrToStringUTF8(this.pointer),
                    VarEnum.VT_LPWSTR => PtrToStringUnicode(this.pointer),
                    VarEnum.VT_BOOL => byteValue == 0xFF,
                    VarEnum.VT_UI8 => ulongValue,
                    VarEnum.VT_UI4 => uintValue,
                    VarEnum.VT_UI2 => ushortValue,
                    VarEnum.VT_UI1 => byteValue,
                    VarEnum.VT_I8 => longValue,
                    VarEnum.VT_I4 => intValue,
                    VarEnum.VT_I2 => shortValue,
                    VarEnum.VT_I1 => sbyteValue,
                    VarEnum.VT_R4 => floatValue,
                    VarEnum.VT_R8 => doubleValue,
                    VarEnum.VT_DECIMAL => GetDecimalNumber(gcHandle.AddrOfPinnedObject()),
                    VarEnum.VT_INT => intValue,
                    VarEnum.VT_ARRAY => propArray,
                    _ => Marshal.GetObjectForNativeVariant<object>(this.pointer)
                };

                int retVal = PropVariantClearInvoke(gcHandle.AddrOfPinnedObject());
                if (retVal != 0)
                    throw new InvalidOperationException($"Error has occurred while clearing the PropVariant structure with message: {Marshal.GetLastPInvokeErrorMessage()}");

                return returnObject;
            }
            catch { throw; }
            finally { gcHandle.Free(); }
        }

        private unsafe decimal GetDecimalNumber(nint ptr)
        {
            void* unsafePtr = (void*)(ptr + 8); // Get the raw pointer of the 8 bytes forward of this struct
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(unsafePtr, 4); // Cast the pointer as a ReadOnlySpan<int>
            return new decimal(span); // Return a new decimal from the ReadOnlySpan<int>
        }
#nullable disable
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
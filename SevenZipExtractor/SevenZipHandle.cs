using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SevenZipExtractor
{
    internal sealed class SevenZipHandle : IDisposable
    {
        private IntPtr sevenZipSafeHandle;

        public SevenZipHandle(string sevenZipLibPath)
        {
            this.sevenZipSafeHandle = Kernel32Dll.LoadLibrary(sevenZipLibPath);
            if (this.sevenZipSafeHandle == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            IntPtr functionPtr = Kernel32Dll.GetProcAddress(this.sevenZipSafeHandle, "GetHandlerProperty");
            if (functionPtr == IntPtr.Zero)
            {
                Kernel32Dll.FreeLibrary(this.sevenZipSafeHandle);
                // this.sevenZipSafeHandle.Close();
                throw new ArgumentException();
            }
        }

        ~SevenZipHandle()
        {
            this.Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (this.sevenZipSafeHandle != IntPtr.Zero)
            {
                Kernel32Dll.FreeLibrary(this.sevenZipSafeHandle);
                // this.sevenZipSafeHandle.Close();
            }

            this.sevenZipSafeHandle = IntPtr.Zero;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IInArchive CreateInArchive(Guid classId)
        {
            if (this.sevenZipSafeHandle == IntPtr.Zero) throw new ObjectDisposedException("SevenZipHandle");

            IntPtr procAddress = Kernel32Dll.GetProcAddress(this.sevenZipSafeHandle, "CreateObject");
            CreateObjectDelegate createObject = (CreateObjectDelegate)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(CreateObjectDelegate));

            object result;
            Guid interfaceId = typeof(IInArchive).GUID;
            createObject(ref classId, ref interfaceId, out result);

            return (IInArchive)result;
        }
    }
}
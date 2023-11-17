using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SevenZipExtractor
{
    // This code was taken from .NET 8 DllImport's generated codes
    internal static class SevenZipHandle
    {
        private const string DLL_PATH = "Lib\\7zxa.dll";

        [DllImport(DLL_PATH, EntryPoint = "CreateObject", ExactSpelling = true)]
        private static extern unsafe int PInvoke(Guid* classID_native, Guid* interfaceID_native, void** outObject_native);

        private static unsafe int CreateObjectDelegate(ref Guid classID, ref Guid interfaceID, out IInArchive outObject)
        {
            bool invokeSucceeded = default;
            Unsafe.SkipInit(out outObject);
            void* outObject_native = default;
            int retVal = default;
            try
            {
                fixed (Guid* interfaceID_native = &interfaceID)
                fixed (Guid* classID_native = &classID)
                {
                    retVal = PInvoke(classID_native, interfaceID_native, &outObject_native);
                }

                invokeSucceeded = true;
                outObject = ComInterfaceMarshaller<IInArchive>.ConvertToManaged(outObject_native);
            }
            finally
            {
                if (invokeSucceeded)
                {
                    ComInterfaceMarshaller<IInArchive>.Free(outObject_native);
                }
            }

            return retVal;
        }

        internal static IInArchive CreateInArchive(Guid classId)
        {
            Guid interfaceId = typeof(IInArchive).GUID;
            CreateObjectDelegate(ref classId, ref interfaceId, out IInArchive result);

            return result;
        }
    }
}
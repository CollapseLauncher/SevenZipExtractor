using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SevenZipExtractor
{
    internal static class NativeMethods
    {
        private const string SEVENZIPDLL_NAME = "7z.dll";
        private const string SEVENZIPDLL_PATH = "Lib\\" + SEVENZIPDLL_NAME;
        private static string CURRENTPROC_PATH = Process.GetCurrentProcess().MainModule!.FileName;

        static NativeMethods()
        {
            // Use custom Dll import resolver
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
        }

        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            IntPtr LoadDllInternal(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
            {
                Console.WriteLine($"[7-zip][NativeMethods::DllImportResolver] Loading library from path: {libraryName} | Search path: {(searchPath == null ? "Default" : searchPath)}");
                // Try load the library and if fails, then throw.
                bool isLoadSuccessful = NativeLibrary.TryLoad(libraryName, assembly, searchPath, out IntPtr pResult);
                if (!isLoadSuccessful || pResult == IntPtr.Zero)
                    throw new FileLoadException($"Failed while loading library from this path: {libraryName}\r\nMake sure that the library/.dll is a valid Win32 library and not corrupted!");

                // If success, then return the pointer to the library
                return pResult;
            }

            // If the library to load is "7z.dll", then try load it
            if (libraryName.EndsWith(SEVENZIPDLL_NAME, StringComparison.InvariantCultureIgnoreCase))
            {
                // Get the root directory of the module, then try get the stock .dll path
                string assemblyParentPath = Path.GetDirectoryName(CURRENTPROC_PATH);
                string sevenZipStockPath = Path.Combine(assemblyParentPath, SEVENZIPDLL_PATH);

                // If the stock .dll is not found in <collapse_install_path>\Lib\7z.dll,
                // then try fallback to the one installed in the system (if exist)
                if (!File.Exists(sevenZipStockPath))
                {
                    // Try fallback to the official 7-Zip's .dll
                    if (File.Exists(sevenZipStockPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", SEVENZIPDLL_NAME))
                    // If not found, try fallback to the ZStandard 7-Zip's .dll
                     || File.Exists(sevenZipStockPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip-Zstandard", SEVENZIPDLL_NAME))
                    // If those two do not exist, then try fallback to the .dll in the <collapse_install_path>\7z.dll
                     || File.Exists(sevenZipStockPath = Path.Combine(assemblyParentPath, SEVENZIPDLL_NAME)))
                        return LoadDllInternal(sevenZipStockPath, assembly, null);

                    // If all fails, throw
                    throw new DllNotFoundException($"DLL file {SEVENZIPDLL_PATH} is not found!");
                }

                // Load the stock 7z.dll library
                return LoadDllInternal(sevenZipStockPath, assembly, null);
            }

            // Load other library
            return LoadDllInternal(libraryName, assembly, searchPath);
        }

        [DllImport(SEVENZIPDLL_PATH, EntryPoint = "CreateObject", ExactSpelling = true)]
        internal static extern unsafe int CreateObjectDelegate(Guid* classID_native, Guid* interfaceID_native, void** outObject_native);

        [DllImport("ole32.dll", EntryPoint = "PropVariantClear", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern int PropVariantClearInvoke(nint ptr);
    }

    // This code was taken from .NET 8 DllImport's generated codes
    internal static class SevenZipHandle
    {
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
                    retVal = NativeMethods.CreateObjectDelegate(classID_native, interfaceID_native, &outObject_native);
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
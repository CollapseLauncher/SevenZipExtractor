using SevenZipExtractor.Interface;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SevenZipExtractor.Unmanaged
{
    internal static partial class NativeMethods
    {
        private const           string SevenZipMainDllName  = "7z.dll";
        private const           string SevenZipMainDllPath  = "Lib\\" + SevenZipMainDllName;
        private const           string SevenZipMiniDllName  = "7za.dll";
        private const           string SevenZipMiniDllPath  = "Lib\\" + SevenZipMiniDllName;
        private const           string SevenZipEMiniDllName = "7zxa.dll";
        private const           string SevenZipEMiniDllPath = "Lib\\" + SevenZipEMiniDllName;
        private static readonly string CurrentProcessPath   = Process.GetCurrentProcess().MainModule!.FileName;

        static NativeMethods()
        {
            // Use custom Dll import resolver
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
        }

        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            // If the library to load is not "7z.dll", then try load it as other library
            if (!libraryName.EndsWith(SevenZipMainDllName, StringComparison.OrdinalIgnoreCase))
            {
                // Load other library
                return LoadDllInternal(libraryName, assembly, searchPath, true);
            }

            // Try load 7zxa.dll first
            IntPtr dllLoadPtr = TryLoadRedirectedDll(SevenZipEMiniDllName, SevenZipEMiniDllPath, assembly, false);

            // If 7zxa.dll is not found, then fallback to 7za.dll
            if (dllLoadPtr == IntPtr.Zero)
            {
                dllLoadPtr = TryLoadRedirectedDll(SevenZipMiniDllName, SevenZipMiniDllPath, assembly, false);
            }

            // If 7za.dll is not found, then fallback to 7z.dll
            if (dllLoadPtr == IntPtr.Zero)
            {
                dllLoadPtr = TryLoadRedirectedDll(SevenZipMainDllName, SevenZipMainDllPath, assembly);
            }

            // If it returns non zero, return pointer.
            if (dllLoadPtr != IntPtr.Zero)
            {
                return dllLoadPtr;
            }

            // Otherwise, throw as it's not found.
            throw new DllNotFoundException("Cannot find or load either 7zxa.dll or 7za.dll or 7z.dll from stock \"Lib\" folder or \"Program Files\" on your machine!");
        }

        private static IntPtr TryLoadRedirectedDll(string dllFileName, string dllFilePath, Assembly assembly, bool throwIfFail = true)
        {
            // Get the root directory of the module, then try to get the stock .dll path
            string? assemblyParentPath = Path.GetDirectoryName(CurrentProcessPath);
            string sevenZipStockPath  = Path.Combine(assemblyParentPath ?? "", dllFilePath);

            // Load stock 7-zip dll if exist
            if (File.Exists(sevenZipStockPath))
            {
                // Load the stock <dllFileName> library
                return LoadDllInternal(sevenZipStockPath, assembly, null, throwIfFail);
            }

            // If the stock .dll is not found in <collapse_install_path>\Lib\<dllFileName>,
            // then try fallback to the one installed in the system (if exist)

            // Try fallback to the official 7-Zip's .dll
            if (File.Exists(sevenZipStockPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", dllFileName))
            // If not found, try fallback to the ZStandard 7-Zip's .dll
            || File.Exists(sevenZipStockPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip-Zstandard", dllFileName))
            // If those two do not exist, then try fallback to the .dll in the <collapse_install_path>\<dllFileName>
            || File.Exists(sevenZipStockPath = Path.Combine(assemblyParentPath ?? "", dllFileName)))
            return LoadDllInternal(sevenZipStockPath, assembly, null, throwIfFail);

            // If all fails, then return zero as fail
            return IntPtr.Zero;
        }

        private static IntPtr LoadDllInternal(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwIfFail)
        {
            Console.WriteLine($"[7-zip][NativeMethods::DllImportResolver] Loading library from path: {libraryName} | Search path: {(searchPath == null ? "Default" : searchPath)}");
            // Try load the library and if fails, then throw.
            bool isLoadSuccessful = NativeLibrary.TryLoad(libraryName, assembly, searchPath, out IntPtr pResult);
            if ((!isLoadSuccessful || pResult == IntPtr.Zero) && throwIfFail)
                throw new FileLoadException($"Failed while loading library from this path: {libraryName}\r\nMake sure that the library/.dll is a valid Win32 library and not corrupted!");

            // If success, then return the pointer to the library
            return pResult;
        }

        private static unsafe void CreateObjectDelegate(ref Guid        classID, ref Guid interfaceID,
                                                        out IInArchive? outObject)
        {
            bool invokeSucceeded = default;
            Unsafe.SkipInit(out outObject);
            void* outObjectNative = default;
            try
            {
                fixed (Guid* interfaceIDNative = &interfaceID)
                {
                    fixed (Guid* classIDNative = &classID)
                    {
                        int result = CreateObjectDelegate(classIDNative, interfaceIDNative, &outObjectNative);
                        if (result != 0)
                        {
                            Marshal.ThrowExceptionForHR(result);
                        }
                    }
                }

                invokeSucceeded = true;
                outObject       = ComInterfaceMarshaller<IInArchive>.ConvertToManaged(outObjectNative);
            }
            finally
            {
                if (invokeSucceeded)
                {
                    ComInterfaceMarshaller<IInArchive>.Free(outObjectNative);
                }
            }
        }

        internal static IInArchive? CreateInArchive(Guid classId)
        {
            Guid interfaceId = typeof(IInArchive).GUID;
            CreateObjectDelegate(ref classId, ref interfaceId, out IInArchive? result);
            return result;
        }

        [LibraryImport(SevenZipMainDllPath, EntryPoint = "CreateObject")]
        internal static unsafe partial int CreateObjectDelegate(Guid* classIDNative, Guid* interfaceIDNative, void** outObjectNative);
    }
}
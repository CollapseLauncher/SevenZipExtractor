using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SevenZipExtractor
{
    internal static partial class Kernel32Dll
    {
        [LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr LoadLibrary(string lpFileName);

        [LibraryImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial IntPtr GetProcAddress(IntPtr hModule, string procName);

        [LibraryImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool FreeLibrary(IntPtr hModule);
    }
}
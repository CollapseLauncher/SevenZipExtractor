using Microsoft.Win32.SafeHandles;
#if !NET5_0_OR_GREATER
using System.Runtime.ConstrainedExecution;
#endif

namespace SevenZipExtractor
{
    internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeLibraryHandle() : base(true)
        {
        }

        /// <summary>Release library handle</summary>
        /// <returns>true if the handle was released</returns>
#if !NET5_0_OR_GREATER
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        protected override bool ReleaseHandle()
        {
            return Kernel32Dll.FreeLibrary(this.handle);
        }
    }
}
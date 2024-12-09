using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace MPQToTACT.MPQ.Native
{
    internal static class Win32Methods
    {
        [DllImport("kernel32", ExactSpelling = false, SetLastError = true)]
        public static extern uint GetMappedFileName(
            IntPtr hProcess,
            IntPtr fileHandle,
            IntPtr lpFilename,
            uint nSize
            );

        [DllImport("kernel32", ExactSpelling = false, SetLastError = true)]
        public static extern uint GetFinalPathNameByHandle(
            IntPtr hFile,
            IntPtr lpszFilePath,
            uint cchFilePath,
            uint dwFlags
            );

        public static string GetFileNameOfMemoryMappedFile(MemoryMappedFile file)
        {
            const uint size = 522;
            var path = Marshal.AllocCoTaskMem(unchecked((int)size)); // MAX_PATH + 1 char

            string result;
            try
            {
                // constant 0x2 = VOLUME_NAME_NT
                var test = GetFinalPathNameByHandle(file.SafeMemoryMappedFileHandle.DangerousGetHandle(), path, size, 0x2);
                if (test != 0)
                    throw new Win32Exception();

                result = Marshal.PtrToStringAuto(path);
            }
            catch
            {
                var test = GetMappedFileName(Process.GetCurrentProcess().Handle, file.SafeMemoryMappedFileHandle.DangerousGetHandle(), path, size);
                if (test != 0)
                    throw new Win32Exception();

                result = Marshal.PtrToStringAuto(path);
            }

            return result;
        }
    }
}

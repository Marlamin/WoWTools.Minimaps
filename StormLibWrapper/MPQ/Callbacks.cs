using System;
using System.Runtime.InteropServices;

namespace MPQToTACT.MPQ
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void SFILE_DOWNLOAD_CALLBACK(IntPtr pvUserData, ulong byteOffset, uint dwTotalBytes);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void SFILE_COMPACT_CALLBACK(IntPtr pvUserData, uint dwWorkType, ulong bytesProcessed, ulong totalBytes);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void SFILE_ADDFILE_CALLBACK(IntPtr pvUserData, uint dwBytesWritte, uint dwTotalBytes, bool bFinalCall);
}

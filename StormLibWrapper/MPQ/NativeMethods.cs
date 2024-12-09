using System;
using System.Runtime.InteropServices;

namespace MPQToTACT.MPQ.Native
{
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
    internal static class NativeMethods
    {
        private const string STORMLIB = "stormlib.dll";

        #region Functions for manipulation with StormLib global flags
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern uint SFileGetLocale();
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern uint SFileSetLocale(uint lcNewLocale);
        #endregion

        #region Functions for archive manipulation
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileOpenArchive([MarshalAs(UnmanagedType.LPTStr)] string szMpqName, uint dwPriority, SFileOpenArchiveFlags dwFlags, out MpqArchiveSafeHandle phMpq);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileCreateArchive([MarshalAs(UnmanagedType.LPTStr)] string szMpqName, uint dwCreateFlags, uint dwMaxFileCount, out MpqArchiveSafeHandle phMpq);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileCreateArchive2([MarshalAs(UnmanagedType.LPTStr)] string szMpqName, ref SFILE_CREATE_MPQ pCreateInfo, out MpqArchiveSafeHandle phMpq);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileSetDownloadCallback(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.FunctionPtr)] SFILE_DOWNLOAD_CALLBACK pfnCallback, IntPtr pvUserData);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileFlushArchive(MpqArchiveSafeHandle hMpq);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileCloseArchive(IntPtr hMpq);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileCloseArchive(MpqArchiveSafeHandle hMpq);
        #endregion

        #region Adds another listfile into MPQ.
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern int SFileAddListFile(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szListFile);
        #endregion

        #region Archive compacting
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileSetCompactCallback(MpqArchiveSafeHandle hMpq, SFILE_COMPACT_CALLBACK compactCB, IntPtr pvUserData);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileCompactArchive(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szListFile, bool bReserved);
        #endregion

        #region Maximum file count
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern uint SFileGetMaxFileCount(MpqArchiveSafeHandle hMpq);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileSetMaxFileCount(MpqArchiveSafeHandle hMpq, uint dwMaxFileCount);
        #endregion

        #region Changing (attributes) file
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern uint SFileGetAttributes(MpqArchiveSafeHandle hMpq);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileSetAttributes(MpqArchiveSafeHandle hMpq, uint dwFlags);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileUpdateFileAttributes(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szFileName);
        #endregion

        #region Functions for manipulation with patch archives
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileOpenPatchArchive(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPWStr)] string szPatchMpqName, [MarshalAs(UnmanagedType.LPStr)] string szPatchPathPrefix, uint dwFlags);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileIsPatchedArchive(MpqArchiveSafeHandle hMpq);
        #endregion

        #region Functions for file manipulation
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileHasFile(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szFileName);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileOpenFileEx(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szFileName, uint dwSearchScope, out MpqFileSafeHandle phFile);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern uint SFileGetFileSize(MpqFileSafeHandle hFile, ref uint pdwFileSizeHigh);

        public static unsafe uint SFileGetFilePointer(MpqFileSafeHandle hFile)
        {
            if (hFile.IsInvalid || hFile.IsClosed)
                throw new InvalidOperationException();

            var handle = hFile.DangerousGetHandle();
            var header = (_TMPQFileHeader*)handle.ToPointer();
            return header->dwFilePos;
        }

        public static unsafe DateTime? SFileGetFileTime(MpqFileSafeHandle hFile)
        {
            if (hFile.IsInvalid || hFile.IsClosed)
                throw new InvalidOperationException();

            var handle = hFile.DangerousGetHandle();
            var header = (_TMPQFileHeader*)handle.ToPointer();
            var time = header->pFileEntry->FileTime;

            if (time == 0)
            {
                return null;
            }
            else
            {
                return DateTime.FromFileTimeUtc((long)time);
            }
        }

        public static unsafe string SFileGetFileHash(MpqFileSafeHandle hFile)
        {
            if (hFile.IsInvalid || hFile.IsClosed)
                throw new InvalidOperationException();

            var handle = hFile.DangerousGetHandle();
            var header = (_TMPQFileHeader*)handle.ToPointer();

            var md5 = header->pFileEntry->md5;
            var chars = new char[32];

            fixed (char* c = chars)
            {
                int b;
                for (var i = 0; i < 16; i++)
                {
                    b = md5[i] >> 4;
                    c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                    b = md5[i] & 0xF;
                    c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
                }

                return new string(c).Substring(0, 32);
            }
        }


        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern uint SFileSetFilePointer(MpqFileSafeHandle hFile, uint lFilePos, ref uint plFilePosHigh, uint dwMoveMethod);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileReadFile(MpqFileSafeHandle hFile, IntPtr lpBuffer, uint dwToRead, out uint pdwRead, ref System.Threading.NativeOverlapped lpOverlapped);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileCloseFile(IntPtr hFile);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileCloseFile(MpqFileSafeHandle hFile);

        #region Retrieving info about a file in the archive
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileGetFileInfo(IntPtr hMpqOrFile, SFileInfoClass InfoClass, IntPtr pvFileInfo, uint cbFileInfoSize, out uint pcbLengthNeeded);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileGetFileInfo(MpqArchiveSafeHandle hMpqOrFile, SFileInfoClass InfoClass, IntPtr pvFileInfo, uint cbFileInfoSize, out uint pcbLengthNeeded);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileGetFileInfo(MpqFileSafeHandle hMpqOrFile, SFileInfoClass InfoClass, IntPtr pvFileInfo, uint cbFileInfoSize, out uint pcbLengthNeeded);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileGetFileName(MpqFileSafeHandle hFile, [MarshalAs(UnmanagedType.LPStr)] out string szFileName);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileFreeFileInfo(IntPtr pvFileInfo, SFileInfoClass infoClass);
        #endregion

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileExtractFile(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szToExtract, [MarshalAs(UnmanagedType.LPTStr)] string szExtracted, uint dwSearchScope);

        #endregion

        #region Functions for file and archive verification
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileGetFileChecksums(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szFileName, out uint pdwCrc32, IntPtr pMD5);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern uint SFileVerifyFile(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szFileName, uint dwFlags);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern int SFileVerifyRawData(MpqArchiveSafeHandle hMpq, uint dwWhatToVerify, [MarshalAs(UnmanagedType.LPStr)] string szFileName);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern uint SFileVerifyArchive(MpqArchiveSafeHandle hMpq);
        #endregion

        #region Functions for file searching
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern IntPtr SFileFindFirstFile(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szMask, out _SFILE_FIND_DATA lpFindFileData, [MarshalAs(UnmanagedType.LPStr)] string szListFile);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileFindNextFile(IntPtr hFind, [In, Out] ref _SFILE_FIND_DATA lpFindFileData);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileFindClose(IntPtr hFind);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern IntPtr SListFileFindFirstFile(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szListFile, [MarshalAs(UnmanagedType.LPStr)] string szMask, [In, Out] ref _SFILE_FIND_DATA lpFindFileData);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SListFileFindNextFile(IntPtr hFind, [In, Out] ref _SFILE_FIND_DATA lpFindFileData);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SListFileFindClose(IntPtr hFind);
        #endregion

        #region Locale support
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern int SFileEnumLocales(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szFileName, IntPtr plcLocales, ref uint pdwMaxLocales, uint dwSearchScope);
        #endregion

        #region Support for adding files to the MPQ
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileCreateFile(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szArchiveName, ulong fileTime, uint dwFileSize, uint lcLocale, uint dwFlags, out IntPtr phFile);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileWriteFile(MpqFileSafeHandle hFile, IntPtr pvData, uint dwSize, uint dwCompression);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileFinishFile(MpqFileSafeHandle hFile);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileAddFileEx(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPTStr)] string szFileName, [MarshalAs(UnmanagedType.LPStr)] string szArchivedName, uint dwFlags, uint dwCompression, uint dwCompressionNext);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileAddFile(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPTStr)] string szFileName, [MarshalAs(UnmanagedType.LPStr)] string szArchivedName, uint dwFlags);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileAddWave(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPTStr)] string szFileName, [MarshalAs(UnmanagedType.LPStr)] string szArchivedName, uint dwFlags, uint dwQuality);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileRemoveFile(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szFileName, uint dwSearchScope);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileRenameFile(MpqArchiveSafeHandle hMpq, [MarshalAs(UnmanagedType.LPStr)] string szOldFileName, [MarshalAs(UnmanagedType.LPStr)] string szNewFileName);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileSetFileLocale(MpqFileSafeHandle hFile, uint lcNewLocale);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileSetDataCompression(uint DataCompression);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern bool SFileSetAddFileCallback(MpqArchiveSafeHandle hMpq, SFILE_ADDFILE_CALLBACK AddFileCB, IntPtr pvUserData);
        #endregion

        #region Compression and decompression
        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern int SCompImplode(IntPtr pvOutBuffer, ref int pcbOutBuffer, IntPtr pvInBuffer, int cbInBuffer);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern int SCompExplode(IntPtr pvOutBuffer, ref int pcbOutBuffer, IntPtr pvInBuffer, int cbInBuffer);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern int SCompCompress(IntPtr pvOutBuffer, ref int pcbOutBuffer, IntPtr pvInBuffer, int cbInBuffer, uint uCompressionMask, int nCmpType, int nCmpLevel);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern int SCompDecompress(IntPtr pvOutBuffer, ref int pcbOutBuffer, IntPtr pvInBuffer, int cbInBuffer);

        [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
        public static extern int SCompDecompress2(IntPtr pvOutBuffer, ref int pcbOutBuffer, IntPtr pvInBuffer, int cbInBuffer);


        #endregion
    }

#pragma warning disable 0169, 0649, IDE0044, IDE0051
    internal struct SFILE_CREATE_MPQ
    {
        public uint cbSize;
        public uint dwMpqVersion;
        private IntPtr pvUserData;
        private uint cbUserData;
        public uint dwStreamFlags;
        public uint dwFileFlags1;
        public uint dwFileFlags2;
        public uint dwAttrFlags;
        public uint dwSectorSize;
        public uint dwRawChunkSize;
        public uint dwMaxFileCount;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public unsafe struct _SFILE_FIND_DATA
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        public IntPtr szPlainName;
        public uint dwHashIndex;                          // Hash table index for the file
        public uint dwBlockIndex;                          // Block table index for the file
        public uint dwFileSize;                            // File size in bytes
        public uint dwFileFlags;                           // MPQ file flags
        public uint dwCompSize;                            // Compressed file size
        public uint dwFileTimeLo;                          // Low 32-bits of the file time (0 if not present)
        public uint dwFileTimeHi;                          // High 32-bits of the file time (0 if not present)
        public uint lcLocale;                              // Locale version
    }

    public unsafe struct _TFileEntry
    {
        public ulong FileNameHash;
        public ulong ByteOffset;
        public ulong FileTime;
        public uint dwFileSize;
        public uint dwCmpSize;
        public uint dwFlags;
        public uint dwCrc32;
        public fixed byte md5[16];
        public IntPtr szFileName;
    }

    // Provides enough of _TMPQFile to get to the file size and current position.
    internal unsafe struct _TMPQFileHeader
    {
        public IntPtr pStream;
        public IntPtr ha;
        public IntPtr pHashEntry;
        public _TFileEntry* pFileEntry;
        public ulong rawFilePos;
        public ulong mpqFilePos;
        public uint dwHashIndex;
        public uint dwFileKey;
        public uint dwFilePos;
        public uint dwMagic;
    }
#pragma warning restore 0169, 0649, IDE0044

}

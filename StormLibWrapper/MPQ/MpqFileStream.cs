using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using MPQToTACT.Helpers;
using MPQToTACT.MPQ.Native;

namespace MPQToTACT.MPQ
{
    public class MpqFileStream : Stream
    {
        private MpqFileSafeHandle _handle;
        private readonly FileAccess _accessType;
        private MpqArchive _owner;
        public readonly uint _flags;

        public readonly string FileName;
        public MPQFileAttributes Flags => (MPQFileAttributes)_flags;

        internal unsafe MpqFileStream(MpqFileSafeHandle handle, FileAccess accessType, MpqArchive owner)
        {
            var header = (_TMPQFileHeader*)handle.DangerousGetHandle().ToPointer();

            FileName = Marshal.PtrToStringAnsi(header->pFileEntry->szFileName).WoWNormalise();

            _handle = handle;
            _accessType = accessType;
            _owner = owner;
            _flags = header->pFileEntry->dwFlags;
        }

        private void VerifyHandle()
        {
            if (!IsVerifiedHandle())
                throw new ObjectDisposedException("MpqFileStream");
        }

        private bool IsVerifiedHandle()
        {
            return !(_handle == null || _handle.IsInvalid || _handle.IsClosed);
        }

        public override bool CanRead => IsVerifiedHandle();

        public override bool CanSeek => IsVerifiedHandle();

        public override bool CanWrite => false;

        public override void Flush()
        {
            VerifyHandle();

            _owner.Flush();
        }

        public override long Length
        {
            get
            {
                if (IsVerifiedHandle())
                {
                    uint high = 0;
                    var low = NativeMethods.SFileGetFileSize(_handle, ref high);

                    ulong val = (high << 32) | low;
                    return unchecked((long)val);
                }

                return 0;
            }
        }

        public override long Position
        {
            get
            {
                VerifyHandle();
                return NativeMethods.SFileGetFilePointer(_handle);
            }
            set => Seek(value, SeekOrigin.Begin);
        }

        public DateTime? CreatedDate
        {
            get => IsVerifiedHandle() ? NativeMethods.SFileGetFileTime(_handle) : null;
        }

        public override unsafe int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset > buffer.Length || (offset + count) > buffer.Length)
                throw new ArgumentException("offset > buffer.Length");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            VerifyHandle();

            bool success;
            uint read;
            fixed (byte* pb = &buffer[offset])
            {
                NativeOverlapped overlapped = default;
                success = NativeMethods.SFileReadFile(_handle, new IntPtr(pb), unchecked((uint)count), out read, ref overlapped);
            }

            // StormLib fails bounds checks
            if (!success && Position != Length)
                throw new Exception("Unable to read file");

            return unchecked((int)read);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            VerifyHandle();

            var low = unchecked((uint)(offset & 0xffffffffu));
            var high = unchecked((uint)(offset >> 32));
            return NativeMethods.SFileSetFilePointer(_handle, low, ref high, (uint)origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override unsafe void Write(byte[] buffer, int offset, int count)
        {
            VerifyHandle();

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset > buffer.Length || (offset + count) > buffer.Length)
                throw new ArgumentException("offset > buffer.Length");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            VerifyHandle();

            bool success;
            fixed (byte* pb = &buffer[offset])
            {
                success = NativeMethods.SFileWriteFile(_handle, new IntPtr(pb), unchecked((uint)count), 0u);
            }

            if (!success)
                throw new Win32Exception();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (_handle != null && !_handle.IsInvalid)
                {
                    _handle.Close();
                    _handle = null;
                }

                if (_owner != null)
                {
                    _owner.RemoveOwnedFile(this);
                    _owner = null;
                }
            }
        }

        public string GetMD5Hash()
        {
            if (IsVerifiedHandle())
            {
                return NativeMethods.SFileGetFileHash(_handle);
            }
            else
            {
                return null;
            }
        }
    }

    [Flags]
    public enum MPQFileAttributes : uint
    {
        /// <summary>
        /// File is compressed using PKWARE Data compression library
        /// </summary>
        Implode = 0x00000100,
        /// <summary>
        /// File is compressed using combination of compression methods
        /// </summary>
        Compress = 0x00000200,
        /// <summary>
        /// The file is encrypted
        /// </summary>
        Encrypted = 0x00010000,
        /// <summary>
        /// The decryption key for the file is altered according to the position of the file in the archive
        /// </summary>
        FixKey = 0x00020000,
        /// <summary>
        /// The file contains incremental patch for an existing file in base MPQ
        /// </summary>
        PatchFile = 0x00100000,
        /// <summary>
        /// Instead of being divided to 0x1000-bytes blocks, the file is stored as single unit
        /// </summary>
        SingleUnit = 0x01000000,
        /// <summary>
        /// File is a deletion marker, indicating that the file no longer exists.
        /// This is used to allow patch archives to delete files present in lower-priority archives in the search chain.
        /// The file usually has length of 0 or 1 byte and its name is a hash
        /// </summary>
        DeleteMarker = 0x02000000,
        /// <summary>
        /// File has checksums for each sector (explained in the File Data section). Ignored if file is not compressed or imploded.
        /// </summary>
        SectorCRC = 0x04000000,
        /// <summary>
        /// Set if file exists, reset when the file was deleted
        /// </summary>
        Exists = 0x80000000,
    }
}

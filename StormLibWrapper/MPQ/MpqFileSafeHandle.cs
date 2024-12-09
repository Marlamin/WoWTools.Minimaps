using System;
using Microsoft.Win32.SafeHandles;

namespace MPQToTACT.MPQ.Native
{
    internal sealed class MpqFileSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public MpqFileSafeHandle(IntPtr handle)
            : base(true)
        {
            this.SetHandle(handle);
        }

        public MpqFileSafeHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.SFileCloseFile(this.handle);
        }
    }
}

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GitHub.Runner.Listener
{
    /// <summary>
    /// Makes a Linux JIT listener non-dumpable before any credential files are
    /// materialized. This is required for same-UID /proc isolation; Yama and a
    /// hidepid=ptraceable procfs mount alone do not protect every proc entry.
    /// </summary>
    internal static class LinuxProcessDumpProtection
    {
        internal delegate int PrctlInvoker(int option, ulong argument2, ulong argument3, ulong argument4, ulong argument5);

        internal const int PrGetDumpable = 3;
        internal const int PrSetDumpable = 4;

        [DllImport("libc", SetLastError = true)]
        private static extern int prctl(int option, ulong argument2, ulong argument3, ulong argument4, ulong argument5);

        internal static void DisableForJit()
        {
            DisableForJit(OperatingSystem.IsLinux(), prctl, () => Marshal.GetLastWin32Error());
        }

        internal static void DisableForJit(bool isLinux, PrctlInvoker invoke, Func<int> getLastError)
        {
            if (!isLinux)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(invoke);
            ArgumentNullException.ThrowIfNull(getLastError);
            if (invoke(PrSetDumpable, 0, 0, 0, 0) != 0)
            {
                throw new Win32Exception(getLastError(), "Unable to disable Linux process dumpability for the JIT listener.");
            }

            int dumpable = invoke(PrGetDumpable, 0, 0, 0, 0);
            if (dumpable != 0)
            {
                throw new InvalidOperationException($"Linux JIT listener dumpability verification failed with value {dumpable}.");
            }
        }
    }
}

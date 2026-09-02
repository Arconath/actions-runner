using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace GitHub.Runner.Listener
{
    /// <summary>
    /// Consumes the Arconath one-shot JIT configuration without following links and
    /// unlinks the opened inode before its contents are materialized in memory.
    /// </summary>
    internal static class JitConfigurationFile
    {
        internal const int MaximumBytes = 1024 * 1024;
        internal const string ProductionRoot = "/var/lib/arconath-runner/jobs";

        private const int AtEmptyPath = 0x1000;
        private const int OPath = 0x200000;
        private const int ODirectory = 0x10000;
        private const int OCloseOnExec = 0x80000;
        private const int ONoFollow = 0x20000;
        private const int ONonBlock = 0x800;
        private const int StatxBasicStats = 0x7ff;
        private const int FSetLease = 1024;
        private const int FReadLock = 0;
        private const ushort FileTypeMask = 0xf000;
        private const ushort RegularFile = 0x8000;
        private const ushort Directory = 0x4000;
        private const ushort PermissionAndSpecialBits = 0x0fff;
        private const ushort OwnerReadWrite = 0x0180;
        private const ushort OwnerAll = 0x01c0;
        private const ushort GroupReadExecute = 0x0028;
        private const ushort OtherExecute = 0x0001;
        private const ulong ResolveNoMagicLinks = 0x02;
        private const ulong ResolveNoSymlinks = 0x04;
        private const ulong ResolveBeneath = 0x08;
        private const long SysOpenAt2X64 = 437;

        [StructLayout(LayoutKind.Sequential)]
        private struct OpenHow
        {
            internal ulong Flags;
            internal ulong Mode;
            internal ulong Resolve;
        }

        // Linux statx is a stable 256-byte ABI. Only fields through Size are read.
        [StructLayout(LayoutKind.Sequential, Size = 256)]
        private struct Statx
        {
            internal uint Mask;
            internal uint BlockSize;
            internal ulong Attributes;
            internal uint LinkCount;
            internal uint UserId;
            internal uint GroupId;
            internal ushort Mode;
            internal ushort Spare;
            internal ulong Inode;
            internal ulong Size;
            internal ulong Blocks;
            internal ulong AttributesMask;
            internal StatxTimestamp AccessTime;
            internal StatxTimestamp BirthTime;
            internal StatxTimestamp ChangeTime;
            internal StatxTimestamp ModificationTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct StatxTimestamp
        {
            internal long Seconds;
            internal uint Nanoseconds;
            internal int Reserved;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int open(string pathname, int flags);

        [DllImport("libc", SetLastError = true, EntryPoint = "syscall")]
        private static extern long OpenAt2(long number, int directory, string pathname, ref OpenHow how, UIntPtr size);

        [DllImport("libc", SetLastError = true)]
        private static extern int statx(int directory, string pathname, int flags, uint mask, out Statx stat);

        [DllImport("libc", SetLastError = true)]
        private static extern int unlinkat(int directory, string pathname, int flags);

        [DllImport("libc", SetLastError = true)]
        private static extern int fcntl(int descriptor, int command, int argument);

        [DllImport("libc")]
        private static extern uint geteuid();

        internal static string ReadAndDelete(string path) => ReadAndDelete(path, ProductionRoot);

        internal static string ReadAndDelete(string path, string trustedRoot, Action openedFileForTest = null)
        {
            if (!OperatingSystem.IsLinux())
            {
                throw new PlatformNotSupportedException("JIT configuration files require the pinned Linux runner layout.");
            }

            ArgumentException.ThrowIfNullOrEmpty(path);
            ArgumentException.ThrowIfNullOrEmpty(trustedRoot);
            string canonicalRoot = Path.GetFullPath(trustedRoot).TrimEnd(Path.DirectorySeparatorChar);
            string fullPath = Path.GetFullPath(path);
            string jobDirectory = Path.GetDirectoryName(fullPath);
            string jobName = Path.GetFileName(jobDirectory);
            if (!string.Equals(Path.GetFileName(fullPath), "jitconfig", StringComparison.Ordinal) ||
                !string.Equals(Path.GetDirectoryName(jobDirectory), canonicalRoot, StringComparison.Ordinal) ||
                !IsJobDirectoryName(jobName))
            {
                throw new InvalidOperationException("JIT configuration is outside the trusted one-job runner root.");
            }

            int rootDescriptor = open(canonicalRoot, OPath | ODirectory | OCloseOnExec | ONoFollow);
            if (rootDescriptor < 0)
            {
                ThrowLastError("open trusted JIT root");
            }

            try
            {
                Statx rootStatus = GetStatus(rootDescriptor);
                if ((rootStatus.Mode & FileTypeMask) != Directory)
                {
                    throw new InvalidOperationException("Trusted JIT root is not a directory.");
                }

                using SafeFileHandle jobHandle = OpenBeneath(rootDescriptor, jobName, OPath | ODirectory | OCloseOnExec);
                Statx jobStatus = GetStatus(jobHandle.DangerousGetHandle().ToInt32());
                ushort jobPermissions = (ushort)(jobStatus.Mode & PermissionAndSpecialBits);
                if ((jobStatus.Mode & FileTypeMask) != Directory ||
                    jobStatus.UserId != geteuid() ||
                    (jobPermissions != OwnerAll &&
                     jobPermissions != (OwnerAll | GroupReadExecute) &&
                     jobPermissions != (OwnerAll | GroupReadExecute | OtherExecute)))
                {
                    throw new InvalidOperationException("JIT job directory must be listener-owned and mode 0700, 0750, or 0751.");
                }

                using SafeFileHandle configHandle = OpenBeneath(
                    jobHandle.DangerousGetHandle().ToInt32(),
                    "jitconfig",
                    OCloseOnExec | ONoFollow | ONonBlock);
                int configDescriptor = configHandle.DangerousGetHandle().ToInt32();
                if (fcntl(configDescriptor, FSetLease, FReadLock) != 0)
                {
                    ThrowLastError("seal JIT configuration against concurrent writers");
                }
                Statx before = GetStatus(configDescriptor);
                if ((before.Mode & FileTypeMask) != RegularFile ||
                    before.UserId != geteuid() ||
                    (before.Mode & PermissionAndSpecialBits) != OwnerReadWrite ||
                    before.LinkCount != 1 ||
                    before.Size == 0 ||
                    before.Size > MaximumBytes)
                {
                    throw new InvalidOperationException("JIT configuration must be a private, single-link regular file no larger than 1 MiB.");
                }

                openedFileForTest?.Invoke();
                if (unlinkat(jobHandle.DangerousGetHandle().ToInt32(), "jitconfig", 0) != 0)
                {
                    ThrowLastError("unlink JIT configuration");
                }
                Statx unlinked = GetStatus(configDescriptor);
                if (unlinked.Inode != before.Inode ||
                    unlinked.LinkCount != 0 ||
                    unlinked.UserId != before.UserId ||
                    unlinked.GroupId != before.GroupId ||
                    unlinked.Mode != before.Mode ||
                    unlinked.Size != before.Size)
                {
                    throw new InvalidOperationException("Opened JIT configuration inode was not sealed by unlink.");
                }

                using FileStream stream = new(configHandle, FileAccess.Read, bufferSize: 4096, isAsync: false);
                using MemoryStream content = new(capacity: checked((int)before.Size));
                byte[] buffer = new byte[8192];
                while (content.Length <= MaximumBytes)
                {
                    int read = stream.Read(buffer, 0, Math.Min(buffer.Length, MaximumBytes + 1 - checked((int)content.Length)));
                    if (read == 0)
                    {
                        break;
                    }
                    content.Write(buffer, 0, read);
                }

                Statx after = GetStatus(configDescriptor);
                if (content.Length == 0 ||
                    content.Length > MaximumBytes ||
                    after.Size != unlinked.Size ||
                    after.Size != (ulong)content.Length ||
                    after.UserId != unlinked.UserId ||
                    after.GroupId != unlinked.GroupId ||
                    after.Mode != unlinked.Mode ||
                    after.ChangeTime.Seconds != unlinked.ChangeTime.Seconds ||
                    after.ChangeTime.Nanoseconds != unlinked.ChangeTime.Nanoseconds ||
                    after.ModificationTime.Seconds != unlinked.ModificationTime.Seconds ||
                    after.ModificationTime.Nanoseconds != unlinked.ModificationTime.Nanoseconds)
                {
                    throw new InvalidOperationException("JIT configuration changed while it was being consumed.");
                }
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                    .GetString(content.GetBuffer(), 0, checked((int)content.Length));
            }
            finally
            {
                new SafeFileHandle(new IntPtr(rootDescriptor), ownsHandle: true).Dispose();
            }
        }

        private static bool IsJobDirectoryName(string value)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("job.", StringComparison.Ordinal) || value.Length == 4)
            {
                return false;
            }
            for (int index = 4; index < value.Length; index++)
            {
                char character = value[index];
                if (!char.IsAsciiLetterOrDigit(character))
                {
                    return false;
                }
            }
            return true;
        }

        private static SafeFileHandle OpenBeneath(int directory, string path, int flags)
        {
            OpenHow how = new()
            {
                Flags = checked((ulong)flags),
                Resolve = ResolveBeneath | ResolveNoMagicLinks | ResolveNoSymlinks,
            };
            long descriptor = OpenAt2(SysOpenAt2X64, directory, path, ref how, (UIntPtr)Marshal.SizeOf<OpenHow>());
            if (descriptor < 0)
            {
                ThrowLastError("open trusted JIT path");
            }
            return new SafeFileHandle(new IntPtr(descriptor), ownsHandle: true);
        }

        private static Statx GetStatus(int descriptor)
        {
            if (statx(descriptor, string.Empty, AtEmptyPath, StatxBasicStats, out Statx status) != 0)
            {
                ThrowLastError("inspect opened JIT path");
            }
            return status;
        }

        private static void ThrowLastError(string operation)
        {
            throw new IOException($"Unable to {operation}: errno {Marshal.GetLastWin32Error()}.");
        }
    }
}

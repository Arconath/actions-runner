using System;
using System.IO;
using System.Runtime.Versioning;
using GitHub.Runner.Listener;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    [SupportedOSPlatform("linux")]
    public sealed class JitConfigurationFileL0 : IDisposable
    {
        private readonly string _root;

        public JitConfigurationFileL0()
        {
            _root = Path.Combine(Path.GetTempPath(), $"arconath-jit-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public void ReadsAndUnlinksPrivateOneShotFile()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }
            string path = CreateConfig("payload");

            Assert.Equal("payload", JitConfigurationFile.ReadAndDelete(path, _root));
            Assert.False(File.Exists(path));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public void RejectsDirectAndParentSymlinks()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }
            string outside = Path.Combine(_root, "outside");
            Directory.CreateDirectory(outside);
            string outsideFile = Path.Combine(outside, "jitconfig");
            File.WriteAllText(outsideFile, "secret");
            File.SetUnixFileMode(outsideFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);

            string directJob = CreateJobDirectory("direct");
            File.CreateSymbolicLink(Path.Combine(directJob, "jitconfig"), outsideFile);
            Assert.ThrowsAny<Exception>(() => JitConfigurationFile.ReadAndDelete(Path.Combine(directJob, "jitconfig"), _root));

            string parentLink = Path.Combine(_root, "job.parent");
            Directory.CreateSymbolicLink(parentLink, outside);
            Assert.ThrowsAny<Exception>(() => JitConfigurationFile.ReadAndDelete(Path.Combine(parentLink, "jitconfig"), _root));
            Assert.True(File.Exists(outsideFile));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public void RejectsHardlinksAndOversizeFiles()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }
            string original = CreateConfig("hardlink", "hard");
            string other = Path.Combine(_root, "copy");
            Assert.Equal(0, NativeMethods.link(original, other));
            Assert.Throws<InvalidOperationException>(() => JitConfigurationFile.ReadAndDelete(original, _root));

            string oversized = CreateConfig(new string('x', JitConfigurationFile.MaximumBytes + 1), "large");
            Assert.Throws<InvalidOperationException>(() => JitConfigurationFile.ReadAndDelete(oversized, _root));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public void RejectsFifoAndWrongPermissions()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }
            string fifoJob = CreateJobDirectory("fifo");
            string fifo = Path.Combine(fifoJob, "jitconfig");
            Assert.Equal(0, NativeMethods.mkfifo(fifo, Convert.ToUInt32("600", 8)));
            Assert.ThrowsAny<Exception>(() => JitConfigurationFile.ReadAndDelete(fifo, _root));

            string permissive = CreateConfig("secret", "mode");
            File.SetUnixFileMode(permissive, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
            Assert.Throws<InvalidOperationException>(() => JitConfigurationFile.ReadAndDelete(permissive, _root));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public void RejectsConcurrentWriterAndReplacementRaces()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }
            string sameLength = CreateConfig("initial", "sameLength");
            using (FileStream writer = new(sameLength, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                writer.Write("changed"u8);
                writer.Flush(flushToDisk: true);
                Assert.ThrowsAny<IOException>(() => JitConfigurationFile.ReadAndDelete(sameLength, _root));
            }

            string truncateRegrow = CreateConfig("original", "truncateRegrow");
            using (FileStream writer = new(truncateRegrow, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                writer.SetLength(0);
                writer.Write("regrown!"u8);
                writer.Flush(flushToDisk: true);
                Assert.ThrowsAny<IOException>(() => JitConfigurationFile.ReadAndDelete(truncateRegrow, _root));
            }

            string raced = CreateConfig("original", "race");
            string moved = raced + ".moved";
            Assert.Throws<InvalidOperationException>(() => JitConfigurationFile.ReadAndDelete(
                raced,
                _root,
                () =>
                {
                    File.Move(raced, moved);
                    File.WriteAllText(raced, "replacement");
                    File.SetUnixFileMode(raced, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }));
            Assert.True(File.Exists(moved));
        }

        private string CreateConfig(string content, string suffix = "valid")
        {
            string job = CreateJobDirectory(suffix);
            string path = Path.Combine(job, "jitconfig");
            File.WriteAllText(path, content);
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            return path;
        }

        private string CreateJobDirectory(string suffix)
        {
            string job = Path.Combine(_root, $"job.{suffix}");
            Directory.CreateDirectory(job);
            File.SetUnixFileMode(job, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            return job;
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("libc", SetLastError = true)]
            internal static extern int mkfifo(string pathname, uint mode);

            [System.Runtime.InteropServices.DllImport("libc", SetLastError = true)]
            internal static extern int link(string oldpath, string newpath);
        }
    }
}

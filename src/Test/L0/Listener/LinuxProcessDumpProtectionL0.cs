using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.Runner.Listener;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class LinuxProcessDumpProtectionL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public void DisablesAndVerifiesDumpability()
        {
            List<(int Option, ulong Argument2)> calls = new();
            int Invoke(int option, ulong argument2, ulong argument3, ulong argument4, ulong argument5)
            {
                calls.Add((option, argument2));
                return 0;
            }

            LinuxProcessDumpProtection.DisableForJit(true, Invoke, () => 0);

            Assert.Equal(
                new[]
                {
                    (LinuxProcessDumpProtection.PrSetDumpable, 0UL),
                    (LinuxProcessDumpProtection.PrGetDumpable, 0UL),
                },
                calls);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public void FailsClosedWhenSetDumpableFails()
        {
            Win32Exception error = Assert.Throws<Win32Exception>(() =>
                LinuxProcessDumpProtection.DisableForJit(
                    true,
                    (option, argument2, argument3, argument4, argument5) => -1,
                    () => 1));

            Assert.Contains("disable Linux process dumpability", error.Message);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public void FailsClosedWhenDumpabilityRemainsEnabled()
        {
            int call = 0;
            Assert.Throws<InvalidOperationException>(() =>
                LinuxProcessDumpProtection.DisableForJit(
                    true,
                    (option, argument2, argument3, argument4, argument5) => ++call == 1 ? 0 : 1,
                    () => 0));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public void NonLinuxExecutionDoesNotInvokePrctl()
        {
            LinuxProcessDumpProtection.DisableForJit(
                false,
                (option, argument2, argument3, argument4, argument5) =>
                    throw new InvalidOperationException("prctl must not run outside Linux"),
                () => 0);
        }
    }
}

#if OS_LINUX || OS_OSX
using System.Security.Cryptography;
using GitHub.Runner.Listener.Configuration;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener.Configuration
{
    public class RSAFileKeyManagerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public void CachedKeyRemainsUsableAfterCredentialFileDeletion()
        {
            using TestHostContext tc = new(this);
            tc.EnqueueInstance<IProcessInvoker>(new ProcessInvokerWrapper());
            var keyManager = new RSAFileKeyManager();
            ((IRunnerService)keyManager).Initialize(tc);

            using RSA created = keyManager.CreateKey();
            byte[] payload = new byte[] { 1, 2, 3, 4 };
            byte[] signature = created.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            keyManager.DeleteKey();

            using RSA cached = keyManager.GetKey();
            Assert.True(cached.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }
    }
}
#endif

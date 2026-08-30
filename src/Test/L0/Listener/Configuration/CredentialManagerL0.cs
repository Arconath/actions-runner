using System;
using System.IO;
using System.Security.Cryptography;
using GitHub.Runner.Listener.Configuration;
using GitHub.Services.Common;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener.Configuration
{
    public class CredentialManagerL0
    {
#if OS_LINUX
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public void ActualStoreAndRsaCachesSurviveCredentialUnlinkIncludingMigration()
        {
            string root = Path.Combine(Path.GetTempPath(), $"runner-credential-seal-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            try
            {
                using TestHostContext tc = new(this) { RootDirectoryOverride = root };
                tc.EnqueueInstance<IProcessInvoker>(new ProcessInvokerWrapper());

                var store = new ConfigurationStore();
                store.Initialize(tc);
                var keyManager = new RSAFileKeyManager();
                ((IRunnerService)keyManager).Initialize(tc);
                tc.SetSingleton<IConfigurationStore>(store);
                tc.SetSingleton<IRSAKeyManager>(keyManager);
                tc.SetSingleton<IRunnerServer>(new Mock<IRunnerServer>().Object);
                tc.SetSingleton<IRunnerDotcomServer>(new Mock<IRunnerDotcomServer>().Object);

                var primary = CreateOAuthCredential("https://pipelines.actions.githubusercontent.com");
                var migrated = CreateOAuthCredential("https://broker.actions.githubusercontent.com");
                store.SaveCredential(primary);
                store.SaveMigratedCredential(migrated);
                using RSA created = keyManager.CreateKey();
                byte[] payload = { 1, 2, 3, 4 };
                byte[] signature = created.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                var configurationManager = new ConfigurationManager();
                configurationManager.Initialize(tc);
                configurationManager.DeleteLocalRunnerCredentials();

                Assert.False(File.Exists(tc.GetConfigFile(WellKnownConfigFile.Credentials)));
                Assert.False(File.Exists(tc.GetConfigFile(WellKnownConfigFile.MigratedCredentials)));
                Assert.False(File.Exists(tc.GetConfigFile(WellKnownConfigFile.RSACredentials)));

                var credentialManager = new CredentialManager();
                credentialManager.Initialize(tc);
                Assert.NotNull(credentialManager.LoadCredentials(allowAuthUrlV2: true));
                Assert.NotNull(credentialManager.LoadCredentials(allowAuthUrlV2: true));
                using RSA cached = keyManager.GetKey();
                Assert.True(cached.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }
#endif

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ConfigurationManagement")]
        public void ReloadsCachedMigratedOAuthCredentialsAfterFilesAreUnlinked()
        {
            using TestHostContext tc = new(this);
            var store = new Mock<IConfigurationStore>();
            var keyManager = new Mock<IRSAKeyManager>();
            var primary = new CredentialData { Scheme = Constants.Configuration.OAuth };
            primary.Data["clientId"] = Guid.NewGuid().ToString();
            primary.Data["authorizationUrl"] = "https://pipelines.actions.githubusercontent.com";
            var migrated = new CredentialData { Scheme = Constants.Configuration.OAuth };
            migrated.Data["clientId"] = Guid.NewGuid().ToString();
            migrated.Data["authorizationUrl"] = "https://broker.actions.githubusercontent.com";

            store.Setup(x => x.HasCredentials()).Returns(false);
            store.Setup(x => x.GetCredentials()).Returns(primary);
            store.Setup(x => x.GetMigratedCredentials()).Returns(migrated);
            keyManager.Setup(x => x.GetKey()).Returns(() => RSA.Create(2048));
            tc.SetSingleton<IConfigurationStore>(store.Object);
            tc.SetSingleton<IRSAKeyManager>(keyManager.Object);

            var manager = new CredentialManager();
            manager.Initialize(tc);

            VssCredentials credentials = manager.LoadCredentials(allowAuthUrlV2: true);

            Assert.NotNull(credentials);
            store.Verify(x => x.HasCredentials(), Times.Never);
            store.Verify(x => x.GetCredentials(), Times.Once);
            store.Verify(x => x.GetMigratedCredentials(), Times.Once);
        }

        private static CredentialData CreateOAuthCredential(string authorizationUrl)
        {
            var credential = new CredentialData { Scheme = Constants.Configuration.OAuth };
            credential.Data["clientId"] = Guid.NewGuid().ToString();
            credential.Data["authorizationUrl"] = authorizationUrl;
            return credential;
        }
    }
}

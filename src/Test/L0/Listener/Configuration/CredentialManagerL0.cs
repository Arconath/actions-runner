using System;
using GitHub.Runner.Listener.Configuration;
using GitHub.Services.Common;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener.Configuration
{
    public class CredentialManagerL0
    {
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
    }
}

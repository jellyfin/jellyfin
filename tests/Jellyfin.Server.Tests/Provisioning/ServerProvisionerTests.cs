using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Provisioning;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Provisioning;

public sealed class ServerProvisionerTests
{
    private readonly ServerConfiguration _serverConfiguration = new();
    private readonly NetworkConfiguration _networkConfiguration = new();
    private readonly Mock<IServerConfigurationManager> _configurationManager = new(MockBehavior.Loose);
    private readonly Mock<IUserManager> _userManager = new(MockBehavior.Loose);
    private readonly Mock<ILibraryManager> _libraryManager = new(MockBehavior.Loose);
    private readonly User _firstUser = new("MyJellyfinUser", "provider", "resetProvider");

    public ServerProvisionerTests()
    {
        _configurationManager.SetupGet(m => m.Configuration).Returns(_serverConfiguration);
        _configurationManager
            .Setup(m => m.GetConfiguration(NetworkConfigurationStore.StoreKey))
            .Returns(_networkConfiguration);
        _userManager.Setup(m => m.GetFirstUser()).Returns(_firstUser);
    }

    private ServerProvisioner Provisioner => new(
        _configurationManager.Object,
        _userManager.Object,
        _libraryManager.Object,
        NullLogger<ServerProvisioner>.Instance);

    [Fact]
    public async Task ProvisionAsync_AlreadySetUp_DoesNothing()
    {
        _serverConfiguration.IsStartupWizardCompleted = true;

        await Provisioner.ProvisionAsync(Manifest());

        _userManager.Verify(m => m.InitializeAsync(), Times.Never);
        _userManager.Verify(m => m.ChangePassword(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        _configurationManager.Verify(m => m.SaveConfiguration(), Times.Never);
    }

    [Fact]
    public async Task ProvisionAsync_CreatesAdministratorAndCompletesWizard()
    {
        var manifest = Manifest();
        manifest.Administrator.Name = "admin";
        manifest.Administrator.Password = "hunter2";

        await Provisioner.ProvisionAsync(manifest);

        _userManager.Verify(m => m.InitializeAsync(), Times.Once);
        _userManager.Verify(m => m.RenameUser(_firstUser.Id, "MyJellyfinUser", "admin"), Times.Once);
        _userManager.Verify(m => m.ChangePassword(_firstUser.Id, "hunter2"), Times.Once);
        Assert.True(_serverConfiguration.IsStartupWizardCompleted);
        _configurationManager.Verify(m => m.SaveConfiguration(), Times.Once);
    }

    [Fact]
    public async Task ProvisionAsync_AdministratorKeepsDefaultName_DoesNotRename()
    {
        var manifest = Manifest();
        manifest.Administrator.Name = "MyJellyfinUser";

        await Provisioner.ProvisionAsync(manifest);

        _userManager.Verify(m => m.RenameUser(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _userManager.Verify(m => m.ChangePassword(_firstUser.Id, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProvisionAsync_SetMembers_AreApplied()
    {
        var manifest = Manifest();
        manifest.ServerName = "provisioned";
        manifest.UICulture = "en-GB";
        manifest.MetadataCountryCode = "DK";
        manifest.PreferredMetadataLanguage = "da";
        manifest.EnableRemoteAccess = true;

        await Provisioner.ProvisionAsync(manifest);

        Assert.Equal("provisioned", _serverConfiguration.ServerName);
        Assert.Equal("en-GB", _serverConfiguration.UICulture);
        Assert.Equal("DK", _serverConfiguration.MetadataCountryCode);
        Assert.Equal("da", _serverConfiguration.PreferredMetadataLanguage);
        Assert.True(_networkConfiguration.EnableRemoteAccess);
        _configurationManager.Verify(m => m.SaveConfiguration(NetworkConfigurationStore.StoreKey, _networkConfiguration), Times.Once);
    }

    [Fact]
    public async Task ProvisionAsync_OmittedMembers_LeaveDefaultsAlone()
    {
        _serverConfiguration.ServerName = "existing";
        _serverConfiguration.UICulture = "en-US";
        _serverConfiguration.MetadataCountryCode = "US";
        _serverConfiguration.PreferredMetadataLanguage = "en";

        await Provisioner.ProvisionAsync(Manifest());

        Assert.Equal("existing", _serverConfiguration.ServerName);
        Assert.Equal("en-US", _serverConfiguration.UICulture);
        Assert.Equal("US", _serverConfiguration.MetadataCountryCode);
        Assert.Equal("en", _serverConfiguration.PreferredMetadataLanguage);
        _configurationManager.Verify(
            m => m.SaveConfiguration(NetworkConfigurationStore.StoreKey, It.IsAny<NetworkConfiguration>()),
            Times.Never);
    }

    [Fact]
    public async Task ProvisionAsync_Libraries_AreCreatedWithTheirPaths()
    {
        var mediaPath = Directory.CreateTempSubdirectory("provision-media").FullName;
        try
        {
            var manifest = Manifest();
            manifest.Libraries =
            [
                new ProvisionLibrary
                {
                    Name = "Movies",
                    CollectionType = CollectionTypeOptions.movies,
                    Paths = [mediaPath]
                }
            ];

            await Provisioner.ProvisionAsync(manifest);

            _libraryManager.Verify(
                m => m.AddVirtualFolder(
                    "Movies",
                    CollectionTypeOptions.movies,
                    It.Is<LibraryOptions>(o => o.PathInfos.Length == 1 && o.PathInfos[0].Path == mediaPath),
                    false),
                Times.Once);
        }
        finally
        {
            Directory.Delete(mediaPath, true);
        }
    }

    [Fact]
    public async Task ProvisionAsync_LibraryPathDoesNotExist_FailsBeforeChangingAnything()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"provision-missing-{Guid.NewGuid()}");
        var manifest = Manifest();
        manifest.Libraries = [new ProvisionLibrary { Name = "Movies", Paths = [missingPath] }];

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Provisioner.ProvisionAsync(manifest));

        Assert.Contains(missingPath, exception.Message, StringComparison.Ordinal);
        Assert.False(_serverConfiguration.IsStartupWizardCompleted);
        _userManager.Verify(m => m.InitializeAsync(), Times.Never);
        _libraryManager.Verify(
            m => m.AddVirtualFolder(It.IsAny<string>(), It.IsAny<CollectionTypeOptions?>(), It.IsAny<LibraryOptions>(), It.IsAny<bool>()),
            Times.Never);
    }

    private static ProvisionManifest Manifest() => new()
    {
        Administrator = new ProvisionAdministrator { Name = "admin", Password = "hunter2" },
        Libraries = new List<ProvisionLibrary>()
    };
}

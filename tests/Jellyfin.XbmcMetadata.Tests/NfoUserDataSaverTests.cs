using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.XbmcMetadata;
using MediaBrowser.XbmcMetadata.Savers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.XbmcMetadata.Tests;

public class NfoUserDataSaverTests
{
    [Theory]
    [InlineData(UserDataSaveReason.PlaybackFinished, true)]
    [InlineData(UserDataSaveReason.TogglePlayed, true)]
    [InlineData(UserDataSaveReason.UpdateUserRating, true)]
    [InlineData(UserDataSaveReason.UpdateUserData, true)]
    [InlineData(UserDataSaveReason.PlaybackStart, false)]
    [InlineData(UserDataSaveReason.PlaybackProgress, false)]
    [InlineData(UserDataSaveReason.Import, false)]
    public async Task UserDataSaved_SavesNfoForReasonsThatChangeWatchedState(UserDataSaveReason reason, bool expectSave)
    {
        var config = new Mock<IConfigurationManager>();
        config.Setup(x => x.GetConfiguration("xbmcmetadata"))
            .Returns(new XbmcMetadataOptions { UserId = Guid.NewGuid().ToString("N") });
        var userDataManager = new Mock<IUserDataManager>();
        var providerManager = new Mock<IProviderManager>();
        providerManager
            .Setup(x => x.SaveMetadataAsync(It.IsAny<BaseItem>(), It.IsAny<ItemUpdateType>(), It.IsAny<IEnumerable<string>>()))
            .Returns(Task.CompletedTask);
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);

        var previousMediaSourceManager = BaseItem.MediaSourceManager;
        try
        {
            BaseItem.MediaSourceManager = mediaSourceManager.Object;
            var item = new Series { Id = Guid.NewGuid(), Path = "/media/show" };
            var saver = new NfoUserDataSaver(NullLogger<NfoUserDataSaver>.Instance, config.Object, userDataManager.Object, providerManager.Object);
            await saver.StartAsync(CancellationToken.None);

            userDataManager.Raise(x => x.UserDataSaved += null, new UserDataSaveEventArgs
            {
                Item = item,
                SaveReason = reason,
                UserId = Guid.NewGuid(),
            });

            providerManager.Verify(
                x => x.SaveMetadataAsync(item, ItemUpdateType.MetadataDownload, It.Is<IEnumerable<string>>(s => Contains(s, BaseNfoSaver.SaverName))),
                expectSave ? Times.Once() : Times.Never());
        }
        finally
        {
            BaseItem.MediaSourceManager = previousMediaSourceManager;
        }
    }

    private static bool Contains(IEnumerable<string> names, string name)
    {
        foreach (var n in names)
        {
            if (n == name)
            {
                return true;
            }
        }

        return false;
    }
}

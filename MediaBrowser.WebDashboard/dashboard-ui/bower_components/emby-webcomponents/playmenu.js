define(['actionsheet', 'datetime', 'playbackManager', 'globalize', 'appSettings'], function (actionsheet, datetime, playbackManager, globalize, appSettings) {

    var isMobileApp = window.Dashboard != null;

    function show(options) {

        var item = options.item;

        var itemType = item.Type;
        var mediaType = item.MediaType;
        var isFolder = item.IsFolder;
        var itemId = item.Id;
        var channelId = item.ChannelId;
        var serverId = item.ServerId;
        var resumePositionTicks = item.UserData ? item.UserData.PlaybackPositionTicks : null;

        var showExternalPlayer = isMobileApp && mediaType == 'Video' && !isFolder && appSettings.enableExternalPlayers();
        var playableItemId = itemType == 'Program' ? channelId : itemId;

        if (!resumePositionTicks && mediaType != "Audio" && !isFolder && !showExternalPlayer) {
            playbackManager.play({
                ids: [playableItemId],
                serverId: serverId
            });
            return;
        }

        var menuItems = [];

        if (resumePositionTicks) {
            menuItems.push({
                name: globalize.translate('sharedcomponents#ResumeAt', datetime.getDisplayRunningTime(resumePositionTicks)),
                id: 'resume'
            });

            menuItems.push({
                name: globalize.translate('sharedcomponents#PlayFromBeginning'),
                id: 'play'
            });
        } else {
            menuItems.push({
                name: globalize.translate('sharedcomponents#Play'),
                id: 'play'
            });
        }

        if (showExternalPlayer) {
            menuItems.push({
                name: globalize.translate('ButtonPlayExternalPlayer'),
                id: 'externalplayer'
            });
        }

        if (playbackManager.canQueueMediaType(mediaType)) {
            menuItems.push({
                name: globalize.translate('sharedcomponents#Queue'),
                id: 'queue'
            });
        }

        if (itemType == "Audio" || itemType == "MusicAlbum" || itemType == "MusicArtist" || itemType == "MusicGenre") {
            menuItems.push({
                name: globalize.translate('sharedcomponents#InstantMix'),
                id: 'instantmix'
            });
        }

        if (isFolder || itemType == "MusicArtist" || itemType == "MusicGenre") {
            menuItems.push({
                name: globalize.translate('sharedcomponents#Shuffle'),
                id: 'shuffle'
            });
        }

        actionsheet.show({

            items: menuItems,
            positionTo: options.positionTo

        }).then(function (id) {
            switch (id) {

                case 'play':
                    playbackManager.play({
                        ids: [playableItemId],
                        serverId: serverId
                    });
                    break;
                case 'externalplayer':
                    LibraryBrowser.playInExternalPlayer(playableItemId);
                    break;
                case 'resume':
                    playbackManager.play({
                        ids: [playableItemId],
                        startPositionTicks: resumePositionTicks,
                        serverId: serverId
                    });
                    break;
                case 'queue':
                    playbackManager.queue(item);
                    break;
                case 'instantmix':
                    playbackManager.instantMix(item);
                    break;
                case 'shuffle':
                    playbackManager.shuffle(item);
                    break;
                default:
                    break;
            }
        });
    }

    return {
        show: show
    };
});
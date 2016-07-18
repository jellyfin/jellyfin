define(['actionsheet', 'datetime', 'playbackManager', 'globalize'], function (actionsheet, datetime, playbackManager, globalize) {

    function show(options) {

        var item = options.item;

        var itemType = item.Type;
        var mediaType = item.MediaType;
        var isFolder = item.IsFolder;
        var itemId = item.Id;
        var serverId = item.ServerId;
        var resumePositionTicks = item.UserData ? item.UserData.PlaybackPositionTicks : null;

        if (!resumePositionTicks && mediaType != "Audio" && !isFolder) {
            playbackManager.play({
                items: [item]
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
                        ids: [itemId],
                        serverId: item.ServerId
                    });
                    break;
                case 'resume':
                    playbackManager.play({
                        ids: [itemId],
                        startPositionTicks: resumePositionTicks,
                        serverId: item.ServerId
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
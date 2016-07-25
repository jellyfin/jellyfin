define(['apphost', 'globalize', 'connectionManager', 'itemHelper', 'embyRouter', 'playbackManager', 'loading', 'appSettings'], function (appHost, globalize, connectionManager, itemHelper, embyRouter, playbackManager, loading, appSettings) {

    var isMobileApp = window.Dashboard != null;

    function getCommands(options) {

        var item = options.item;

        var serverId = item.ServerId;
        var apiClient = connectionManager.getApiClient(serverId);

        var canPlay = playbackManager.canPlay(item);

        return apiClient.getCurrentUser().then(function (user) {

            var commands = [];

            if (itemHelper.supportsAddingToCollection(item)) {
                commands.push({
                    name: globalize.translate('sharedcomponents#AddToCollection'),
                    id: 'addtocollection'
                });
            }

            if (itemHelper.supportsAddingToPlaylist(item)) {
                commands.push({
                    name: globalize.translate('sharedcomponents#AddToPlaylist'),
                    id: 'addtoplaylist'
                });
            }

            if (item.Type == 'Timer' && user.Policy.EnableLiveTvManagement) {
                commands.push({
                    name: globalize.translate('sharedcomponents#ButtonCancel'),
                    id: 'canceltimer'
                });
            }

            if (item.CanDelete) {
                commands.push({
                    name: globalize.translate('sharedcomponents#Delete'),
                    id: 'delete'
                });
            }

            if (itemHelper.canEdit(user, item.Type)) {

                if (options.edit !== false) {

                    var text = item.Type == 'Timer' ? globalize.translate('sharedcomponents#Edit') : globalize.translate('sharedcomponents#EditInfo');

                    commands.push({
                        name: text,
                        id: 'edit'
                    });
                }
            }

            if (itemHelper.canEditImages(user, item.Type)) {

                if (isMobileApp) {
                    if (options.editImages !== false) {
                        commands.push({
                            name: globalize.translate('sharedcomponents#EditImages'),
                            id: 'editimages'
                        });
                    }
                }
            }

            if (itemHelper.canEdit(user, item.Type)) {

                if (item.MediaType == 'Video' && item.Type != 'TvChannel' && item.Type != 'Program' && item.LocationType != 'Virtual') {
                    if (options.editSubtitles !== false) {
                        commands.push({
                            name: globalize.translate('sharedcomponents#EditSubtitles'),
                            id: 'editsubtitles'
                        });
                    }
                }
            }

            if (item.CanDownload && appHost.supports('filedownload')) {
                commands.push({
                    name: globalize.translate('sharedcomponents#Download'),
                    id: 'download'
                });
            }

            if (isMobileApp && options.identify !== false) {
                if (itemHelper.canIdentify(user, item.Type)) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#Identify'),
                        id: 'identify'
                    });
                }
            }

            if (item.MediaType == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicArtist" || item.Type == "MusicGenre" || item.CollectionType == "music") {
                if (options.instantMix !== false) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#InstantMix'),
                        id: 'instantmix'
                    });
                }
            }

            if (options.open !== false) {
                if (item.Type != 'Timer' && item.Type != 'Audio') {
                    commands.push({
                        name: globalize.translate('sharedcomponents#Open'),
                        id: 'open'
                    });
                }
            }

            if (canPlay) {
                if (options.play !== false) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#Play'),
                        id: 'resume'
                    });

                    if (isMobileApp && appSettings.enableExternalPlayers()) {
                        commands.push({
                            name: globalize.translate('ButtonPlayExternalPlayer'),
                            id: 'externalplayer'
                        });
                    }
                }

                if (options.playAllFromHere && item.Type != 'Program' && item.Type != 'TvChannel') {
                    commands.push({
                        name: globalize.translate('sharedcomponents#PlayAllFromHere'),
                        id: 'playallfromhere'
                    });
                }

                if (playbackManager.canQueueMediaType(item.MediaType)) {
                    if (options.queue !== false) {
                        commands.push({
                            name: globalize.translate('sharedcomponents#Queue'),
                            id: 'queue'
                        });
                    }

                    if (options.queueAllFromHere) {
                        commands.push({
                            name: globalize.translate('sharedcomponents#QueueAllFromHere'),
                            id: 'queueallfromhere'
                        });
                    }
                }
            }

            if (item.Type == 'Program' && (!item.TimerId && !item.SeriesTimerId)) {

                commands.push({
                    name: Globalize.translate('sharedcomponents#Record'),
                    id: 'record'
                });
            }

            if (user.Policy.IsAdministrator) {

                if (item.Type != 'Timer' && item.Type != 'Program') {
                    commands.push({
                        name: globalize.translate('sharedcomponents#Refresh'),
                        id: 'refresh'
                    });
                }
            }

            if (item.PlaylistItemId && options.playlistId) {
                commands.push({
                    name: globalize.translate('sharedcomponents#RemoveFromPlaylist'),
                    id: 'removefromplaylist'
                });
            }

            if (options.collectionId) {
                commands.push({
                    name: globalize.translate('sharedcomponents#RemoveFromCollection'),
                    id: 'removefromcollection'
                });
            }

            if (options.share !== false) {
                if (itemHelper.canShare(user, item)) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#Share'),
                        id: 'share'
                    });
                }
            }

            if (item.IsFolder || item.Type == "MusicArtist" || item.Type == "MusicGenre") {
                if (options.shuffle !== false) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#Shuffle'),
                        id: 'shuffle'
                    });
                }
            }

            if (isMobileApp && options.sync !== false) {
                if (itemHelper.canSync(user, item)) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#Sync'),
                        id: 'sync'
                    });
                }
            }

            if (options.openAlbum !== false && item.AlbumId) {
                commands.push({
                    name: Globalize.translate('sharedcomponents#ViewAlbum'),
                    id: 'album'
                });
            }

            if (options.openArtist !== false && item.ArtistItems && item.ArtistItems.length) {
                commands.push({
                    name: Globalize.translate('sharedcomponents#ViewArtist'),
                    id: 'artist'
                });
            }

            return commands;
        });
    }

    function getResolveFunction(resolve, id, changed, deleted) {

        return function () {
            resolve({
                command: id,
                updated: changed,
                deleted: deleted
            });
        };
    }

    function executeCommand(item, id, options) {

        var itemId = item.Id;
        var serverId = item.ServerId;
        var apiClient = connectionManager.getApiClient(serverId);

        return new Promise(function (resolve, reject) {

            switch (id) {

                case 'addtocollection':
                    {
                        require(['collectionEditor'], function (collectionEditor) {

                            new collectionEditor().show({
                                items: [itemId],
                                serverId: serverId

                            }).then(getResolveFunction(resolve, id, true), getResolveFunction(resolve, id));
                        });
                        break;
                    }
                case 'addtoplaylist':
                    {
                        require(['playlistEditor'], function (playlistEditor) {

                            new playlistEditor().show({
                                items: [itemId],
                                serverId: serverId

                            }).then(getResolveFunction(resolve, id, true), getResolveFunction(resolve, id));
                        });
                        break;
                    }
                case 'download':
                    {
                        require(['fileDownloader'], function (fileDownloader) {
                            var downloadHref = apiClient.getUrl("Items/" + itemId + "/Download", {
                                api_key: apiClient.accessToken()
                            });

                            fileDownloader.download([
                            {
                                url: downloadHref,
                                itemId: itemId,
                                serverId: serverId
                            }]);

                            getResolveFunction(getResolveFunction(resolve, id), id)();
                        });

                        break;
                    }
                case 'editsubtitles':
                    {
                        require(['subtitleEditor'], function (subtitleEditor) {

                            var serverId = apiClient.serverInfo().Id;
                            subtitleEditor.show(itemId, serverId).then(getResolveFunction(resolve, id, true), getResolveFunction(resolve, id));
                        });
                        break;
                    }
                case 'edit':
                    {
                        editItem(apiClient, item).then(getResolveFunction(resolve, id, true), getResolveFunction(resolve, id));
                        break;
                    }
                case 'editimages':
                    {
                        require(['components/imageeditor/imageeditor'], function (ImageEditor) {

                            ImageEditor.show(itemId).then(getResolveFunction(resolve, id, true), getResolveFunction(resolve, id));
                        });
                        break;
                    }
                case 'identify':
                    {
                        require(['components/itemidentifier/itemidentifier'], function (itemidentifier) {

                            itemidentifier.show(itemId).then(getResolveFunction(resolve, id, true), getResolveFunction(resolve, id));
                        });
                        break;
                    }
                case 'refresh':
                    {
                        refresh(apiClient, itemId);
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'open':
                    {
                        embyRouter.showItem(item);
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'play':
                    {
                        play(item, false);
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'resume':
                    {
                        play(item, true);
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'queue':
                    {
                        play(item, false, true);
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'record':
                    require(['recordingCreator'], function (recordingCreator) {
                        recordingCreator.show(itemId, serverId).then(getResolveFunction(resolve, id, true), getResolveFunction(resolve, id));
                    });
                    break;
                case 'shuffle':
                    {
                        playbackManager.shuffle(item);
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'instantmix':
                    {
                        playbackManager.instantMix(item);
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'delete':
                    {
                        deleteItem(apiClient, itemId).then(getResolveFunction(resolve, id, true, true), getResolveFunction(resolve, id));
                        break;
                    }
                case 'share':
                    {
                        require(['sharingmanager'], function (sharingManager) {
                            sharingManager.showMenu({
                                serverId: serverId,
                                itemId: itemId

                            }).then(getResolveFunction(resolve, id));
                        });
                        break;
                    }
                case 'externalplayer':
                    LibraryBrowser.playInExternalPlayer(itemId);
                    getResolveFunction(resolve, id)();
                    break;
                case 'album':
                    {
                        embyRouter.showItem(item.AlbumId, item.ServerId);
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'artist':
                    {
                        embyRouter.showItem(item.ArtistItems[0].Id, item.ServerId);
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'playallfromhere':
                    {
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'queueallfromhere':
                    {
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'sync':
                    {
                        require(['syncDialog'], function (syncDialog) {
                            syncDialog.showMenu({
                                items: [
                                {
                                    Id: itemId
                                }]
                            });
                        });
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'removefromplaylist':

                    apiClient.ajax({

                        url: apiClient.getUrl('Playlists/' + options.playlistId + '/Items', {
                            EntryIds: [item.PlaylistItemId].join(',')
                        }),

                        type: 'DELETE'

                    }).then(function () {

                        getResolveFunction(resolve, id, true)();
                    });

                    break;
                case 'removefromcollection':

                    apiClient.ajax({
                        type: "DELETE",
                        url: apiClient.getUrl("Collections/" + options.collectionId + "/Items", {

                            Ids: [item.Id].join(',')
                        })

                    }).then(function () {

                        getResolveFunction(resolve, id, true)();
                    });

                    break;
                case 'canceltimer':
                    deleteTimer(apiClient, item, resolve, id);
                    break;
                default:
                    reject();
                    break;
            }
        });
    }

    function deleteTimer(apiClient, item, resolve, command) {

        require(['confirm'], function (confirm) {

            confirm(globalize.translate('sharedcomponents#MessageConfirmRecordingCancellation'), globalize.translate('sharedcomponents#HeaderConfirmRecordingCancellation')).then(function () {

                loading.show();

                apiClient.cancelLiveTvTimer(item.Id).then(function () {

                    require(['toast'], function (toast) {
                        toast(globalize.translate('sharedcomponents#RecordingCancelled'));
                    });

                    loading.hide();
                    getResolveFunction(resolve, command, true)();
                });
            });
        });
    }

    function play(item, resume, queue) {

        var method = queue ? 'queue' : 'play';

        var startPosition = 0;
        if (resume && item.UserData && item.UserData.PlaybackPositionTicks) {
            startPosition = item.UserData.PlaybackPositionTicks;
        }

        if (item.Type == 'Program') {
            playbackManager[method]({
                ids: [item.ChannelId],
                startPositionTicks: startPosition
            });
        } else {
            playbackManager[method]({
                items: [item],
                startPositionTicks: startPosition
            });
        }
    }

    function editItem(apiClient, item) {

        return new Promise(function (resolve, reject) {

            var serverId = apiClient.serverInfo().Id;

            if (item.Type == 'Timer') {
                require(['recordingEditor'], function (recordingEditor) {

                    recordingEditor.show(item.Id, serverId).then(resolve, reject);
                });
            } else {
                require(['metadataEditor'], function (metadataEditor) {

                    metadataEditor.show(item.Id, serverId).then(resolve, reject);
                });
            }
        });
    }

    function deleteItem(apiClient, itemId) {

        return new Promise(function (resolve, reject) {

            var msg = globalize.translate('sharedcomponents#ConfirmDeleteItem');
            var title = globalize.translate('sharedcomponents#HeaderDeleteItem');

            require(['confirm'], function (confirm) {

                confirm(msg, title).then(function () {

                    apiClient.deleteItem(itemId).then(function () {
                        resolve(true);
                    });

                }, reject);

            });
        });
    }

    function refresh(apiClient, itemId) {

        require(['refreshDialog'], function (refreshDialog) {
            new refreshDialog({
                itemIds: [itemId],
                serverId: apiClient.serverInfo().Id
            }).show();
        });
    }

    function show(options) {

        return getCommands(options).then(function (commands) {

            return new Promise(function (resolve, reject) {

                require(['actionsheet'], function (actionSheet) {

                    actionSheet.show({

                        items: commands,
                        positionTo: options.positionTo

                    }).then(function (id) {
                        executeCommand(options.item, id, options).then(resolve);
                    }, reject);
                });
            });

        });
    }

    return {
        getCommands: getCommands,
        show: show
    };
});
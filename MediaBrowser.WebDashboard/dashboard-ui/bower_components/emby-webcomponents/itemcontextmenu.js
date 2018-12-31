define(['apphost', 'globalize', 'connectionManager', 'itemHelper', 'appRouter', 'playbackManager', 'loading', 'appSettings', 'browser', 'actionsheet'], function (appHost, globalize, connectionManager, itemHelper, appRouter, playbackManager, loading, appSettings, browser, actionsheet) {
    'use strict';

    function getCommands(options) {

        var item = options.item;

        var canPlay = playbackManager.canPlay(item);

        var commands = [];

        var user = options.user;

        var restrictOptions = (browser.operaTv || browser.web0s) && !user.Policy.IsAdministrator;

        if (canPlay && item.MediaType !== 'Photo') {
            if (options.play !== false) {
                commands.push({
                    name: globalize.translate('sharedcomponents#Play'),
                    id: 'resume'
                });
            }

            if (options.playAllFromHere && item.Type !== 'Program' && item.Type !== 'TvChannel') {
                commands.push({
                    name: globalize.translate('sharedcomponents#PlayAllFromHere'),
                    id: 'playallfromhere'
                });
            }
        }

        if (playbackManager.canQueue(item)) {

            if (options.queue !== false) {
                commands.push({
                    name: globalize.translate('sharedcomponents#AddToPlayQueue'),
                    id: 'queue'
                });
            }

            if (options.queue !== false) {
                commands.push({
                    name: globalize.translate('sharedcomponents#PlayNext'),
                    id: 'queuenext'
                });
            }

            //if (options.queueAllFromHere) {
            //    commands.push({
            //        name: globalize.translate('sharedcomponents#QueueAllFromHere'),
            //        id: 'queueallfromhere'
            //    });
            //}
        }



        if (item.IsFolder || item.Type === "MusicArtist" || item.Type === "MusicGenre") {
            if (item.CollectionType !== 'livetv') {
                if (options.shuffle !== false) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#Shuffle'),
                        id: 'shuffle'
                    });
                }
            }
        }

        if (item.MediaType === "Audio" || item.Type === "MusicAlbum" || item.Type === "MusicArtist" || item.Type === "MusicGenre") {
            if (options.instantMix !== false && !itemHelper.isLocalItem(item)) {
                commands.push({
                    name: globalize.translate('sharedcomponents#InstantMix'),
                    id: 'instantmix'
                });
            }
        }

        if (commands.length) {
            commands.push({
                divider: true
            });
        }

        if (!restrictOptions) {
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
        }

        if ((item.Type === 'Timer') && user.Policy.EnableLiveTvManagement && options.cancelTimer !== false) {
            commands.push({
                name: globalize.translate('sharedcomponents#CancelRecording'),
                id: 'canceltimer'
            });
        }

        if ((item.Type === 'Recording' && item.Status === 'InProgress') && user.Policy.EnableLiveTvManagement && options.cancelTimer !== false) {
            commands.push({
                name: globalize.translate('sharedcomponents#CancelRecording'),
                id: 'canceltimer'
            });
        }

        if ((item.Type === 'SeriesTimer') && user.Policy.EnableLiveTvManagement && options.cancelTimer !== false) {
            commands.push({
                name: globalize.translate('sharedcomponents#CancelSeries'),
                id: 'cancelseriestimer'
            });
        }

        if (itemHelper.canConvert(item, user, connectionManager.getApiClient(item))) {
            commands.push({
                name: globalize.translate('sharedcomponents#Convert'),
                id: 'convert'
            });
        }

        if (item.CanDelete && options.deleteItem !== false) {

            if (item.Type === 'Playlist' || item.Type === 'BoxSet') {
                commands.push({
                    name: globalize.translate('sharedcomponents#Delete'),
                    id: 'delete'
                });
            } else {
                commands.push({
                    name: globalize.translate('sharedcomponents#DeleteMedia'),
                    id: 'delete'
                });
            }
        }

        if (item.CanDownload && appHost.supports('filedownload')) {
            commands.push({
                name: globalize.translate('sharedcomponents#Download'),
                id: 'download'
            });
        }

        if (appHost.supports('sync') && options.syncLocal !== false) {
            if (itemHelper.canSync(user, item)) {
                commands.push({
                    name: globalize.translate('sharedcomponents#Download'),
                    id: 'synclocal'
                });
            }
        }

        var canEdit = itemHelper.canEdit(user, item);
        if (canEdit) {

            if (options.edit !== false && item.Type !== 'SeriesTimer') {

                var text = (item.Type === 'Timer' || item.Type === 'SeriesTimer') ? globalize.translate('sharedcomponents#Edit') : globalize.translate('sharedcomponents#EditMetadata');

                commands.push({
                    name: text,
                    id: 'edit'
                });
            }
        }

        if (itemHelper.canEditImages(user, item)) {

            if (options.editImages !== false) {
                commands.push({
                    name: globalize.translate('sharedcomponents#EditImages'),
                    id: 'editimages'
                });
            }
        }

        if (canEdit) {

            if (item.MediaType === 'Video' && item.Type !== 'TvChannel' && item.Type !== 'Program' && item.LocationType !== 'Virtual' && !(item.Type === 'Recording' && item.Status !== 'Completed')) {
                if (options.editSubtitles !== false) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#EditSubtitles'),
                        id: 'editsubtitles'
                    });
                }
            }
        }

        if (options.identify !== false) {
            if (itemHelper.canIdentify(user, item)) {
                commands.push({
                    name: globalize.translate('sharedcomponents#Identify'),
                    id: 'identify'
                });
            }
        }

        if (item.Type === 'Program' && options.record !== false) {

            if (item.TimerId) {
                commands.push({
                    name: Globalize.translate('sharedcomponents#ManageRecording'),
                    id: 'record'
                });
            }
        }

        if (item.Type === 'Program' && options.record !== false) {

            if (!item.TimerId) {
                commands.push({
                    name: Globalize.translate('sharedcomponents#Record'),
                    id: 'record'
                });
            }
        }

        if (itemHelper.canRefreshMetadata(item, user)) {
            commands.push({
                name: globalize.translate('sharedcomponents#RefreshMetadata'),
                id: 'refresh'
            });
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

        if (!restrictOptions) {
            if (options.share === true) {
                if (itemHelper.canShare(item, user)) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#Share'),
                        id: 'share'
                    });
                }
            }
        }

        if (options.sync !== false) {
            if (itemHelper.canSync(user, item)) {
                commands.push({
                    name: globalize.translate('sharedcomponents#Sync'),
                    id: 'sync'
                });
            }
        }

        if (options.openAlbum !== false && item.AlbumId && item.MediaType !== 'Photo') {
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
                            var downloadHref = apiClient.getItemDownloadUrl(itemId);

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
                        require(['imageEditor'], function (imageEditor) {

                            imageEditor.show({
                                itemId: itemId,
                                serverId: serverId

                            }).then(getResolveFunction(resolve, id, true), getResolveFunction(resolve, id));
                        });
                        break;
                    }
                case 'identify':
                    {
                        require(['itemIdentifier'], function (itemIdentifier) {

                            itemIdentifier.show(itemId, serverId).then(getResolveFunction(resolve, id, true), getResolveFunction(resolve, id));
                        });
                        break;
                    }
                case 'refresh':
                    {
                        refresh(apiClient, item);
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'open':
                    {
                        appRouter.showItem(item);
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
                case 'queuenext':
                    {
                        play(item, false, true, true);
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
                        deleteItem(apiClient, item).then(getResolveFunction(resolve, id, true, true), getResolveFunction(resolve, id));
                        break;
                    }
                case 'share':
                    {
                        navigator.share({
                            title: item.Name,
                            text: item.Overview,

                            // TODO: Make this the server's external url
                            url: 'https://emby.media'
                        });
                        break;
                    }
                case 'album':
                    {
                        appRouter.showItem(item.AlbumId, item.ServerId);
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'artist':
                    {
                        appRouter.showItem(item.ArtistItems[0].Id, item.ServerId);
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
                case 'convert':
                    {
                        require(['syncDialog'], function (syncDialog) {
                            syncDialog.showMenu({
                                items: [item],
                                serverId: serverId,
                                mode: 'convert'
                            });
                        });
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'sync':
                    {
                        require(['syncDialog'], function (syncDialog) {
                            syncDialog.showMenu({
                                items: [item],
                                serverId: serverId,
                                mode: 'sync'
                            });
                        });
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'synclocal':
                    {
                        require(['syncDialog'], function (syncDialog) {
                            syncDialog.showMenu({
                                items: [item],
                                serverId: serverId,
                                mode: 'download'
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
                case 'cancelseriestimer':
                    deleteSeriesTimer(apiClient, item, resolve, id);
                    break;
                default:
                    reject();
                    break;
            }
        });
    }

    function deleteTimer(apiClient, item, resolve, command) {

        require(['recordingHelper'], function (recordingHelper) {

            var timerId = item.TimerId || item.Id;

            recordingHelper.cancelTimerWithConfirmation(timerId, item.ServerId).then(function () {
                getResolveFunction(resolve, command, true)();
            });
        });
    }

    function deleteSeriesTimer(apiClient, item, resolve, command) {

        require(['recordingHelper'], function (recordingHelper) {

            recordingHelper.cancelSeriesTimerWithConfirmation(item.Id, item.ServerId).then(function () {
                getResolveFunction(resolve, command, true)();
            });
        });
    }

    function play(item, resume, queue, queueNext) {

        var method = queue ? (queueNext ? 'queueNext' : 'queue') : 'play';

        var startPosition = 0;
        if (resume && item.UserData && item.UserData.PlaybackPositionTicks) {
            startPosition = item.UserData.PlaybackPositionTicks;
        }

        if (item.Type === 'Program') {
            playbackManager[method]({
                ids: [item.ChannelId],
                startPositionTicks: startPosition,
                serverId: item.ServerId
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

            if (item.Type === 'Timer') {
                require(['recordingEditor'], function (recordingEditor) {

                    recordingEditor.show(item.Id, serverId).then(resolve, reject);
                });
            } else if (item.Type === 'SeriesTimer') {
                require(['seriesRecordingEditor'], function (recordingEditor) {

                    recordingEditor.show(item.Id, serverId).then(resolve, reject);
                });
            } else {
                require(['metadataEditor'], function (metadataEditor) {

                    metadataEditor.show(item.Id, serverId).then(resolve, reject);
                });
            }
        });
    }

    function deleteItem(apiClient, item) {

        return new Promise(function (resolve, reject) {

            require(['deleteHelper'], function (deleteHelper) {

                deleteHelper.deleteItem({

                    item: item,
                    navigate: false

                }).then(function () {

                    resolve(true);

                }, reject);

            });
        });
    }

    function refresh(apiClient, item) {

        require(['refreshDialog'], function (refreshDialog) {
            new refreshDialog({
                itemIds: [item.Id],
                serverId: apiClient.serverInfo().Id,
                mode: item.Type === 'CollectionFolder' ? 'scan' : null
            }).show();
        });
    }

    function show(options) {

        var commands = getCommands(options);

        if (!commands.length) {
            return Promise.reject();
        }

        return actionsheet.show({

            items: commands,
            positionTo: options.positionTo,

            resolveOnClick: ['share']

        }).then(function (id) {
            return executeCommand(options.item, id, options);
        });
    }

    return {
        getCommands: getCommands,
        show: show
    };
});
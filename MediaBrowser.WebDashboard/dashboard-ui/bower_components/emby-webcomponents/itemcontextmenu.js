define(['apphost', 'globalize', 'connectionManager', 'itemHelper', 'embyRouter', 'playbackManager'], function (appHost, globalize, connectionManager, itemHelper, embyRouter, playbackManager) {

    function getCommands(options) {

        var item = options.item;

        var serverId = item.ServerId;
        var apiClient = connectionManager.getApiClient(serverId);

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

            if (item.CanDelete) {
                commands.push({
                    name: globalize.translate('sharedcomponents#Delete'),
                    id: 'delete'
                });
            }

            if (user.Policy.IsAdministrator) {
                if (item.MediaType == 'Video' && item.Type != 'TvChannel' && item.Type != 'Program' && item.LocationType != 'Virtual') {
                    commands.push({
                        name: globalize.translate('sharedcomponents#EditSubtitles'),
                        id: 'editsubtitles'
                    });
                }
            }

            if (item.CanDownload && appHost.supports('filedownload')) {
                commands.push({
                    name: globalize.translate('sharedcomponents#Download'),
                    id: 'download'
                });
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
                commands.push({
                    name: globalize.translate('sharedcomponents#Open'),
                    id: 'open'
                });
            }

            if (options.play) {
                commands.push({
                    name: globalize.translate('sharedcomponents#Play'),
                    id: 'resume'
                });
            }

            if (options.playAllFromHere) {
                commands.push({
                    name: globalize.translate('sharedcomponents#PlayAllFromHere'),
                    id: 'playallfromhere'
                });
            }

            if (playbackManager.canQueueMediaType(item.MediaType)) {
                if (options.queue) {
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

            if (user.Policy.IsAdministrator) {

                commands.push({
                    name: globalize.translate('sharedcomponents#Refresh'),
                    id: 'refresh'
                });
            }

            if (item.Type != 'Timer' && user.Policy.EnablePublicSharing && appHost.supports('sharing')) {
                commands.push({
                    name: globalize.translate('sharedcomponents#Share'),
                    id: 'share'
                });
            }

            if (item.IsFolder || item.Type == "MusicArtist" || item.Type == "MusicGenre") {
                if (options.shuffle !== false) {
                    commands.push({
                        name: globalize.translate('sharedcomponents#Shuffle'),
                        id: 'shuffle'
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

    function executeCommand(item, id) {

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
                        playbackManager.play({
                            items: [item]
                        });
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'resume':
                    {
                        playbackManager.play({
                            items: [item]
                        });
                        getResolveFunction(resolve, id)();
                        break;
                    }
                case 'queue':
                    {
                        playbackManager.queue({
                            items: [item]
                        });
                        getResolveFunction(resolve, id)();
                        break;
                    }
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
                default:
                    reject();
                    break;
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
                        executeCommand(options.item, id).then(resolve);
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
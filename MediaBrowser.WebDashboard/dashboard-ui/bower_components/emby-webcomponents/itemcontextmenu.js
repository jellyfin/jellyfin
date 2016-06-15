define(['apphost', 'globalize', 'connectionManager', 'itemHelper'], function (appHost, globalize, connectionManager, itemHelper) {

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

            if (user.Policy.IsAdministrator) {

                commands.push({
                    name: globalize.translate('Refresh'),
                    id: 'refresh'
                });
            }

            if (item.Type != 'Timer' && user.Policy.EnablePublicSharing && appHost.supports('sharing')) {
                commands.push({
                    name: globalize.translate('Share'),
                    id: 'share'
                });
            }

            return commands;
        });
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

                            }).then(reject, reject);
                        });
                        break;
                    }
                case 'addtoplaylist':
                    {
                        require(['playlistEditor'], function (playlistEditor) {

                            new playlistEditor().show({
                                items: [itemId],
                                serverId: serverId

                            }).then(reject, reject);
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

                            reject();
                        });

                        break;
                    }
                case 'editsubtitles':
                    {
                        require(['subtitleEditor'], function (subtitleEditor) {

                            var serverId = apiClient.serverInfo().Id;
                            subtitleEditor.show(itemId, serverId).then(resolve, reject);
                        });
                        break;
                    }
                case 'refresh':
                    {
                        refresh(apiClient, itemId);
                        reject();
                        break;
                    }
                case 'delete':
                    {
                        deleteItem(apiClient, itemId).then(function () {
                            resolve(true);
                        });
                        break;
                    }
                case 'share':
                    {
                        require(['sharingmanager'], function (sharingManager) {
                            sharingManager.showMenu({
                                serverId: serverId,
                                itemId: itemId

                            }).then(reject);
                        });
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
                        items: commands
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
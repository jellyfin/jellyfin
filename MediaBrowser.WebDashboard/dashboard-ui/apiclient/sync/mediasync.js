(function (globalScope) {

    function mediaSync() {

        var self = this;

        self.sync = function (apiClient, serverInfo) {

            var deferred = DeferredBuilder.Deferred();

            reportOfflineActions(apiClient, serverInfo).done(function () {

                // Do the first data sync
                syncData(apiClient, serverInfo, false).done(function () {

                    // Download new content
                    getNewMedia(apiClient).done(function () {

                        // Do the second data sync
                        syncData(apiClient, serverInfo, false).done(function () {

                            deferred.resolve();

                        }).fail(getOnFail(deferred));

                    }).fail(getOnFail(deferred));

                }).fail(getOnFail(deferred));

            }).fail(getOnFail(deferred));

            return deferred.promise();
        };

        function reportOfflineActions(apiClient, serverInfo) {

            Logger.log('Begin reportOfflineActions');

            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                LocalAssetManager.getOfflineActions(serverInfo.Id).done(function (actions) {

                    if (!actions.length) {
                        deferred.resolve();
                        return;
                    }

                    apiClient.reportOfflineActions(actions).done(function () {

                        deferred.resolve();

                    }).fail(getOnFail(deferred));

                }).fail(getOnFail(deferred));
            });

            return deferred.promise();
        }

        function syncData(apiClient, serverInfo, syncUserItemAccess) {

            Logger.log('Begin syncData');

            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                LocalAssetManager.getServerItemIds(serverInfo.Id).done(function (localIds) {

                    var request = {
                        TargetId: apiClient.deviceId(),
                        LocalItemIds: localIds,
                        OfflineUserIds: (serverInfo.Users || []).map(function (u) { return u.Id; })
                    };

                    apiClient.syncData(request).done(function (result) {

                        afterSyncData(apiClient, serverInfo, syncUserItemAccess, result, deferred);

                    }).fail(getOnFail(deferred));

                }).fail(getOnFail(deferred));
            });

            return deferred.promise();
        }

        function afterSyncData(apiClient, serverInfo, enableSyncUserItemAccess, syncDataResult, deferred) {

            Logger.log('Begin afterSyncData');

            removeLocalItems(syncDataResult, serverInfo.Id).done(function (result) {

                if (enableSyncUserItemAccess) {
                    syncUserItemAccess(syncDataResult, serverInfo.Id).done(function () {

                        deferred.resolve();

                    }).fail(getOnFail(deferred));
                }
                else {
                    deferred.resolve();
                }

            }).fail(getOnFail(deferred));

            deferred.resolve();
        }

        function removeLocalItems(syncDataResult, serverId) {

            Logger.log('Begin removeLocalItems');

            var deferred = DeferredBuilder.Deferred();

            removeNextLocalItem(syncDataResult.ItemIdsToRemove, 0, serverId, deferred);

            return deferred.promise();
        }

        function removeNextLocalItem(itemIdsToRemove, index, serverId, deferred) {

            var length = itemIdsToRemove.length;

            if (index >= length) {

                deferred.resolve();
                return;
            }

            removeLocalItem(itemIdsToRemove[index], serverId).done(function () {

                removeNextLocalItem(itemIdsToRemove, index + 1, serverId, deferred);
            }).fail(function () {
                removeNextLocalItem(itemIdsToRemove, index + 1, serverId, deferred);
            });
        }

        function removeLocalItem(itemId, serverId) {

            Logger.log('Begin removeLocalItem');

            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                LocalAssetManager.removeLocalItem(itemId, serverId).done(function (localIds) {

                    deferred.resolve();

                }).fail(getOnFail(deferred));
            });

            return deferred.promise();
        }

        function getNewMedia(apiClient) {

            Logger.log('Begin getNewMedia');

            var deferred = DeferredBuilder.Deferred();

            apiClient.getReadySyncItems(apiClient.deviceId()).done(function (jobItems) {

                getNextNewItem(jobItems, 0, apiClient, deferred);

            }).fail(getOnFail(deferred));

            return deferred.promise();
        }

        function getNextNewItem(jobItems, index, apiClient, deferred) {

            var length = jobItems.length;

            if (index >= length) {

                deferred.resolve();
                return;
            }

            getNewItem(jobItems[index], apiClient).done(function () {

                getNextNewItem(jobItems, index + 1, apiClient, deferred);
            }).fail(function () {
                getNextNewItem(jobItems, index + 1, apiClient, deferred);
            });
        }

        function getNewItem(jobItem, apiClient) {
            var deferred = DeferredBuilder.Deferred();
            deferred.resolve();
            return deferred.promise();
        }

        function syncUserItemAccess(syncDataResult, serverId) {
            Logger.log('Begin syncUserItemAccess');

            var deferred = DeferredBuilder.Deferred();

            var itemIds = [];
            for (var id in syncDataResult.ItemUserAccess) {
                itemIds.push(id);
            }

            syncNextUserAccessForItem(itemIds, 0, syncDataResult, serverId, deferred);

            return deferred.promise();
        }

        function syncNextUserAccessForItem(itemIds, index, syncDataResult, serverId, deferred) {

            var length = itemIds.length;

            if (index >= length) {

                deferred.resolve();
                return;
            }

            syncUserAccessForItem(itemIds[index], syncDataResult).done(function () {

                syncNextUserAccessForItem(itemIds, index + 1, syncDataResult, serverId, deferred);
            }).fail(function () {
                syncNextUserAccessForItem(itemIds, index + 1, syncDataResult, serverId, deferred);
            });
        }

        function syncUserAccessForItem(itemId, syncDataResult) {
            Logger.log('Begin syncUserAccessForItem');

            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                LocalAssetManager.getLocalItem(itemId, serverId).done(function (localItem) {

                    var userIdsWithAccess = syncDataResult.ItemUserAccess[itemId];

                    if (userIdsWithAccess.join(',') == localItem.UserIdsWithAccess.join(',')) {
                        // Hasn't changed, nothing to do
                        deferred.resolve();
                    }
                    else {

                        localItem.UserIdsWithAccess = userIdsWithAccess;
                        localAssetManager.addOrUpdateLocalItem(localItem).done(function () {
                            deferred.resolve();
                        }).fail(getOnFail(deferred));
                    }

                }).fail(getOnFail(deferred));
            });

            return deferred.promise();
        }

        function getOnFail(deferred) {
            return function () {

                deferred.reject();
            };
        }
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.MediaSync = mediaSync;

})(this);
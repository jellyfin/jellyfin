(function (globalScope) {

    function mediaSync() {

        var self = this;

        self.sync = function (apiClient, serverInfo, options) {

            var deferred = DeferredBuilder.Deferred();

            reportOfflineActions(apiClient, serverInfo).then(function () {

                // Do the first data sync
                syncData(apiClient, serverInfo, false).then(function () {

                    // Download new content
                    getNewMedia(apiClient, serverInfo, options).then(function () {

                        // Do the second data sync
                        syncData(apiClient, serverInfo, false).then(function () {

                            deferred.resolve();

                        }, getOnFail(deferred));

                    }, getOnFail(deferred));

                }, getOnFail(deferred));

            }, getOnFail(deferred));

            return deferred.promise();
        };

        function reportOfflineActions(apiClient, serverInfo) {

            console.log('Begin reportOfflineActions');

            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                LocalAssetManager.getOfflineActions(serverInfo.Id).then(function (actions) {

                    if (!actions.length) {
                        deferred.resolve();
                        return;
                    }

                    apiClient.reportOfflineActions(actions).then(function () {

                        LocalAssetManager.deleteOfflineActions(actions).then(function () {

                            deferred.resolve();

                        }, getOnFail(deferred));

                    }, getOnFail(deferred));

                }, getOnFail(deferred));
            });

            return deferred.promise();
        }

        function syncData(apiClient, serverInfo, syncUserItemAccess) {

            console.log('Begin syncData');

            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                LocalAssetManager.getServerItemIds(serverInfo.Id).then(function (localIds) {

                    var request = {
                        TargetId: apiClient.deviceId(),
                        LocalItemIds: localIds,
                        OfflineUserIds: (serverInfo.Users || []).map(function (u) { return u.Id; })
                    };

                    apiClient.syncData(request).then(function (result) {

                        afterSyncData(apiClient, serverInfo, syncUserItemAccess, result, deferred);

                    }, getOnFail(deferred));

                }, getOnFail(deferred));
            });

            return deferred.promise();
        }

        function afterSyncData(apiClient, serverInfo, enableSyncUserItemAccess, syncDataResult, deferred) {

            console.log('Begin afterSyncData');

            removeLocalItems(syncDataResult, serverInfo.Id).then(function (result) {

                if (enableSyncUserItemAccess) {
                    syncUserItemAccess(syncDataResult, serverInfo.Id).then(function () {

                        deferred.resolve();

                    }, getOnFail(deferred));
                }
                else {
                    deferred.resolve();
                }

            }, getOnFail(deferred));

            deferred.resolve();
        }

        function removeLocalItems(syncDataResult, serverId) {

            console.log('Begin removeLocalItems');

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

            removeLocalItem(itemIdsToRemove[index], serverId).then(function () {

                removeNextLocalItem(itemIdsToRemove, index + 1, serverId, deferred);
            }, function () {
                removeNextLocalItem(itemIdsToRemove, index + 1, serverId, deferred);
            });
        }

        function removeLocalItem(itemId, serverId) {

            console.log('Begin removeLocalItem');

            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                LocalAssetManager.removeLocalItem(itemId, serverId).then(function (localIds) {

                    deferred.resolve();

                }, getOnFail(deferred));
            });

            return deferred.promise();
        }

        function getNewMedia(apiClient, serverInfo, options) {

            console.log('Begin getNewMedia');

            var deferred = DeferredBuilder.Deferred();

            apiClient.getReadySyncItems(apiClient.deviceId()).then(function (jobItems) {

                getNextNewItem(jobItems, 0, apiClient, serverInfo, options, deferred);

            }, getOnFail(deferred));

            return deferred.promise();
        }

        function getNextNewItem(jobItems, index, apiClient, serverInfo, options, deferred) {

            var length = jobItems.length;

            if (index >= length) {

                deferred.resolve();
                return;
            }

            var hasGoneNext = false;
            var goNext = function () {

                if (!hasGoneNext) {
                    hasGoneNext = true;
                    getNextNewItem(jobItems, index + 1, apiClient, serverInfo, options, deferred);
                }
            };

            getNewItem(jobItems[index], apiClient, serverInfo, options).then(goNext, goNext);
        }

        function getNewItem(jobItem, apiClient, serverInfo, options) {

            console.log('Begin getNewItem');

            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                var libraryItem = jobItem.Item;
                LocalAssetManager.createLocalItem(libraryItem, serverInfo, jobItem.OriginalFileName).then(function (localItem) {

                    downloadMedia(apiClient, jobItem, localItem, options).then(function (isQueued) {

                        if (isQueued) {
                            deferred.resolve();
                            return;
                        }

                        getImages(apiClient, jobItem, localItem).then(function () {

                            getSubtitles(apiClient, jobItem, localItem).then(function () {

                                apiClient.reportSyncJobItemTransferred(jobItem.SyncJobItemId).then(function () {

                                    deferred.resolve();

                                }, getOnFail(deferred));

                            }, getOnFail(deferred));

                        }, getOnFail(deferred));

                    }, getOnFail(deferred));

                }, getOnFail(deferred));
            });

            return deferred.promise();
        }

        function downloadMedia(apiClient, jobItem, localItem, options) {

            console.log('Begin downloadMedia');
            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                var url = apiClient.getUrl("Sync/JobItems/" + jobItem.SyncJobItemId + "/File", {
                    api_key: apiClient.accessToken()
                });

                var localPath = localItem.LocalPath;

                console.log('Downloading media. Url: ' + url + '. Local path: ' + localPath);

                options = options || {};

                LocalAssetManager.downloadFile(url, localPath, options.enableBackgroundTransfer, options.enableNewDownloads).then(function (path, isQueued) {

                    if (isQueued) {
                        deferred.resolveWith(null, [true]);
                        return;
                    }
                    LocalAssetManager.addOrUpdateLocalItem(localItem).then(function () {

                        deferred.resolveWith(null, [false]);

                    }, getOnFail(deferred));

                }, getOnFail(deferred));

            });

            return deferred.promise();
        }

        function getImages(apiClient, jobItem, localItem) {

            console.log('Begin getImages');
            var deferred = DeferredBuilder.Deferred();

            getNextImage(0, apiClient, localItem, deferred);

            return deferred.promise();
        }

        function getNextImage(index, apiClient, localItem, deferred) {

            console.log('Begin getNextImage');
            if (index >= 4) {

                deferred.resolve();
                return;
            }

            // Just for now while media syncing gets worked out
            deferred.resolve();
            return;

            var libraryItem = localItem.Item;

            var serverId = libraryItem.ServerId;
            var itemId = null;
            var imageTag = null;
            var imageType = "Primary";

            switch (index) {

                case 0:
                    itemId = libraryItem.Id;
                    imageType = "Primary";
                    imageTag = (libraryItem.ImageTags || {})["Primary"];
                    break;
                case 1:
                    itemId = libraryItem.SeriesId;
                    imageType = "Primary";
                    imageTag = libraryItem.SeriesPrimaryImageTag;
                    break;
                case 2:
                    itemId = libraryItem.SeriesId;
                    imageType = "Thumb";
                    imageTag = libraryItem.SeriesPrimaryImageTag;
                    break;
                case 3:
                    itemId = libraryItem.AlbumId;
                    imageType = "Primary";
                    imageTag = libraryItem.AlbumPrimaryImageTag;
                    break;
                default:
                    break;
            }

            if (!itemId || !imageTag) {
                getNextImage(index + 1, apiClient, localItem, deferred);
                return;
            }

            downloadImage(apiClient, serverId, itemId, imageTag, imageType).then(function () {

                // For the sake of simplicity, limit to one image
                deferred.resolve();
                return;

                getNextImage(index + 1, apiClient, localItem, deferred);

            }, getOnFail(deferred));
        }

        function downloadImage(apiClient, serverId, itemId, imageTag, imageType) {

            console.log('Begin downloadImage');
            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                LocalAssetManager.hasImage(serverId, itemId, imageTag).then(function (hasImage) {

                    if (hasImage) {
                        deferred.resolve();
                        return;
                    }

                    var imageUrl = apiClient.getImageUrl(itemId, {
                        tag: imageTag,
                        type: imageType,
                        api_key: apiClient.accessToken()
                    });

                    LocalAssetManager.downloadImage(imageUrl, serverId, itemId, imageTag).then(function () {

                        deferred.resolve();

                    }, getOnFail(deferred));

                });
            });

            return deferred.promise();
        }

        function getSubtitles(apiClient, jobItem, localItem) {

            console.log('Begin getSubtitles');
            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                if (!jobItem.Item.MediaSources.length) {
                    console.log("Cannot download subtitles because video has no media source info.");
                    deferred.resolve();
                    return;
                }

                var files = jobItem.AdditionalFiles.filter(function (f) {
                    return f.Type == 'Subtitles';
                });

                var mediaSource = jobItem.Item.MediaSources[0];

                getNextSubtitle(files, 0, apiClient, jobItem, localItem, mediaSource, deferred);
            });

            return deferred.promise();
        }

        function getNextSubtitle(files, index, apiClient, jobItem, localItem, mediaSource, deferred) {

            var length = files.length;

            if (index >= length) {

                deferred.resolve();
                return;
            }

            getItemSubtitle(file, apiClient, jobItem, localItem, mediaSource).then(function () {

                getNextSubtitle(files, index + 1, apiClient, jobItem, localItem, mediaSource, deferred);

            }, function () {
                getNextSubtitle(files, index + 1, apiClient, jobItem, localItem, mediaSource, deferred);
            });
        }

        function getItemSubtitle(file, apiClient, jobItem, localItem, mediaSource) {

            console.log('Begin getItemSubtitle');
            var deferred = DeferredBuilder.Deferred();

            var subtitleStream = mediaSource.MediaStreams.filter(function (m) {
                return m.Type == 'Subtitle' && m.Index == file.Index;
            })[0];

            if (!subtitleStream) {

                // We shouldn't get in here, but let's just be safe anyway
                console.log("Cannot download subtitles because matching stream info wasn't found.");
                deferred.reject();
                return;
            }

            var url = apiClient.getUrl("Sync/JobItems/" + jobItem.SyncJobItemId + "/AdditionalFiles", {
                Name: file.Name,
                api_key: apiClient.accessToken()
            });

            require(['localassetmanager'], function () {

                LocalAssetManager.downloadSubtitles(url, localItem, subtitleStream).then(function (subtitlePath) {

                    subtitleStream.Path = subtitlePath;
                    LocalAssetManager.addOrUpdateLocalItem(localItem).then(function () {
                        deferred.resolve();
                    }, getOnFail(deferred));

                }, getOnFail(deferred));
            });

            return deferred.promise();
        }

        function syncUserItemAccess(syncDataResult, serverId) {
            console.log('Begin syncUserItemAccess');

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

            syncUserAccessForItem(itemIds[index], syncDataResult).then(function () {

                syncNextUserAccessForItem(itemIds, index + 1, syncDataResult, serverId, deferred);
            }, function () {
                syncNextUserAccessForItem(itemIds, index + 1, syncDataResult, serverId, deferred);
            });
        }

        function syncUserAccessForItem(itemId, syncDataResult) {
            console.log('Begin syncUserAccessForItem');

            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                LocalAssetManager.getUserIdsWithAccess(itemId, serverId).then(function (savedUserIdsWithAccess) {

                    var userIdsWithAccess = syncDataResult.ItemUserAccess[itemId];

                    if (userIdsWithAccess.join(',') == savedUserIdsWithAccess.join(',')) {
                        // Hasn't changed, nothing to do
                        deferred.resolve();
                    }
                    else {

                        LocalAssetManager.saveUserIdsWithAccess(itemId, serverId, userIdsWithAccess).then(function () {
                            deferred.resolve();
                        }, getOnFail(deferred));
                    }

                }, getOnFail(deferred));
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
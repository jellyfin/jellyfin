(function (globalScope) {

    function mediaSync() {

        var self = this;

        self.sync = function (apiClient, serverInfo, options) {

            var deferred = DeferredBuilder.Deferred();

            reportOfflineActions(apiClient, serverInfo).done(function () {

                // Do the first data sync
                syncData(apiClient, serverInfo, false).done(function () {

                    // Download new content
                    getNewMedia(apiClient, serverInfo, options).done(function () {

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

                        LocalAssetManager.deleteOfflineActions(actions).done(function () {

                            deferred.resolve();

                        }).fail(getOnFail(deferred));

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

        function getNewMedia(apiClient, serverInfo, options) {

            Logger.log('Begin getNewMedia');

            var deferred = DeferredBuilder.Deferred();

            apiClient.getReadySyncItems(apiClient.deviceId()).done(function (jobItems) {

                getNextNewItem(jobItems, 0, apiClient, serverInfo, options, deferred);

            }).fail(getOnFail(deferred));

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

            getNewItem(jobItems[index], apiClient, serverInfo, options).done(goNext).fail(goNext);
        }

        function getNewItem(jobItem, apiClient, serverInfo, options) {

            Logger.log('Begin getNewItem');

            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                var libraryItem = jobItem.Item;
                LocalAssetManager.createLocalItem(libraryItem, serverInfo, jobItem.OriginalFileName).done(function (localItem) {

                    downloadMedia(apiClient, jobItem, localItem, options).done(function (isQueued) {

                        if (isQueued) {
                            deferred.resolve();
                            return;
                        }

                        getImages(apiClient, jobItem, localItem).done(function () {

                            getSubtitles(apiClient, jobItem, localItem).done(function () {

                                apiClient.reportSyncJobItemTransferred(jobItem.SyncJobItemId).done(function () {

                                    deferred.resolve();

                                }).fail(getOnFail(deferred));

                            }).fail(getOnFail(deferred));

                        }).fail(getOnFail(deferred));

                    }).fail(getOnFail(deferred));

                }).fail(getOnFail(deferred));
            });

            return deferred.promise();
        }

        function downloadMedia(apiClient, jobItem, localItem, options) {

            Logger.log('Begin downloadMedia');
            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                var url = apiClient.getUrl("Sync/JobItems/" + jobItem.SyncJobItemId + "/File", {
                    api_key: apiClient.accessToken()
                });

                var localPath = localItem.LocalPath;

                Logger.log('Downloading media. Url: ' + url + '. Local path: ' + localPath);

                options = options || {};

                LocalAssetManager.downloadFile(url, localPath, options.enableBackgroundTransfer, options.enableNewDownloads).done(function (path, isQueued) {

                    if (isQueued) {
                        deferred.resolveWith(null, [true]);
                        return;
                    }
                    LocalAssetManager.addOrUpdateLocalItem(localItem).done(function () {

                        deferred.resolveWith(null, [false]);

                    }).fail(getOnFail(deferred));

                }).fail(getOnFail(deferred));

            });

            return deferred.promise();
        }

        function getImages(apiClient, jobItem, localItem) {

            Logger.log('Begin getImages');
            var deferred = DeferredBuilder.Deferred();

            getNextImage(0, apiClient, localItem, deferred);

            return deferred.promise();
        }

        function getNextImage(index, apiClient, localItem, deferred) {

            Logger.log('Begin getNextImage');
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

            downloadImage(apiClient, serverId, itemId, imageTag, imageType).done(function () {

                // For the sake of simplicity, limit to one image
                deferred.resolve();
                return;

                getNextImage(index + 1, apiClient, localItem, deferred);

            }).fail(getOnFail(deferred));
        }

        function downloadImage(apiClient, serverId, itemId, imageTag, imageType) {

            Logger.log('Begin downloadImage');
            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                LocalAssetManager.hasImage(serverId, itemId, imageTag).done(function (hasImage) {

                    if (hasImage) {
                        deferred.resolve();
                        return;
                    }

                    var imageUrl = apiClient.getImageUrl(itemId, {
                        tag: imageTag,
                        type: imageType,
                        api_key: apiClient.accessToken()
                    });

                    LocalAssetManager.downloadImage(imageUrl, serverId, itemId, imageTag).done(function () {

                        deferred.resolve();

                    }).fail(getOnFail(deferred));

                });
            });

            return deferred.promise();
        }

        function getSubtitles(apiClient, jobItem, localItem) {

            Logger.log('Begin getSubtitles');
            var deferred = DeferredBuilder.Deferred();

            require(['localassetmanager'], function () {

                if (!jobItem.Item.MediaSources.length) {
                    logger.Error("Cannot download subtitles because video has no media source info.");
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

            getItemSubtitle(file, apiClient, jobItem, localItem, mediaSource).done(function () {

                getNextSubtitle(files, index + 1, apiClient, jobItem, localItem, mediaSource, deferred);

            }).fail(function () {
                getNextSubtitle(files, index + 1, apiClient, jobItem, localItem, mediaSource, deferred);
            });
        }

        function getItemSubtitle(file, apiClient, jobItem, localItem, mediaSource) {

            Logger.log('Begin getItemSubtitle');
            var deferred = DeferredBuilder.Deferred();

            var subtitleStream = mediaSource.MediaStreams.filter(function (m) {
                return m.Type == 'Subtitle' && m.Index == file.Index;
            })[0];

            if (!subtitleStream) {

                // We shouldn't get in here, but let's just be safe anyway
                Logger.log("Cannot download subtitles because matching stream info wasn't found.");
                deferred.reject();
                return;
            }

            var url = apiClient.getUrl("Sync/JobItems/" + jobItem.SyncJobItemId + "/AdditionalFiles", {
                Name: file.Name,
                api_key: apiClient.accessToken()
            });

            require(['localassetmanager'], function () {

                LocalAssetManager.downloadSubtitles(url, localItem, subtitleStream).done(function (subtitlePath) {

                    subtitleStream.Path = subtitlePath;
                    LocalAssetManager.addOrUpdateLocalItem(localItem).done(function () {
                        deferred.resolve();
                    }).fail(getOnFail(deferred));

                }).fail(getOnFail(deferred));
            });

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

                LocalAssetManager.getUserIdsWithAccess(itemId, serverId).done(function (savedUserIdsWithAccess) {

                    var userIdsWithAccess = syncDataResult.ItemUserAccess[itemId];

                    if (userIdsWithAccess.join(',') == savedUserIdsWithAccess.join(',')) {
                        // Hasn't changed, nothing to do
                        deferred.resolve();
                    }
                    else {

                        LocalAssetManager.saveUserIdsWithAccess(itemId, serverId, userIdsWithAccess).done(function () {
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
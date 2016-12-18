define(['localassetmanager'], function (localassetmanager) {
    'use strict';

    function processDownloadStatus(apiClient, serverInfo, options) {

        console.log('[mediasync] Begin processDownloadStatus');

        return localassetmanager.getServerItems(serverInfo.Id).then(function (items) {

            console.log('[mediasync] Begin processDownloadStatus getServerItems completed');

            var progressItems = items.filter(function (item) {
                return item.SyncStatus === 'transferring' || item.SyncStatus === 'queued';
            });

            var p = Promise.resolve();
            var cnt = 0;

            progressItems.forEach(function (item) {
                p = p.then(function () {
                    return reportTransfer(apiClient, item);
                });
                cnt++;
            });

            return p.then(function () {
                console.log('[mediasync] Exit processDownloadStatus. Items reported: ' + cnt.toString());
                return Promise.resolve();
            });
        });
    }

    function reportTransfer(apiClient, item) {

        return localassetmanager.getItemFileSize(item.LocalPath).then(function (size) {
            // The background transfer service on Windows leaves the file empty (size = 0) until it 
            // has been downloaded completely
            if (size > 0) {
                return apiClient.reportSyncJobItemTransferred(item.SyncJobItemId).then(function () {
                    item.SyncStatus = 'synced';
                    return localassetmanager.addOrUpdateLocalItem(item);
                }, function (error) {
                    console.error("[mediasync] Mediasync error on reportSyncJobItemTransferred", error);
                    item.SyncStatus = 'error';
                    return localassetmanager.addOrUpdateLocalItem(item);
                });
            } else {
                return localassetmanager.isDownloadInQueue(item.SyncJobItemId).then(function (result) {
                    if (result) {
                        // just wait for completion
                        return Promise.resolve();
                    }

                    console.log("[mediasync] reportTransfer: Size is 0 and download no longer in queue. Deleting item.");
                    return localassetmanager.removeLocalItem(item).then(function () {
                        console.log('[mediasync] reportTransfer: Item deleted.');
                        return Promise.resolve();
                    }, function (err2) {
                        console.log('[mediasync] reportTransfer: Failed to delete item.', error);
                        return Promise.resolve();
                    });
                });
            }

        }, function (error) {

            console.error('[mediasync] reportTransfer: error on getItemFileSize. Deleting item.', error);
            return localassetmanager.removeLocalItem(item).then(function () {
                console.log('[mediasync] reportTransfer: Item deleted.');
                return Promise.resolve();
            }, function (err2) {
                console.log('[mediasync] reportTransfer: Failed to delete item.', error);
                return Promise.resolve();
            });
        });
    }

    function reportOfflineActions(apiClient, serverInfo) {

        console.log('[mediasync] Begin reportOfflineActions');

        return localassetmanager.getUserActions(serverInfo.Id).then(function (actions) {

            if (!actions.length) {
                console.log('[mediasync] Exit reportOfflineActions (no actions)');
                return Promise.resolve();
            }

            return apiClient.reportOfflineActions(actions).then(function () {

                return localassetmanager.deleteUserActions(actions).then(function () {
                    console.log('[mediasync] Exit reportOfflineActions (actions reported and deleted.)');
                    return Promise.resolve();
                });

            }, function (err) {

                // delete those actions even on failure, because if the error is caused by 
                // the action data itself, this could otherwise lead to a situation that 
                // never gets resolved
                console.error('[mediasync] error on apiClient.reportOfflineActions: ' + err.toString());
                return localassetmanager.deleteUserActions(actions);
            });
        });
    }

    function syncData(apiClient, serverInfo, syncUserItemAccess) {

        console.log('[mediasync] Begin syncData');

        return localassetmanager.getServerItems(serverInfo.Id).then(function (items) {

            var completedItems = items.filter(function (item) {
                return (item) && ((item.SyncStatus === 'synced') || (item.SyncStatus === 'error'));
            });

            var request = {
                TargetId: apiClient.deviceId(),
                LocalItemIds: completedItems.map(function (xitem) { return xitem.ItemId; }),
                OfflineUserIds: (serverInfo.Users || []).map(function (u) { return u.Id; })
            };

            return apiClient.syncData(request).then(function (result) {

                return afterSyncData(apiClient, serverInfo, syncUserItemAccess, result).then(function () {
                    return Promise.resolve();
                }, function () {
                    return Promise.resolve();
                });

            });
        });
    }

    function afterSyncData(apiClient, serverInfo, enableSyncUserItemAccess, syncDataResult) {

        console.log('[mediasync] Begin afterSyncData');

        var p = Promise.resolve();

        if (syncDataResult.ItemIdsToRemove) {
            syncDataResult.ItemIdsToRemove.forEach(function (itemId) {
                p = p.then(function () {
                    return removeLocalItem(itemId, serverInfo.Id);
                });
            });
        }

        if (enableSyncUserItemAccess) {
            p = p.then(function () {
                return syncUserItemAccess(syncDataResult, serverInfo.Id);
            });
        }

        return p.then(function () {
            console.log('[mediasync] Exit afterSyncData');
            return Promise.resolve();
        });
    }

    function removeLocalItem(itemId, serverId) {

        console.log('[mediasync] Begin removeLocalItem');

        return localassetmanager.getLocalItem(serverId, itemId).then(function (item) {

            if (item) {
                return localassetmanager.removeLocalItem(item);
            }

            return Promise.resolve();

        });
    }

    function getNewMedia(apiClient, serverInfo, options) {

        console.log('[mediasync] Begin getNewMedia');

        return apiClient.getReadySyncItems(apiClient.deviceId()).then(function (jobItems) {

            var p = Promise.resolve();

            jobItems.forEach(function (jobItem) {
                p = p.then(function () {
                    return getNewItem(jobItem, apiClient, serverInfo, options);
                });
            });

            return p.then(function () {
                console.log('[mediasync] Exit getNewMedia');
                return Promise.resolve();
            });
        });
    }

    function getNewItem(jobItem, apiClient, serverInfo, options) {

        console.log('[mediasync] Begin getNewItem');

        var libraryItem = jobItem.Item;

        return localassetmanager.getLocalItem(serverInfo.Id, libraryItem.Id).then(function (existingItem) {

            console.log('[mediasync] getNewItem.getLocalItem completed');

            if (existingItem) {
                if (existingItem.SyncStatus === 'queued' || existingItem.SyncStatus === 'transferring' || existingItem.SyncStatus === 'synced') {
                    console.log('[mediasync] getNewItem.getLocalItem found existing item');
                    return Promise.resolve();
                }
            }

            console.log('[mediasync] getNewItem.getLocalItem no existing item found');

            return localassetmanager.createLocalItem(libraryItem, serverInfo, jobItem).then(function (localItem) {

                console.log('[mediasync] getNewItem.createLocalItem completed');

                localItem.SyncStatus = 'queued';

                return downloadMedia(apiClient, jobItem, localItem, options).then(function () {

                    return getImages(apiClient, jobItem, localItem).then(function () {

                        return getSubtitles(apiClient, jobItem, localItem);

                    });
                });
            });
        });
    }

    function downloadMedia(apiClient, jobItem, localItem, options) {

        console.log('[mediasync] Begin downloadMedia');

        var url = apiClient.getUrl("Sync/JobItems/" + jobItem.SyncJobItemId + "/File", {
            api_key: apiClient.accessToken()
        });

        var localPath = localItem.LocalPath;

        console.log('[mediasync] Downloading media. Url: ' + url + '. Local path: ' + localPath);

        options = options || {};

        return localassetmanager.downloadFile(url, localItem).then(function (filename) {

            localItem.SyncStatus = 'transferring';
            return localassetmanager.addOrUpdateLocalItem(localItem);
        });
    }

    function getImages(apiClient, jobItem, localItem) {

        console.log('[mediasync] Begin getImages');

        return getNextImage(0, apiClient, localItem);
    }

    function getNextImage(index, apiClient, localItem) {

        console.log('[mediasync] Begin getNextImage');

        //if (index >= 4) {

        //    deferred.resolve();
        //    return;
        //}

        // Just for now while media syncing gets worked out
        return Promise.resolve();

        //var libraryItem = localItem.Item;

        //var serverId = libraryItem.ServerId;
        //var itemId = null;
        //var imageTag = null;
        //var imageType = "Primary";

        //switch (index) {

        //    case 0:
        //        itemId = libraryItem.Id;
        //        imageType = "Primary";
        //        imageTag = (libraryItem.ImageTags || {})["Primary"];
        //        break;
        //    case 1:
        //        itemId = libraryItem.SeriesId;
        //        imageType = "Primary";
        //        imageTag = libraryItem.SeriesPrimaryImageTag;
        //        break;
        //    case 2:
        //        itemId = libraryItem.SeriesId;
        //        imageType = "Thumb";
        //        imageTag = libraryItem.SeriesPrimaryImageTag;
        //        break;
        //    case 3:
        //        itemId = libraryItem.AlbumId;
        //        imageType = "Primary";
        //        imageTag = libraryItem.AlbumPrimaryImageTag;
        //        break;
        //    default:
        //        break;
        //}

        //if (!itemId || !imageTag) {
        //    getNextImage(index + 1, apiClient, localItem, deferred);
        //    return;
        //}

        //downloadImage(apiClient, serverId, itemId, imageTag, imageType).then(function () {

        //    // For the sake of simplicity, limit to one image
        //    deferred.resolve();
        //    return;

        //    getNextImage(index + 1, apiClient, localItem, deferred);

        //}, getOnFail(deferred));
    }

    function downloadImage(apiClient, serverId, itemId, imageTag, imageType) {

        console.log('[mediasync] Begin downloadImage');

        return localassetmanager.hasImage(serverId, itemId, imageTag).then(function (hasImage) {

            if (hasImage) {
                return Promise.resolve();
            }

            var imageUrl = apiClient.getImageUrl(itemId, {
                tag: imageTag,
                type: imageType,
                api_key: apiClient.accessToken()
            });

            return localassetmanager.downloadImage(imageUrl, serverId, itemId, imageTag);
        });
    }

    function getSubtitles(apiClient, jobItem, localItem) {

        console.log('[mediasync] Begin getSubtitles');

        if (!jobItem.Item.MediaSources.length) {
            console.log("[mediasync] Cannot download subtitles because video has no media source info.");
            return Promise.resolve();
        }

        var files = jobItem.AdditionalFiles.filter(function (f) {
            return f.Type === 'Subtitles';
        });

        var mediaSource = jobItem.Item.MediaSources[0];

        var p = Promise.resolve();

        files.forEach(function (file) {
            p = p.then(function () {
                return getItemSubtitle(file, apiClient, jobItem, localItem, mediaSource);
            });
        });

        return p.then(function () {
            console.log('[mediasync] Exit getSubtitles');
            return Promise.resolve();
        });
    }

    function getItemSubtitle(file, apiClient, jobItem, localItem, mediaSource) {

        console.log('[mediasync] Begin getItemSubtitle');

        var subtitleStream = mediaSource.MediaStreams.filter(function (m) {
            return m.Type === 'Subtitle' && m.Index === file.Index;
        })[0];

        if (!subtitleStream) {

            // We shouldn't get in here, but let's just be safe anyway
            console.log("[mediasync] Cannot download subtitles because matching stream info wasn't found.");
            return Promise.resolve();
        }

        var url = apiClient.getUrl("Sync/JobItems/" + jobItem.SyncJobItemId + "/AdditionalFiles", {
            Name: file.Name,
            api_key: apiClient.accessToken()
        });

        var fileName = localassetmanager.getSubtitleSaveFileName(jobItem.OriginalFileName, subtitleStream.Language, subtitleStream.IsForced, subtitleStream.Codec);
        var localFilePath = localassetmanager.getLocalFilePath(localItem, fileName);

        return localassetmanager.downloadSubtitles(url, localFilePath).then(function (subtitlePath) {

            subtitleStream.Path = subtitlePath;
            return localassetmanager.addOrUpdateLocalItem(localItem);
        });
    }

    return function () {

        var self = this;

        self.sync = function (apiClient, serverInfo, options) {

            console.log("[mediasync]************************************* Start sync");

            return processDownloadStatus(apiClient, serverInfo, options).then(function () {

                return reportOfflineActions(apiClient, serverInfo).then(function () {

                    //// Do the first data sync
                    //return syncData(apiClient, serverInfo, false).then(function () {

                    // Download new content
                    return getNewMedia(apiClient, serverInfo, options).then(function () {

                        // Do the second data sync
                        return syncData(apiClient, serverInfo, false).then(function () {
                            console.log("[mediasync]************************************* Exit sync");
                        });
                    });
                    //});
                });
            });
        };
    };

    //function syncUserItemAccess(syncDataResult, serverId) {
    //    console.log('[mediasync] Begin syncUserItemAccess');

    //    var itemIds = [];
    //    for (var id in syncDataResult.ItemUserAccess) {
    //        itemIds.push(id);
    //    }

    //    return syncNextUserAccessForItem(itemIds, 0, syncDataResult, serverId);
    //}

    //function syncNextUserAccessForItem(itemIds, index, syncDataResult, serverId) {

    //    var length = itemIds.length;

    //    if (index >= length) {

    //        return Promise.resolve
    //        return;
    //    }

    //    syncUserAccessForItem(itemIds[index], syncDataResult).then(function () {

    //        syncNextUserAccessForItem(itemIds, index + 1, syncDataResult, serverId, deferred);
    //    }, function () {
    //        syncNextUserAccessForItem(itemIds, index + 1, syncDataResult, serverId, deferred);
    //    });
    //}

    //function syncUserAccessForItem(itemId, syncDataResult) {
    //    console.log('[mediasync] Begin syncUserAccessForItem');

    //    var deferred = DeferredBuilder.Deferred();

    //    localassetmanager.getUserIdsWithAccess(itemId, serverId).then(function (savedUserIdsWithAccess) {

    //        var userIdsWithAccess = syncDataResult.ItemUserAccess[itemId];

    //        if (userIdsWithAccess.join(',') === savedUserIdsWithAccess.join(',')) {
    //            // Hasn't changed, nothing to do
    //            deferred.resolve();
    //        }
    //        else {

    //            localassetmanager.saveUserIdsWithAccess(itemId, serverId, userIdsWithAccess).then(function () {
    //                deferred.resolve();
    //            }, getOnFail(deferred));
    //        }

    //    }, getOnFail(deferred));

    //    return deferred.promise();
    //}

    //}

});
define(['filerepository', 'itemrepository', 'userrepository', 'useractionrepository', 'transfermanager', 'cryptojs-md5'], function (filerepository, itemrepository, userrepository, useractionrepository, transfermanager) {
    'use strict';

    function getLocalItem(serverId, itemId) {
        var id = getLocalId(serverId, itemId);
        return itemrepository.get(id);
    }

    function getLocalItemById(id) {
        return itemrepository.get(id);
    }

    function getLocalId(serverId, itemId) {

        return CryptoJS.MD5(serverId + itemId).toString();
    }

    function saveOfflineUser(user) {
        return userrepository.set(user.Id, user);
    }

    function deleteOfflineUser(id) {
        return userrepository.remove(id);
    }

    function recordUserAction(action) {

        action.Id = createGuid();
        return useractionrepository.set(action.Id, action);
    }

    function getUserActions(serverId) {
        return useractionrepository.getByServerId(serverId);
    }

    function deleteUserAction(action) {
        return useractionrepository.remove(action.Id);
    }

    function deleteUserActions(actions) {
        var results = [];

        actions.forEach(function (action) {
            results.push(deleteUserAction(action));
        });

        return Promise.all(results);
    }

    function getServerItemIds(serverId) {
        return itemrepository.getServerItemIds(serverId);
    }

    function getServerItems(serverId) {

        return itemrepository.getServerIds(serverId).then(function (localIds) {

            var actions = localIds.map(function (id) {
                return getLocalItemById(id);
            });

            return Promise.all(actions).then(function (items) {

                return Promise.resolve(items);
            });
        });
    }

    function getViews(serverId, userId) {

        return itemrepository.getServerItemTypes(serverId, userId).then(function (types) {

            var list = [];
            var item;

            if (types.indexOf('audio') > -1) {

                item = {
                    Name: 'Music',
                    ServerId: serverId,
                    Id: 'localview:MusicView',
                    Type: 'MusicView',
                    CollectionType: 'Music',
                    IsFolder: true
                };

                list.push(item);
            }

            if (types.indexOf('photo') > -1) {

                item = {
                    Name: 'Photos',
                    ServerId: serverId,
                    Id: 'localview:PhotosView',
                    Type: 'PhotosView',
                    CollectionType: 'Photos',
                    IsFolder: true
                };

                list.push(item);
            }

            if (types.indexOf('episode') > -1) {

                item = {
                    Name: 'TV',
                    ServerId: serverId,
                    Id: 'localview:TVView',
                    Type: 'TVView',
                    CollectionType: 'TvShows',
                    IsFolder: true
                };

                list.push(item);
            }

            if (types.indexOf('video') > -1 ||
                types.indexOf('movie') > -1 ||
                types.indexOf('musicvideo') > -1) {

                item = {
                    Name: 'Videos',
                    ServerId: serverId,
                    Id: 'localview:VideosView',
                    Type: 'VideosView',
                    CollectionType: 'HomeVideos',
                    IsFolder: true
                };

                list.push(item);
            }

            return Promise.resolve(list);
        });
    }

    function getViewItems(serverId, userId, parentId) {

        return getServerItems(serverId).then(function (items) {

            var resultItems = items.filter(function (item) {

                var type = (item.Item.Type || '').toLowerCase();

                switch (parentId) {
                    case 'localview:MusicView':
                        return type === 'audio';
                    case 'localview:PhotosView':
                        return type === 'photo';
                    case 'localview:TVView':
                        return type === 'episode';
                    case 'localview:VideosView':
                        return type === 'movie' || type === 'video' || type === 'musicvideo';
                    default:
                        return false;
                }
            }).map(function (item2) {
                return item2.Item;
            });

            return Promise.resolve(resultItems);
        });
    }


    function removeLocalItem(localItem) {

        return itemrepository.get(localItem.Id).then(function (item) {
            return filerepository.deleteFile(item.LocalPath).then(function () {

                var p = Promise.resolve(true);

                if (item.AdditionalFiles) {
                    item.AdditionalFiles.forEach(function (file) {
                        p = p.then(function () {
                            return filerepository.deleteFile(file.Path);
                        });
                    });
                }

                return p.then(function (file) {
                    return itemrepository.remove(localItem.Id);
                });

            }, function (error) {

                var p = Promise.resolve(true);

                if (item.AdditionalFiles) {
                    item.AdditionalFiles.forEach(function (file) {
                        p = p.then(function (item) {
                            return filerepository.deleteFile(file.Path);
                        });
                    });
                }

                return p.then(function (file) {
                    return itemrepository.remove(localItem.Id);
                });
            });
        });
    }

    function addOrUpdateLocalItem(localItem) {
        return itemrepository.set(localItem.Id, localItem);
    }

    function createLocalItem(libraryItem, serverInfo, jobItem) {

        var path = getDirectoryPath(libraryItem, serverInfo);
        var localFolder = filerepository.getFullLocalPath(path);

        path.push(getLocalFileName(libraryItem, jobItem.OriginalFileName));

        var localPath = filerepository.getFullLocalPath(path);

        for (var i = 0; i < libraryItem.MediaSources.length; i++) {
            var mediaSource = libraryItem.MediaSources[i];
            mediaSource.Path = localPath;
            mediaSource.Protocol = 'File';
        }

        var item = {

            Item: libraryItem,
            ItemId: libraryItem.Id,
            ServerId: serverInfo.Id,
            LocalPath: localPath,
            LocalFolder: localFolder,
            AdditionalFiles: jobItem.AdditionalFiles.slice(0),
            Id: getLocalId(serverInfo.Id, libraryItem.Id),
            SyncJobItemId: jobItem.SyncJobItemId
        };

        return Promise.resolve(item);
    }

    function getSubtitleSaveFileName(localItem, mediaPath, language, isForced, format) {

        var name = getNameWithoutExtension(mediaPath);

        if (language) {
            name += "." + language.toLowerCase();
        }

        if (isForced) {
            name += ".foreign";
        }

        name = name + "." + format.toLowerCase();

        var localPathArray = [localItem.LocalFolder, name];
        var localFilePath = filerepository.getPathFromArray(localPathArray);

        return localFilePath;

    }

    function getItemFileSize(path) {
        return filerepository.getItemFileSize(path);
    }

    function getNameWithoutExtension(path) {

        var fileName = path;

        var pos = fileName.lastIndexOf(".");

        if (pos > 0) {
            fileName = fileName.substring(0, pos);
        }

        return fileName;
    }

    function downloadFile(url, localItem) {

        var folder = filerepository.getLocalPath();
        return transfermanager.downloadFile(url, folder, localItem);
    }

    function downloadSubtitles(url, fileName) {

        var folder = filerepository.getLocalPath();
        return transfermanager.downloadSubtitles(url, folder, fileName);
    }

    function getImageUrl(serverId, itemId, imageType, index) {

        var pathArray = getImagePath(serverId, itemId, imageType, index);
        var relPath = pathArray.join('/');

        var prefix = 'ms-appdata:///local';
        return prefix + '/' + relPath;
    }

    function hasImage(serverId, itemId, imageType, index) {

        var pathArray = getImagePath(serverId, itemId, imageType, index);
        var localFilePath = filerepository.getFullMetadataPath(pathArray);

        return filerepository.fileExists(localFilePath).then(function (exists) {
            // TODO: Maybe check for broken download when file size is 0 and item is not queued
            ////if (exists) {
            ////    if (!transfermanager.isDownloadFileInQueue(localFilePath)) {
            ////        // If file exists but 
            ////        exists = false;
            ////    }
            ////}

            return Promise.resolve(exists);
        }, function (err) {
            return Promise.resolve(false);
        });
    }

    function downloadImage(localItem, url, serverId, itemId, imageType, index) {

        var pathArray = getImagePath(serverId, itemId, imageType, index);
        var localFilePath = filerepository.getFullMetadataPath(pathArray);

        if (!localItem.AdditionalFiles) {
            localItem.AdditionalFiles = [];
        }

        var fileInfo = {
            Path: localFilePath,
            Type: 'Image',
            Name: imageType + index.toString(),
            ImageType: imageType
        };

        localItem.AdditionalFiles.push(fileInfo);

        var folder = filerepository.getMetadataPath();
        return transfermanager.downloadImage(url, folder, localFilePath);
    }

    function isDownloadFileInQueue(path) {

        return transfermanager.isDownloadFileInQueue(path);
    }

    function translateFilePath(path) {
        return Promise.resolve(path);
    }

    // Helpers ***********************************************************

    function getDirectoryPath(item, server) {

        var parts = [];
        parts.push(server.Name);

        var itemtype = item.Type.toLowerCase();

        if (itemtype === 'episode') {

            parts.push("TV");

            var seriesName = item.SeriesName;
            if (seriesName) {
                parts.push(seriesName);
            }

            var seasonName = item.SeasonName;
            if (seasonName) {
                parts.push(seasonName);
            }

        } else if (itemtype === 'video') {

            parts.push("Videos");
            parts.push(item.Name);

        } else if (itemtype === 'audio') {

            parts.push("Music");

            var albumArtist = item.AlbumArtist;
            if (albumArtist) {
                parts.push(albumArtist);
            }

            if ((item.AlbumId) && (item.Album)) {
                parts.push(item.Album);
            }

        } else if (itemtype === 'photo') {

            parts.push("Photos");

            if ((item.AlbumId) && (item.Album)) {
                parts.push(item.Album);
            }

        }

        var finalParts = [];
        for (var i = 0; i < parts.length; i++) {

            finalParts.push(filerepository.getValidFileName(parts[i]));
        }

        return finalParts;
    }

    function getImagePath(serverId, itemId, imageType, index) {

        var parts = [];
        parts.push('Metadata');
        parts.push(serverId);
        parts.push('images');
        parts.push(itemId + '_' + imageType + '_' + index.toString() + '.png');

        var finalParts = [];
        for (var i = 0; i < parts.length; i++) {

            finalParts.push(filerepository.getValidFileName(parts[i]));
        }

        return finalParts;
    }

    function getLocalFileName(item, originalFileName) {

        var filename = originalFileName || item.Name;

        return filerepository.getValidFileName(filename);
    }

    function resyncTransfers() {
        return transfermanager.resyncTransfers();
    }

    function createGuid() {
        var d = new Date().getTime();
        if (window.performance && typeof window.performance.now === "function") {
            d += performance.now(); //use high-precision timer if available
        }
        var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = (d + Math.random() * 16) % 16 | 0;
            d = Math.floor(d / 16);
            return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
        });
        return uuid;
    }

    return {

        getLocalItem: getLocalItem,
        saveOfflineUser: saveOfflineUser,
        deleteOfflineUser: deleteOfflineUser,
        recordUserAction: recordUserAction,
        getUserActions: getUserActions,
        deleteUserAction: deleteUserAction,
        deleteUserActions: deleteUserActions,
        getServerItemIds: getServerItemIds,
        removeLocalItem: removeLocalItem,
        addOrUpdateLocalItem: addOrUpdateLocalItem,
        createLocalItem: createLocalItem,
        downloadFile: downloadFile,
        downloadSubtitles: downloadSubtitles,
        hasImage: hasImage,
        downloadImage: downloadImage,
        getImageUrl: getImageUrl,
        translateFilePath: translateFilePath,
        getSubtitleSaveFileName: getSubtitleSaveFileName,
        getLocalItemById: getLocalItemById,
        getServerItems: getServerItems,
        getItemFileSize: getItemFileSize,
        isDownloadFileInQueue: isDownloadFileInQueue,
        getViews: getViews,
        getViewItems: getViewItems,
        resyncTransfers: resyncTransfers
    };
});
(function () {

    function getLocalMediaSource(serverId, itemId) {

        var deferred = DeferredBuilder.Deferred();

        // android
        if (window.ApiClientBridge) {
            var json = ApiClientBridge.getLocalMediaSource(serverId, itemId);

            if (json) {
                deferred.resolveWith(null, [JSON.parse(json)]);
            }
            else {
                deferred.resolveWith(null, [null]);
            }
            return deferred.promise();
        }

        getLocalItem(itemId, serverId).done(function (localItem) {

            if (localItem && localItem.MediaSources.length) {

                var mediaSource = localItem.MediaSources[0];

                fileExists(mediaSource.Path).done(function (exists) {

                    if (exists) {
                        deferred.resolveWith(null, [mediaSource]);
                    }
                    else {
                        deferred.resolveWith(null, [null]);
                    }

                }).fail(getOnFail(deferred));
                return;
            }

            deferred.resolveWith(null, [null]);

        }).fail(getOnFail(deferred));
        return deferred.promise();
    }

    function getCameraPhotos() {
        var deferred = DeferredBuilder.Deferred();

        if (window.CameraRoll) {

            var photos = [];

            CameraRoll.getPhotos(function (result) {
                photos.push(result);
            });

            setTimeout(function () {

                // clone the array in case the callback is still getting called
                Logger.log('Found ' + photos.length + ' in camera roll');

                deferred.resolveWith(null, [photos]);

            }, 2000);

        } else {
            deferred.resolveWith(null, [[]]);
        }
        return deferred.promise();
    }

    function saveOfflineUser(user) {

        // TODO
        var deferred = DeferredBuilder.Deferred();
        deferred.resolve();
        return deferred.promise();
    }

    function getOfflineActions(serverId) {
        // TODO
        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, [[]]);
        return deferred.promise();
    }

    function getServerItemIds(serverId) {
        // TODO
        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, [[]]);
        return deferred.promise();
    }

    function removeLocalItem(itemId, serverId) {
        // TODO
        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, []);
        return deferred.promise();
    }

    function getLocalItem(itemId, serverId) {
        // TODO
        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, []);
        return deferred.promise();
    }

    function addOrUpdateLocalItem(localItem) {
        // TODO
        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, [null]);
        return deferred.promise();
    }

    function createLocalItem(libraryItem, serverInfo, originalFileName) {

        var path = getDirectoryPath(libraryItem, serverInfo);
        path.push(getLocalFileName(libraryItem, originalFileName));

        var item = {};

        var deferred = DeferredBuilder.Deferred();

        getFileSystem().done(function (fileSystem) {

            var localPath = fileSystem.root.toURL() + "/" + path.join('/');

            item.LocalPath = localPath;

            for (var i = 0, length = libraryItem.MediaSources.length; i < length; i++) {

                var mediaSource = libraryItem.MediaSources[i];
                mediaSource.setPath(localPath);
                mediaSource.setProtocol(MediaProtocol.File);
            }

            item.ServerId = serverInfo.Id;
            item.Item = libraryItem;
            item.ItemId = libraryItem.Id;
            item.Id = getLocalId(item.ServerId, item.ItemId);
            deferred.resolveWith(null, [item]);
        });

        return deferred.promise();
    }

    function getDirectoryPath(item, serverInfo) {

        var parts = [];
        parts.push(server.Name);

        if (item.Type == "Episode") {
            parts.push("TV");
            parts.push(item.SeriesName);

            if (item.SeasonName) {
                parts.push(item.SeasonName);
            }
        }
        else if (item.MediaType == 'Video') {
            parts.push("Videos");
            parts.push(item.Name);
        }
        else if (item.MediaType == 'Audio') {
            parts.push("Music");

            if (item.AlbumArtist) {
                parts.push(item.AlbumArtist);
            }

            if (item.Album) {
                parts.push(item.Album);
            }
        }
        else if (item.MediaType == 'Photo') {
            parts.push("Photos");

            if (item.Album) {
                parts.push(item.Album);
            }
        }

        return parts.map(getValidFileName);
    }

    function getLocalFileName(libraryItem, originalFileName) {

        var filename = originalFileName || libraryItem.Name;

        return fileRepository.getValidFileName(filename);
    }

    function getValidFileName(filename) {
        // TODO
        return filename;
    }

    function downloadFile(url, localPath) {

        var deferred = DeferredBuilder.Deferred();

        Logger.log('downloading: ' + url + ' to ' + localPath);
        var ft = new FileTransfer();
        ft.download(url, localPath, function (entry) {

            var localUrl = normalizeReturnUrl(entry.toURL());

            Logger.log('Downloaded local url: ' + localUrl);
            deferred.resolveWith(null, [localUrl]);
        });

        return deferred.promise();
    }

    function downloadSubtitles(url, localItem, subtitleStream) {

        var path = item.LocalPath;

        var filename = getSubtitleSaveFileName(item, subtitleStream.Language, subtitleStream.IsForced) + "." + subtitleStream.Codec.toLowerCase();

        var parentPath = getParentDirectoryPath(path);

        path = combinePaths(parentPath, filename);

        return downloadFile(url, path);
    }

    function getSubtitleSaveFileName(item, language, isForced) {

        var path = item.LocalPath;

        var name = getNameWithoutExtension(path);

        if (language) {
            name += "." + language.toLowerCase();
        }

        if (isForced) {
            name += ".foreign";
        }

        return name;
    }

    function getNameWithoutExtension(path) {

        var parts = path.split('/');
        var name = parts[parts.length - 1];

        var index = name.lastIndexOf('.');

        if (index != -1) {
            name = name.substring(0, index);
        }

        return name;
    }

    function getParentDirectoryPath(path) {

        var parts = path.split('/');
        parts.length--;

        return parts.join('/');
    }

    function combinePaths(path1, path2) {

        return path1 + path2;
    }

    function getLocalId(serverId, itemId) {

    }

    function hasImage(serverId, itemId, imageTag) {

        var deferred = DeferredBuilder.Deferred();
        getImageLocalPath(serverId, itemId, imageTag).done(function (localPath) {

            fileExists(localPath).done(function (exists) {

                deferred.resolveWith(null, [exists]);

            }).fail(getOnFail(deferred));

        }).fail(getOnFail(deferred));
        return deferred.promise();
    }

    function downloadImage(url, serverId, itemId, imageTag) {

        var deferred = DeferredBuilder.Deferred();
        getImageLocalPath(serverId, itemId, imageTag).done(function (localPath) {

            downloadFile(url, localPath).done(function () {

                deferred.resolve();

            }).fail(getOnFail(deferred));

        }).fail(getOnFail(deferred));
        return deferred.promise();
    }

    function getImageLocalPath(serverId, itemId, imageTag) {
        var deferred = DeferredBuilder.Deferred();

        getFileSystem().done(function (fileSystem) {
            var path = fileSystem.root.toURL() + "/emby/images/" + serverId + "/" + itemId + "/" + imageTag;

            deferred.resolveWith(null, [path]);
        });

        return deferred.promise();
    }

    function fileExists(path) {

        var deferred = DeferredBuilder.Deferred();

        resolveLocalFileSystemURL(path, function (fileEntry) {

            deferred.resolveWith(null, [true]);

        }, function () {

            deferred.resolveWith(null, [false]);
        });

        return deferred.promise();
    }

    var fileSystem;
    function getFileSystem() {

        var deferred = DeferredBuilder.Deferred();

        if (fileSystem) {
            deferred.resolveWith(null, [fileSystem]);
        } else {
            requestFileSystem(PERSISTENT, 0, function (fs) {
                fileSystem = fs;
                deferred.resolveWith(null, [fileSystem]);
            });
        }

        return deferred.promise();
    }

    function getOnFail(deferred) {
        return function () {

            deferred.reject();
        };
    }

    window.LocalAssetManager = {
        getLocalMediaSource: getLocalMediaSource,
        saveOfflineUser: saveOfflineUser,
        getCameraPhotos: getCameraPhotos,
        getOfflineActions: getOfflineActions,
        getServerItemIds: getServerItemIds,
        removeLocalItem: removeLocalItem,
        getLocalItem: getLocalItem,
        addOrUpdateLocalItem: addOrUpdateLocalItem,
        createLocalItem: createLocalItem,
        downloadFile: downloadFile,
        downloadSubtitles: downloadSubtitles,
        hasImage: hasImage,
        downloadImage: downloadImage
    };

})();
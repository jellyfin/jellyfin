(function () {

    function getLocalMediaSource(serverId, itemId) {

        // android
        if (window.ApiClientBridge) {
            var json = ApiClientBridge.getLocalMediaSource(serverId, itemId);

            if (json) {
                return JSON.parse(json);
            }
        }

        return null;
    }

    function saveOfflineUser(user) {
        var deferred = DeferredBuilder.Deferred();
        deferred.resolve();
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

    function getOfflineActions(serverId) {
        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, [[]]);
        return deferred.promise();
    }

    function getServerItemIds(serverId) {
        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, [[]]);
        return deferred.promise();
    }

    function removeLocalItem(itemId, serverId) {
        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, []);
        return deferred.promise();
    }

    function getLocalItem(itemId, serverId) {
        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, []);
        return deferred.promise();
    }

    function addOrUpdateLocalItem(localItem) {
        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, []);
        return deferred.promise();
    }

    function createLocalItem(libraryItem, serverId, originalFileName) {

        var item = {};

        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, [item]);
        return deferred.promise();
    }

    function downloadFile(url, localPath) {

        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, []);
        return deferred.promise();
    }

    function downloadSubtitles(url, localItem, subtitleStreamh) {

        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, [""]);
        return deferred.promise();
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
        downloadSubtitles: downloadSubtitles
    };

})();
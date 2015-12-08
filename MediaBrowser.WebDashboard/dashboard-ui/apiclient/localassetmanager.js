(function () {

    function getLocalMediaSource(serverId, itemId) {
        return new Promise(function (resolve, reject) {
            resolve(null);
        });
    }

    function saveOfflineUser(user) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }

    function deleteOfflineUser(id) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }

    function getCameraPhotos() {
        return new Promise(function (resolve, reject) {
            resolve([]);
        });
    }

    function getOfflineActions(serverId) {
        return new Promise(function (resolve, reject) {
            resolve([]);
        });
    }

    function deleteOfflineActions(actions) {
        return new Promise(function (resolve, reject) {
            resolve([]);
        });
    }

    function getServerItemIds(serverId) {
        return new Promise(function (resolve, reject) {
            resolve([]);
        });
    }

    function removeLocalItem(itemId, serverId) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }

    function getLocalItem(itemId, serverId) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }

    function addOrUpdateLocalItem(localItem) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }

    function createLocalItem(libraryItem, serverInfo, originalFileName) {

        return new Promise(function (resolve, reject) {
            resolve({});
        });
    }

    function downloadFile(url, localPath) {

        return new Promise(function (resolve, reject) {
            resolve();
        });
    }

    function downloadSubtitles(url, localItem, subtitleStreamh) {

        return new Promise(function (resolve, reject) {
            resolve("");
        });
    }

    function hasImage(serverId, itemId, imageTag) {
        return new Promise(function (resolve, reject) {
            resolve(false);
        });
    }

    function downloadImage(url, serverId, itemId, imageTag) {
        return new Promise(function (resolve, reject) {
            resolve(false);
        });
    }

    function fileExists(path) {

        return new Promise(function (resolve, reject) {
            resolve(false);
        });
    }

    function translateFilePath(path) {

        return new Promise(function (resolve, reject) {
            resolve(path);
        });
    }

    window.LocalAssetManager = {
        getLocalMediaSource: getLocalMediaSource,
        saveOfflineUser: saveOfflineUser,
        deleteOfflineUser: deleteOfflineUser,
        getCameraPhotos: getCameraPhotos,
        getOfflineActions: getOfflineActions,
        deleteOfflineActions: deleteOfflineActions,
        getServerItemIds: getServerItemIds,
        removeLocalItem: removeLocalItem,
        getLocalItem: getLocalItem,
        addOrUpdateLocalItem: addOrUpdateLocalItem,
        createLocalItem: createLocalItem,
        downloadFile: downloadFile,
        downloadSubtitles: downloadSubtitles,
        hasImage: hasImage,
        downloadImage: downloadImage,
        fileExists: fileExists,
        translateFilePath: translateFilePath
    };

})();
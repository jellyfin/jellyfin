define([], function () {

    function getLocalMediaSource(serverId, itemId) {
        return Promise.resolve(null);
    }

    function saveOfflineUser(user) {
        return Promise.resolve();
    }

    function deleteOfflineUser(id) {
        return Promise.resolve();
    }

    function getCameraPhotos() {
        return Promise.resolve([]);
    }

    function getOfflineActions(serverId) {
        return Promise.resolve([]);
    }

    function deleteOfflineActions(actions) {
        return Promise.resolve([]);
    }

    function getServerItemIds(serverId) {
        return Promise.resolve([]);
    }

    function removeLocalItem(itemId, serverId) {
        return Promise.resolve();
    }

    function getLocalItem(itemId, serverId) {
        return Promise.resolve();
    }

    function addOrUpdateLocalItem(localItem) {
        return Promise.resolve();
    }

    function createLocalItem(libraryItem, serverInfo, originalFileName) {

        return Promise.resolve({});
    }

    function downloadFile(url, localPath) {

        return Promise.resolve();
    }

    function downloadSubtitles(url, localItem, subtitleStreamh) {

        return Promise.resolve('');
    }

    function hasImage(serverId, itemId, imageTag) {
        return Promise.resolve(false);
    }

    function downloadImage(url, serverId, itemId, imageTag) {
        return Promise.resolve(false);
    }

    function fileExists(path) {
        return Promise.resolve(false);
    }

    function translateFilePath(path) {
        return Promise.resolve(path);
    }

    return {
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
});
define([], function () {
    'use strict';

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

    function recordUserAction(action) {
        return Promise.resolve();
    }

    function getUserActions(serverId) {
        return Promise.resolve([]);
    }

    function deleteUserAction(action) {
        return Promise.resolve();
    }

    function deleteUserActions(actions) {
        //TODO: 
        return Promise.resolve();
    }

    function getServerItemIds(serverId) {
        return Promise.resolve([]);
    }

    function removeLocalItem(localItem) {
        return Promise.resolve();
    }

    function getLocalItem(itemId, serverId) {
        return Promise.resolve();
    }

    function addOrUpdateLocalItem(localItem) {
        return Promise.resolve();
    }

    function createLocalItem(libraryItem, serverInfo, jobItem) {

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

    function getLocalFilePath(path) {
        return null;
    }

    function getLocalItemById(id) {
        return null;
    }

    return {
        getLocalItem: getLocalItem,
        saveOfflineUser: saveOfflineUser,
        deleteOfflineUser: deleteOfflineUser,
        getCameraPhotos: getCameraPhotos,
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
        fileExists: fileExists,
        translateFilePath: translateFilePath,
        getLocalFilePath: getLocalFilePath,
        getLocalItemById: getLocalItemById
    };
});
define(['filerepository'], function (filerepository) {
    'use strict';

    function downloadFile(url, folderName, localPath) {

        return Promise.resolve();
    }

    function downloadSubtitles(url, folderName, localItem) {

        return Promise.resolve('');
    }

    function downloadImage(url, folderName, serverId, itemId, imageTag) {
        return Promise.resolve(false);
    }

    return {
        downloadFile: downloadFile,
        downloadSubtitles: downloadSubtitles,
        downloadImage: downloadImage
    };
});
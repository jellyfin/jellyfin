define(['filerepository'], function (filerepository) {
    'use strict';

    function downloadFile(url, localPath) {

        return Promise.resolve();
    }

    function downloadSubtitles(url, localItem, subtitleStreamh) {

        return Promise.resolve('');
    }

    function downloadImage(url, serverId, itemId, imageTag) {
        return Promise.resolve(false);
    }

    return {
        downloadFile: downloadFile,
        downloadSubtitles: downloadSubtitles,
        downloadImage: downloadImage
    };
});
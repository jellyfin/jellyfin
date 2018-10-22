define([], function() {
    "use strict";

    function getValidFileName(path) {
        return path
    }

    function getFullLocalPath(pathArray) {
        return pathArray.join("/")
    }

    function getPathFromArray(pathArray) {
        return pathArray.join("/")
    }

    function deleteFile(path) {
        return Promise.resolve()
    }

    function deleteDirectory(path) {
        return Promise.resolve()
    }

    function fileExists(path) {
        return Promise.resolve()
    }

    function getItemFileSize(path) {
        return Promise.resolve(0)
    }

    function getImageUrl(pathParts) {
        return pathParts.join("/")
    }
    return {
        getValidFileName: getValidFileName,
        getFullLocalPath: getFullLocalPath,
        getPathFromArray: getPathFromArray,
        deleteFile: deleteFile,
        deleteDirectory: deleteDirectory,
        fileExists: fileExists,
        getItemFileSize: getItemFileSize,
        getImageUrl: getImageUrl
    }
});
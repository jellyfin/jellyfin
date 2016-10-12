define(['cryptojs-md5'], function () {
    'use strict';

    function loadImage(elem, url) {

        if (elem.tagName !== "IMG") {

            elem.style.backgroundImage = "url('" + url + "')";
            return Promise.resolve(elem);

        } else {
            elem.setAttribute("src", url);
            return Promise.resolve(elem);
        }
    }

    // Request Quota (only for File System API)  
    var requestedBytes = 1024 * 1024 * 1500;
    var imageCacheDirectoryEntry;
    var imageCacheFolder = 'images';

    function createDir(rootDirEntry, folders, callback, errorCallback) {
        // Throw out './' or '/' and move on to prevent something like '/foo/.//bar'.
        if (folders[0] === '.' || folders[0] === '') {
            folders = folders.slice(1);
        }
        rootDirEntry.getDirectory(folders[0], { create: true }, function (dirEntry) {
            // Recursively add the new subfolder (if we still have another to create).
            if (folders.length > 1) {
                createDir(dirEntry, folders.slice(1), callback, errorCallback);
            } else {
                callback(dirEntry);
            }
        }, errorCallback);
    }

    navigator.webkitPersistentStorage.requestQuota(
        requestedBytes, function (grantedBytes) {

            var requestMethod = window.webkitRequestFileSystem || window.requestFileSystem;

            requestMethod(PERSISTENT, grantedBytes, function (fs) {

                fileSystem = fs;

                createDir(fileSystem.root, imageCacheFolder.split('/'), function (dirEntry) {

                    imageCacheDirectoryEntry = dirEntry;

                    // TODO: find a better time to schedule this
                    setTimeout(cleanCache, 60000);
                });

            });

        });

    function toArray(list) {
        return Array.prototype.slice.call(list || [], 0);
    }

    function cleanCache() {

        var dirReader = imageCacheDirectoryEntry.createReader();
        var entries = [];

        var onReadFail = function () {
            console.log('dirReader.readEntries failed');
        };

        // Keep calling readEntries() until no more results are returned.
        var readEntries = function () {
            dirReader.readEntries(function (results) {
                if (!results.length) {
                    entries.forEach(cleanFile);
                } else {
                    entries = entries.concat(toArray(results));
                    readEntries();
                }
            }, onReadFail);
        };

        // Start reading the directory.
        readEntries();
    }

    function cleanFile(fileEntry) {
        if (!fileEntry.isFile) {
            return;
        }

        fileEntry.file(function (file) {

            getLastModified(file, fileEntry).then(function (lastModifiedDate) {

                var elapsed = new Date().getTime() - lastModifiedDate;
                // 40 days
                var maxElapsed = 3456000000;
                if (elapsed >= maxElapsed) {

                    var fullPath = fileEntry.fullPath;
                    console.log('deleting file: ' + fullPath);

                    fileEntry.remove(function () {
                        console.log('File deleted: ' + fullPath);
                    }, function () {
                        console.log('Failed to delete file: ' + fullPath);
                    });
                }
            });

        });
    }

    function getLastModified(file, fileEntry) {

        var lastModifiedDate = file.lastModified || file.lastModifiedDate || file.modificationTime;
        if (lastModifiedDate) {
            if (lastModifiedDate.getTime) {
                lastModifiedDate = lastModifiedDate.getTime();
            }
            return Promise.resolve(lastModifiedDate);
        }

        return new Promise(function (resolve, reject) {

            fileEntry.getMetadata(function (metadata) {
                var lastModifiedDate = metadata.lastModified || metadata.lastModifiedDate || metadata.modificationTime;
                if (lastModifiedDate) {
                    if (lastModifiedDate.getTime) {
                        lastModifiedDate = lastModifiedDate.getTime();
                    }
                }
                resolve(lastModifiedDate);
            });
        });
    }

    function getCacheKey(url) {

        // Try to strip off the domain to share the cache between local and remote connections
        var index = url.indexOf('://');

        if (index !== -1) {
            url = url.substring(index + 3);

            index = url.indexOf('/');

            if (index !== -1) {
                url = url.substring(index + 1);
            }

        }

        return CryptoJS.MD5(url).toString();
    }

    function downloadToFile(url, dir, filename, callback, errorCallback) {

        console.log('Downloading ' + url);

        var xhr = new XMLHttpRequest();
        xhr.open('GET', url, true);
        xhr.responseType = "arraybuffer";

        xhr.onload = function (e) {
            if (this.status === 200) {
                writeData(dir, filename, this.getResponseHeader('Content-Type'), this.response, callback, errorCallback);
            } else {
                errorCallback();
            }
        };

        xhr.send();
    }

    function writeData(dir, filename, fileType, data, callback, errorCallback) {

        dir.getFile(filename, { create: true }, function (fileEntry) {

            // Create a FileWriter object for our FileEntry (log.txt).
            fileEntry.createWriter(function (fileWriter) {

                fileWriter.onwriteend = function (e) {
                    callback(fileEntry);
                };

                fileWriter.onerror = errorCallback;

                // Create a new Blob and write it to log.txt.
                var blob = new Blob([data], { type: fileType });

                fileWriter.write(blob);

            }, errorCallback);

        }, errorCallback);
    }

    function getImageUrl(originalUrl) {

        return new Promise(function (resolve, reject) {

            if (originalUrl.indexOf('tag=') !== -1) {
                originalUrl += "&accept=webp";
            }

            var key = getCacheKey(originalUrl);

            var fileEntryCallback = function (fileEntry) {
                resolve(fileEntry.toURL());
            };

            var errorCallback = function (e) {
                console.log('Imagestore error: ' + e.name);
                reject();
            };

            if (!fileSystem || !imageCacheDirectoryEntry) {
                errorCallback('');
                return;
            }

            var path = '/' + imageCacheFolder + "/" + key;

            fileSystem.root.getFile(path, { create: false }, fileEntryCallback, function () {

                downloadToFile(originalUrl, imageCacheDirectoryEntry, key, fileEntryCallback, errorCallback);
            });
        });
    }

    var fileSystem;

    return {
        loadImage: function (elem, url) {

            return getImageUrl(url).then(function (localUrl) {

                return loadImage(elem, localUrl);

            }, function () {
                return loadImage(elem, url);
            });
        }
    };

});
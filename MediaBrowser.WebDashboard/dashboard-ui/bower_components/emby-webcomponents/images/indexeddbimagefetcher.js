define(['cryptojs-md5'], function () {
    'use strict';

    var indexedDB = window.indexedDB || window.webkitIndexedDB || window.mozIndexedDB || window.OIndexedDB || window.msIndexedDB;
    var dbVersion = 1;
    var imagesTableName = "images";
    var db;

    function createObjectStore(dataBase) {

        dataBase.createObjectStore(imagesTableName, { keyPath: "id" });
        db = dataBase;
    }

    // Create/open database
    var request = indexedDB.open("imagesDb2", dbVersion);

    request.onupgradeneeded = function () {
        createObjectStore(request.result);
    };

    request.onsuccess = function (event) {

        console.log("Success creating/accessing IndexedDB database");

        var localDb = request.result;

        localDb.onerror = function (event) {
            console.log("Error creating/accessing IndexedDB database");
        };

        // Interim solution for Google Chrome to create an objectStore. Will be deprecated
        if (localDb.setVersion) {
            if (localDb.version !== dbVersion) {
                var setVersion = localDb.setVersion(dbVersion);
                setVersion.onsuccess = function () {
                    createObjectStore(localDb);
                };
            } else {
                db = localDb;
            }
        } else {
            db = localDb;
        }
    };

    function revoke(url) {

        //URL.revokeObjectURL(url);

    }

    function loadImage(elem, url) {

        if (elem.tagName !== "IMG") {

            elem.style.backgroundImage = "url('" + url + "')";
            revoke(url);
            return Promise.resolve(elem);

        } else {
            elem.setAttribute("src", url);
            revoke(url);
            return Promise.resolve(elem);
        }
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

    function getFromDb(key) {

        return new Promise(function (resolve, reject) {

            var transaction = db.transaction(imagesTableName, "read");

            // Retrieve the file that was just stored
            var request = transaction.objectStore(imagesTableName).get(key);

            request.onsuccess = function (event) {
                var imgFile = event.target.result;

                // Get window.URL object
                var URL = window.URL || window.webkitURL;

                // Create and revoke ObjectURL
                var imgURL = URL.createObjectURL(imgFile);

                resolve(imgURL);
            };

            request.onerror = reject;
        });
    }

    function saveImageToDb(blob, key, resolve) {

        // Open a transaction to the database
        var transaction = db.transaction(imagesTableName, "readwrite");

        // Put the blob into the dabase
        var put = transaction.objectStore(imagesTableName).put({ id: key, data: blob });

        // Get window.URL object
        var URL = window.URL || window.webkitURL;

        var imgURL = URL.createObjectURL(blob);

        resolve(imgURL);
    }

    function getImageUrl(originalUrl) {

        var key = getCacheKey(originalUrl);

        return getFromDb(key).catch(function () {

            return new Promise(function (resolve, reject) {

                var xhr = new XMLHttpRequest();

                xhr.open("GET", originalUrl, true);
                // Set the responseType to blob
                xhr.responseType = "blob";

                xhr.addEventListener("load", function () {
                    if (xhr.status === 200) {

                        // Put the received blob into IndexedDB
                        saveImageToDb(xhr.response, key, resolve);
                    } else {
                        reject();
                    }
                }, false);

                xhr.onerror = reject;

                // Send XHR
                xhr.send();
            });
        });
    }

    return {
        loadImage: function (elem, url) {

            if (!db) {
                return loadImage(elem, url);
            }

            return getImageUrl(url).then(function (localUrl) {

                return loadImage(elem, localUrl);

            }, function () {
                return loadImage(elem, url);
            });
        }
    };

});
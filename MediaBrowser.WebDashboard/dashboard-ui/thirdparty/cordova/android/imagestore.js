
(function () {

    function setImageIntoElement(elem, url) {

        if (elem.tagName === "DIV") {

            elem.style.backgroundImage = "url('" + url + "')";

        } else {
            elem.setAttribute("src", url);
        }
    }

    // IndexedDB
    var indexedDb = window.indexedDB || window.webkitIndexedDB || window.mozIndexedDB || window.OIndexedDB || window.msIndexedDB,
        dbVersion = 1.0;

    var dbName = "emby7";
    var imagesStoreName = "images";

    function createObjectStore(dataBase) {
        // Create an objectStore
        console.log("Creating objectStore");
        dataBase.createObjectStore(imagesStoreName);
    }

    function openDb() {

        var deferred = $.Deferred();

        // Create/open database
        var request = indexedDb.open(dbName, dbVersion);

        request.onerror = function (event) {

            console.log("Error creating/accessing IndexedDB database");
            deferred.reject();
        };

        request.onsuccess = function (event) {
            console.log("Success creating/accessing IndexedDB database");

            var db = request.result || event.target.result;

            db.onerror = function (event) {
                console.log("Error creating/accessing IndexedDB database");
            };

            // Interim solution for Google Chrome to create an objectStore. Will be deprecated
            if (db.setVersion) {
                if (db.version != dbVersion) {
                    var setVersion = db.setVersion(dbVersion);
                    setVersion.onsuccess = function () {
                        createObjectStore(db);
                        deferred.resolveWith(null, [db]);
                    };
                } else {
                    deferred.resolveWith(null, [db]);
                }
            } else {
                deferred.resolveWith(null, [db]);
            }
        }

        // For future use. Currently only in latest Firefox versions
        request.onupgradeneeded = function (event) {
            createObjectStore(event.target.result);
        };

        return deferred.promise();
    }

    function indexedDbBlobImageStore() {

        var self = this;

        openDb().done(function (db) {

            self._db = db;
            window.ImageStore = self;
        });

        self.addImageToDatabase = function (blob, key, deferred) {

            console.log("addImageToDatabase");

            // Open a transaction to the database
            var transaction = self.db().transaction([imagesStoreName], "readwrite");

            // Put the blob into the dabase
            var putRequest = transaction.objectStore(imagesStoreName).put(blob, key);

            putRequest.onsuccess = function (event) {
                deferred.resolve();
            };

            putRequest.onerror = function () {
                deferred.reject();
            };
        };

        self.db = function () {

            return self._db;
        };

        self.get = function (key) {

            var deferred = DeferredBuilder.Deferred();

            var transaction = self.db().transaction([imagesStoreName], "readonly");

            // Open a transaction to the database
            var getRequest = transaction.objectStore(imagesStoreName).get(key);

            getRequest.onsuccess = function (event) {

                var imgFile = event.target.result;

                if (imgFile) {

                    // Get window.URL object
                    var URL = window.URL || window.webkitURL;

                    // Create and revoke ObjectURL
                    var imgUrl = URL.createObjectURL(imgFile);

                    deferred.resolveWith(null, [imgUrl]);
                } else {
                    deferred.reject();
                }
            };

            getRequest.onerror = function () {
                deferred.reject();
            };

            return deferred.promise();
        };

        function getCacheKey(url) {

            // Try to strip off the domain to share the cache between local and remote connections
            var index = url.indexOf('://');

            if (index != -1) {
                url = url.substring(index + 3);

                index = url.indexOf('/');

                if (index != -1) {
                    url = url.substring(index + 1);
                }

            }

            return CryptoJS.MD5(url).toString();
        }

        self.getImageUrl = function (originalUrl) {

            console.log('getImageUrl:' + originalUrl);

            var key = getCacheKey(originalUrl);

            var deferred = DeferredBuilder.Deferred();

            self.get(key).done(function (url) {

                deferred.resolveWith(null, [url]);

            }).fail(function () {

                self.downloadImage(originalUrl, key).done(function () {
                    self.get(key).done(function (url) {

                        deferred.resolveWith(null, [url]);

                    }).fail(function () {

                        deferred.reject();
                    });
                }).fail(function () {

                    deferred.reject();
                });
            });

            return deferred.promise();
        };

        self.downloadImage = function (url, key) {

            var deferred = DeferredBuilder.Deferred();

            console.log('downloadImage:' + url);

            // Create XHR
            var xhr = new XMLHttpRequest();

            xhr.open("GET", url, true);
            // Set the responseType to blob
            xhr.responseType = "blob";

            xhr.addEventListener("load", function () {

                if (xhr.status === 200) {
                    console.log("Image retrieved");

                    // Put the received blob into IndexedDB
                    self.addImageToDatabase(this.response, key, deferred);
                } else {
                    deferred.reject();
                }
            }, false);

            // Send XHR
            xhr.send();
            return deferred.promise();
        };

        self.setImageInto = function (elem, url) {

            function onFail() {
                setImageIntoElement(elem, url);
            }

            self.getImageUrl(url).done(function (localUrl) {

                setImageIntoElement(elem, localUrl);

            }).fail(onFail);
        };
    }

    new indexedDbBlobImageStore();

})();
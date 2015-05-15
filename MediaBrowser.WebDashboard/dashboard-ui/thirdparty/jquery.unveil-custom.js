/**
 * jQuery Unveil
 * A very lightweight jQuery plugin to lazy load images
 * http://luis-almeida.github.com/unveil
 *
 * Licensed under the MIT license.
 * Copyright 2013 Luís Almeida
 * https://github.com/luis-almeida
 */

(function ($) {

    var unveilId = 0;


    function getThreshold() {

        if (window.AppInfo && AppInfo.hasLowImageBandwidth) {
            return 0;
        }

        // Test search before setting to 0
        return 100;
    }

    $.fn.unveil = function () {

        var $w = $(window),
            th = getThreshold(),
            attrib = "data-src",
            images = this,
            loaded;

        unveilId++;
        var eventNamespace = 'unveil' + unveilId;

        this.one("unveil", function () {
            var elem = this;
            var source = elem.getAttribute(attrib);
            if (source) {
                ImageStore.setImageInto(elem, source);
                elem.setAttribute("data-src", '');
            }
        });

        function unveil() {
            var inview = images.filter(function () {
                var $e = $(this);

                if ($e.is(":hidden")) return;
                var wt = $w.scrollTop(),
                    wb = wt + $w.height(),
                    et = $e.offset().top,
                    eb = et + $e.height();

                return eb >= wt - th && et <= wb + th;
            });

            loaded = inview.trigger("unveil");
            images = images.not(loaded);

            if (!images.length) {
                $w.off('scroll.' + eventNamespace);
                $w.off('resize.' + eventNamespace);
            }
        }

        $w.on('scroll.' + eventNamespace, unveil);
        $w.on('resize.' + eventNamespace, unveil);

        unveil();

        return this;

    };

    $.fn.lazyChildren = function () {

        var lazyItems = $(".lazy", this);

        if (lazyItems.length) {
            lazyItems.unveil();
        }

        return this;
    };

    $.fn.lazyImage = function (url) {

        return this.attr('data-src', url).unveil();
    };

})(window.jQuery || window.Zepto);

(function () {

    // IndexedDB
    var indexedDB = window.indexedDB || window.webkitIndexedDB || window.mozIndexedDB || window.OIndexedDB || window.msIndexedDB,
        dbVersion = 1.0;

    var dbName = "emby4";
    var imagesStoreName = "images";

    function createObjectStore(dataBase) {
        // Create an objectStore
        console.log("Creating objectStore");
        dataBase.createObjectStore(imagesStoreName);
    }

    function setImageIntoElement(elem, url) {

        if (elem.tagName === "DIV") {

            elem.style.backgroundImage = "url('" + url + "')";

        } else {
            elem.setAttribute("src", url);
        }
    }

    function openDb() {

        var deferred = $.Deferred();

        // Create/open database
        var request = indexedDB.open(dbName, dbVersion);

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

    function indexedDbImageStore() {

        var self = this;

        openDb().done(function (db) {

            self._db = db;
            window.ImageStore = self;
        });

        self.addImageToDatabase = function (blob, key) {

            console.log("addImageToDatabase");

            // Open a transaction to the database
            var transaction = self.db().transaction([imagesStoreName], "readwrite");

            // Put the blob into the dabase
            var put = transaction.objectStore(imagesStoreName).put(blob, key);
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
                    deferred.resolveWith(null, [imgFile]);
                } else {
                    deferred.reject();
                }
            };

            getRequest.onerror = function () {
                deferred.reject();
            };

            return deferred.promise();
        };

        self.getImageUrl = function (originalUrl) {

            console.log('getImageUrl:' + originalUrl);

            var key = CryptoJS.SHA1(originalUrl).toString();

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
            xhr.responseType = "arraybuffer";

            xhr.addEventListener("load", function () {

                if (xhr.status === 200) {
                    console.log("Image retrieved");

                    try {

                        var arr = new Uint8Array(this.response);

                        // Convert the int array to a binary string
                        // We have to use apply() as we are converting an *array*
                        // and String.fromCharCode() takes one or more single values, not
                        // an array.
                        var raw = String.fromCharCode.apply(null, arr);

                        // This works!!!
                        var b64 = btoa(raw);
                        var dataURL = "data:image/jpeg;base64," + b64;

                        // Put the received blob into IndexedDB
                        self.addImageToDatabase(dataURL, key);
                        deferred.resolve();
                    } catch (err) {
                        console.log("Error adding image to database");
                        deferred.reject();
                    }
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

    function indexedDbBlobImageStore() {

        var self = this;

        openDb().done(function (db) {

            self._db = db;
            window.ImageStore = self;
        });

        self.addImageToDatabase = function (blob, key) {

            console.log("addImageToDatabase");

            // Open a transaction to the database
            var transaction = self.db().transaction([imagesStoreName], "readwrite");

            // Put the blob into the dabase
            var put = transaction.objectStore(imagesStoreName).put(blob, key);
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

        self.getImageUrl = function (originalUrl) {

            console.log('getImageUrl:' + originalUrl);

            var key = CryptoJS.SHA1(originalUrl + "1").toString();

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
            xhr.responseType = "arraybuffer";

            xhr.addEventListener("load", function () {

                if (xhr.status === 200) {
                    console.log("Image retrieved");

                    try {
                        var blob = new Blob([this.response], { type: this.getResponseHeader('content-type') });

                        // Put the received blob into IndexedDB
                        self.addImageToDatabase(blob, key);
                        deferred.resolve();
                    } catch (err) {
                        console.log("Error adding blob to database");
                        alert("Error adding blob to database");
                        deferred.reject();
                    }
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

    function simpleImageStore() {

        var self = this;

        self.setImageInto = setImageIntoElement;
    }

    console.log('creating simpleImageStore');
    window.ImageStore = new simpleImageStore();

    //if ($.browser.safari && indexedDB && window.Blob) {
    //    console.log('creating indexedDbBlobImageStore');
    //    new indexedDbBlobImageStore();
    //}
    //else if ($.browser.safari && indexedDB) {
    //    console.log('creating indexedDbImageStore');
    //    new indexedDbImageStore();
    //}

})();
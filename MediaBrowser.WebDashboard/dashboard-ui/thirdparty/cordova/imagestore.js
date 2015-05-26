(function () {

    function setImageIntoElement(elem, url) {

        if (elem.tagName === "DIV") {

            elem.style.backgroundImage = "url('" + url + "')";

        } else {
            elem.setAttribute("src", url);
        }
    }

    function onDbOpened(imageStore, db) {

        imageStore._db = db;
        window.ImageStore = imageStore;
    }

    function openDb(imageStore) {

        // Create/open database
        var db = window.sqlitePlugin.openDatabase({ name: "my.db" });

        db.transaction(function (tx) {

            tx.executeSql('CREATE TABLE IF NOT EXISTS images (id text primary key, data text)');
            tx.executeSql('create index if not exists idx_images on images(id)');

            onDbOpened(imageStore, db);
        });
    }

    function sqliteImageStore() {

        var self = this;

        self.addImageToDatabase = function (blob, key) {

            var deferred = DeferredBuilder.Deferred();

            console.log("addImageToDatabase");

            self.db().transaction(function (tx) {

                tx.executeSql("INSERT INTO images (id, data) VALUES (?,?)", [key, blob], function (tx, res) {

                    deferred.resolve();
                }, function (e) {
                    deferred.reject();
                });
            });

            return deferred.promise();
        };

        self.db = function () {

            return self._db;
        };

        self.get = function (key) {

            var deferred = DeferredBuilder.Deferred();

            self.db().transaction(function (tx) {

                tx.executeSql("SELECT data from images where id=?", [key], function (tx, res) {

                    if (res.rows.length) {

                        deferred.resolveWith(null, [res.rows.item(0).data]);
                    } else {
                        deferred.reject();
                    }
                }, function (e) {
                    deferred.reject();
                });
            });

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

                        // Put the received blob into the database
                        self.addImageToDatabase(dataURL, key).done(function () {
                            deferred.resolve();
                        }).fail(function () {
                            deferred.reject();
                        });
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

        openDb(self);
    }

    new sqliteImageStore();

})();

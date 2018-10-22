define([], function() {
    "use strict";

    function getDb(callback) {
        var db = databaseInstance;
        if (db) return void callback(db);
        var request = indexedDB.open(dbName, dbVersion);
        request.onerror = function(event) {}, request.onupgradeneeded = function(event) {
            var db = event.target.result;
            db.createObjectStore(dbName).transaction.oncomplete = function(event) {
                callback(db)
            }
        }, request.onsuccess = function(event) {
            var db = event.target.result;
            callback(db)
        }
    }

    function getByServerId(serverId) {
        return getAll().then(function(items) {
            return items.filter(function(item) {
                return item.ServerId === serverId
            })
        })
    }

    function getAll() {
        return new Promise(function(resolve, reject) {
            getDb(function(db) {
                var request, storeName = dbName,
                    transaction = db.transaction([storeName], "readonly"),
                    objectStore = transaction.objectStore(storeName);
                if ("getAll" in objectStore) request = objectStore.getAll(null, 1e4), request.onsuccess = function(event) {
                    resolve(event.target.result)
                };
                else {
                    var results = [];
                    request = objectStore.openCursor(), request.onsuccess = function(event) {
                        var cursor = event.target.result;
                        cursor ? (results.push(cursor.value), cursor.continue()) : resolve(results)
                    }
                }
                request.onerror = reject
            })
        })
    }

    function get(key) {
        return new Promise(function(resolve, reject) {
            getDb(function(db) {
                var storeName = dbName,
                    transaction = db.transaction([storeName], "readonly"),
                    objectStore = transaction.objectStore(storeName),
                    request = objectStore.get(key);
                request.onerror = reject, request.onsuccess = function(event) {
                    resolve(request.result)
                }
            })
        })
    }

    function set(key, val) {
        return new Promise(function(resolve, reject) {
            getDb(function(db) {
                var storeName = dbName,
                    transaction = db.transaction([storeName], "readwrite"),
                    objectStore = transaction.objectStore(storeName),
                    request = objectStore.put(val, key);
                request.onerror = reject, request.onsuccess = resolve
            })
        })
    }

    function remove(key) {
        return new Promise(function(resolve, reject) {
            getDb(function(db) {
                var storeName = dbName,
                    transaction = db.transaction([storeName], "readwrite"),
                    objectStore = transaction.objectStore(storeName),
                    request = objectStore.delete(key);
                request.onerror = reject, request.onsuccess = resolve
            })
        })
    }

    function clear() {
        return new Promise(function(resolve, reject) {
            getDb(function(db) {
                var storeName = dbName,
                    transaction = db.transaction([storeName], "readwrite"),
                    objectStore = transaction.objectStore(storeName),
                    request = objectStore.clear();
                request.onerror = reject, request.onsuccess = resolve
            })
        })
    }
    var databaseInstance, indexedDB = self.indexedDB || self.mozIndexedDB || self.webkitIndexedDB || self.msIndexedDB,
        dbName = (self.IDBTransaction || self.webkitIDBTransaction || self.msIDBTransaction, self.IDBKeyRange || self.webkitIDBKeyRange || self.msIDBKeyRange, "useractions"),
        dbVersion = 1;
    return {
        get: get,
        set: set,
        remove: remove,
        clear: clear,
        getAll: getAll,
        getByServerId: getByServerId
    }
});
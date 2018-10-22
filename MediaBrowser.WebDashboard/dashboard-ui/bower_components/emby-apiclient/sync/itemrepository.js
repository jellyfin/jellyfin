define([], function() {
    "use strict";

    function ServerDatabase(dbName, readyCallback) {
        var request = indexedDB.open(dbName, dbVersion);
        request.onerror = function(event) {}, request.onupgradeneeded = function(event) {
            var db = event.target.result;
            db.createObjectStore(dbName).transaction.oncomplete = function(event) {
                readyCallback(db)
            }
        }, request.onsuccess = function(event) {
            var db = event.target.result;
            readyCallback(db)
        }
    }

    function getDbName(serverId) {
        return "items_" + serverId
    }

    function getDb(serverId, callback) {
        var dbName = getDbName(serverId),
            db = databases[dbName];
        if (db) return void callback(db);
        new ServerDatabase(dbName, function(db) {
            databases[dbName] = db, callback(db)
        })
    }

    function getServerItemTypes(serverId, userId) {
        return getAll(serverId, userId).then(function(all) {
            return all.map(function(item2) {
                return item2.Item.Type || ""
            }).filter(filterDistinct)
        })
    }

    function getAll(serverId, userId) {
        return new Promise(function(resolve, reject) {
            getDb(serverId, function(db) {
                var request, storeName = getDbName(serverId),
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

    function get(serverId, key) {
        return new Promise(function(resolve, reject) {
            getDb(serverId, function(db) {
                var storeName = getDbName(serverId),
                    transaction = db.transaction([storeName], "readonly"),
                    objectStore = transaction.objectStore(storeName),
                    request = objectStore.get(key);
                request.onerror = reject, request.onsuccess = function(event) {
                    resolve(request.result)
                }
            })
        })
    }

    function set(serverId, key, val) {
        return new Promise(function(resolve, reject) {
            getDb(serverId, function(db) {
                var storeName = getDbName(serverId),
                    transaction = db.transaction([storeName], "readwrite"),
                    objectStore = transaction.objectStore(storeName),
                    request = objectStore.put(val, key);
                request.onerror = reject, request.onsuccess = resolve
            })
        })
    }

    function remove(serverId, key) {
        return new Promise(function(resolve, reject) {
            getDb(serverId, function(db) {
                var storeName = getDbName(serverId),
                    transaction = db.transaction([storeName], "readwrite"),
                    objectStore = transaction.objectStore(storeName),
                    request = objectStore.delete(key);
                request.onerror = reject, request.onsuccess = resolve
            })
        })
    }

    function clear(serverId) {
        return new Promise(function(resolve, reject) {
            getDb(serverId, function(db) {
                var storeName = getDbName(serverId),
                    transaction = db.transaction([storeName], "readwrite"),
                    objectStore = transaction.objectStore(storeName),
                    request = objectStore.clear();
                request.onerror = reject, request.onsuccess = resolve
            })
        })
    }

    function filterDistinct(value, index, self) {
        return self.indexOf(value) === index
    }
    var indexedDB = self.indexedDB || self.mozIndexedDB || self.webkitIndexedDB || self.msIndexedDB,
        dbVersion = (self.IDBTransaction || self.webkitIDBTransaction || self.msIDBTransaction, self.IDBKeyRange || self.webkitIDBKeyRange || self.msIDBKeyRange, 1),
        databases = {};
    return {
        get: get,
        set: set,
        remove: remove,
        clear: clear,
        getAll: getAll,
        getServerItemTypes: getServerItemTypes
    }
});
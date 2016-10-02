define([], function () {
    'use strict';

    var myStore = {};
    var cache;
    var localData;

    function updateCache() {
        cache.put('data', new Response(JSON.stringify(localData)));
    }

    myStore.setItem = function (name, value) {

        if (localData) {
            var changed = localData[name] !== value;

            if (changed) {
                localData[name] = value;
                updateCache();
            }
        }
    };

    myStore.getItem = function (name) {

        if (localData) {
            return localData[name];
        }
    };

    myStore.removeItem = function (name) {

        if (localData) {
            localData[name] = null;
            delete localData[name];
            updateCache();
        }
    };

    myStore.init = function () {
        return caches.open('embydata').then(function (result) {
            cache = result;
            localData = {};
        });
    };

    return myStore;
});
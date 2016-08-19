define([], function () {

    var myStore = {};
    var cache;
    var localData;

    function updateCache() {
        cache.put('data', new Response(JSON.stringify(localData)));
    }

    myStore.setItem = function (name, value) {
        localStorage.setItem(name, value);

        if (localData) {
            var changed = localData[name] != value;

            if (changed) {
                localData[name] = value;
                updateCache();
            }
        }
    };

    myStore.getItem = function (name) {
        return localStorage.getItem(name);
    };

    myStore.removeItem = function (name) {
        localStorage.removeItem(name);

        if (localData) {
            localData[name] = null;
            delete localData[name];
            updateCache();
        }
    };

    try {

        caches.open('embydata').then(function (result) {
            cache = result;
            localData = {};
        });

    } catch (err) {
        console.log('Error opening cache: ' + err);
    }

    return myStore;
});
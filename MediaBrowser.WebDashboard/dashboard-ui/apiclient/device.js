(function (globalScope) {

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.generateDeviceId = function (keyName, seed) {

        keyName = keyName || 'randomId';

        var keys = [];

        keys.push(navigator.userAgent);
        keys.push((navigator.cpuClass || ""));

        if (seed) {
            keys.push(seed);
        }
        var randomId = '';

        //  Since the above is not guaranteed to be unique per device, add a little more
        randomId = appStorage.getItem(keyName);

        if (!randomId) {

            randomId = new Date().getTime();

            appStorage.setItem(keyName, randomId.toString());
        }

        keys.push(randomId);
        return CryptoJS.SHA1(keys.join('|')).toString();
    };

})(window);
(function (globalScope) {

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.generateDeviceId = function () {

        var keys = [];

        keys.push(navigator.userAgent);
        keys.push((navigator.cpuClass || ""));

        var randomId = '';

        //  Since the above is not guaranteed to be unique per device, add a little more
        randomId = store.getItem('randomId');

        if (!randomId) {

            randomId = new Date().getTime();

            store.setItem('randomId', randomId.toString());
        }

        keys.push(randomId);
        return CryptoJS.SHA1(keys.join('|')).toString();
    };

})(window);
(function () {

    window.LocalSync = {

        isSupported: function () {
            return true;
        },

        startSync: function () {
            AndroidSync.startSync();
        },

        getSyncStatus: function () {
            return AndroidSync.getSyncStatus();
        }
    };

})();
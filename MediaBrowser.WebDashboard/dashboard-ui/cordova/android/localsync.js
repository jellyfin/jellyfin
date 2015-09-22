(function () {

    window.LocalSync = {

        isSupported: function () {
            return true;
        },

        sync: function () {
            AndroidSync.startSync();
        },

        getSyncStatus: function () {
            return AndroidSync.getSyncStatus();
        }
    };

})();
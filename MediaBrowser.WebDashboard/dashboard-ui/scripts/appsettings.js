(function (window) {

    function update(key, val) {
        appStorage.setItem(key, val);

        Events.trigger(AppSettings, 'settingupdated', [key]);
    }

    window.AppSettings = {

        maxStreamingBitrate: function (val) {

            if (val != null) {
                update('preferredVideoBitrate', val);
            }

            return parseInt(appStorage.getItem('preferredVideoBitrate') || '') || 1500000;
        },
        maxChromecastBitrate: function (val) {

            if (val != null) {
                update('chromecastBitrate', val);
            }

            return parseInt(appStorage.getItem('chromecastBitrate') || '') || 3000000;
        },
        enableChromecastAc3: function (val) {

            if (val != null) {
                update('enablechromecastac3', val.toString());
            }

            return appStorage.getItem('enablechromecastac3') == 'true';
        },
        enableExternalPlayers: function (val) {

            if (val != null) {
                update('externalplayers', val.toString());
            }

            return appStorage.getItem('externalplayers') == 'true';
        },
        enableItemPreviews: function (val) {

            if (val != null) {
                update('enableItemPreviews', val.toString());
            }

            return appStorage.getItem('enableItemPreviews') == 'true';
        },
        enableFullScreen: function (val) {

            if (val != null) {
                update('enableFullScreen', val.toString());
            }

            return appStorage.getItem('enableFullScreen') == 'true';
        },
        enableSyncToExternalStorage: function (val) {

            if (val != null) {
                update('enableSyncToExternalStorage', val.toString());
            }

            return appStorage.getItem('enableSyncToExternalStorage') == 'true';
        },

        displayPreferencesKey: function() {
            if (AppInfo.isNativeApp) {
                return 'Emby Mobile';
            }

            return 'webclient';
        }
    };


})(window);
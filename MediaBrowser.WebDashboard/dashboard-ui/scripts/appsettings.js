(function (window) {

    function update(key, val) {
        appStorage.setItem(key, val);

        Events.trigger(AppSettings, 'settingupdated', [key]);
    }

    window.AppSettings = {

        enableAutomaticBitrateDetection: function (val) {

            if (val != null) {
                update('enableAutomaticBitrateDetection', val.toString());
            }

            var savedVal = appStorage.getItem('enableAutomaticBitrateDetection');

            if (!savedVal) {
                if (AppInfo.isNativeApp) {
                    //return false;
                }
            }

            return appStorage.getItem('enableAutomaticBitrateDetection') != 'false';
        },
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
        enableCinemaMode: function (val) {

            if (val != null) {
                update('enableCinemaMode', val.toString());
            }

            val = appStorage.getItem('enableCinemaMode');

            if (val) {
                return val != 'false';
            }

            if (browserInfo.mobile) {
                return false;
            }

            return true;
        },
        enableFullScreen: function (val) {

            if (val != null) {
                update('enableFullScreen', val.toString());
            }

            return appStorage.getItem('enableFullScreen') == 'true';
        },
        syncOnlyOnWifi: function (val) {

            if (val != null) {
                update('syncOnlyOnWifi', val.toString());
            }

            return appStorage.getItem('syncOnlyOnWifi') != 'false';
        },
        syncLosslessAudio: function (val) {

            if (val != null) {
                update('syncLosslessAudio', val.toString());
            }

            return appStorage.getItem('syncLosslessAudio') != 'false';
        },
        syncPath: function (val) {

            if (val != null) {
                update('syncPath', val);
            }

            return appStorage.getItem('syncPath');
        },

        displayLanguage: function (val) {

            if (val != null) {
                update('displayLanguage', val);
            }

            return appStorage.getItem('displayLanguage') || navigator.language || navigator.userLanguage || 'en-US';
        },

        cameraUploadServers: function (val) {

            if (val != null) {
                update('cameraUploadServers', val.join(','));
            }

            val = appStorage.getItem('cameraUploadServers');

            if (val) {
                return val.split(',');
            }

            return [];
        },

        displayPreferencesKey: function () {
            if (AppInfo.isNativeApp) {
                return 'Emby Mobile';
            }

            return 'webclient';
        }
    };


})(window);
define(['appStorage'], function (appStorage) {

    function getKey(name, userId) {

        if (userId) {
            name = userId + '-' + name;
        }

        return name;
    }

    function get(name, userId) {

        return appStorage.getItem(getKey(name, userId));
    }

    function set(name, value, userId) {
        appStorage.setItem(getKey(name, userId), value);
    }

    return {
        enableAutomaticBitrateDetection: function (val) {

            if (val != null) {
                set('enableAutomaticBitrateDetection', val.toString());
            }

            return get('enableAutomaticBitrateDetection') != 'false';
        },
        maxStreamingBitrate: function (val) {

            if (val != null) {
                set('preferredVideoBitrate', val);
            }

            return parseInt(get('preferredVideoBitrate') || '') || 1500000;
        },
        maxChromecastBitrate: function (val) {

            if (val != null) {
                set('chromecastBitrate1', val);
            }

            val = get('chromecastBitrate1');

            return val ? parseInt(val) : null;
        },
        syncOnlyOnWifi: function (val) {

            if (val != null) {
                set('syncOnlyOnWifi', val.toString());
            }

            return get('syncOnlyOnWifi') != 'false';
        },
        syncPath: function (val) {

            if (val != null) {
                set('syncPath', val);
            }

            return get('syncPath');
        },

        cameraUploadServers: function (val) {

            if (val != null) {
                set('cameraUploadServers', val.join(','));
            }

            val = get('cameraUploadServers');

            if (val) {
                return val.split(',');
            }

            return [];
        },
        set: set,
        get: get
    };
});
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
        set: set,
        get: get
    };
});
(function (window, store) {

    function update(key, val) {
        store.setItem(key, val);

        Events.trigger(AppSettings, 'settingupdated', [key]);
    }

    window.AppSettings = {

        maxStreamingBitrate: function (val) {

            if (val != null) {
                update('preferredVideoBitrate', val);
            }

            return parseInt(store.getItem('preferredVideoBitrate') || '') || 1500000;
        },
        maxChromecastBitrate: function (val) {

            if (val != null) {
                update('chromecastBitrate', val);
            }

            return parseInt(store.getItem('chromecastBitrate') || '') || 3000000;
        },
        enableExternalPlayers: function (val) {

            if (val != null) {
                update('externalplayers', val.toString());
            }

            return store.getItem('externalplayers') == 'true';
        },
        enableItemPreviews: function (val) {

            if (val != null) {
                update('enableItemPreviews', val.toString());
            }

            return store.getItem('enableItemPreviews') == 'true';
        },
        enableFullScreen: function (val) {

            if (val != null) {
                update('enableFullScreen', val.toString());
            }

            return store.getItem('enableFullScreen') == 'true';
        }

    };


})(window, window.appStorage);
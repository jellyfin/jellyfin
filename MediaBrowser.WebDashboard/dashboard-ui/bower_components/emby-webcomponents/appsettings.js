define(['appStorage', 'events'], function (appStorage, events) {

    function getKey(name, userId) {

        if (userId) {
            name = userId + '-' + name;
        }

        return name;
    }

    return new function () {

        var self = this;

        self.enableAutoLogin = function (val) {

            if (val != null) {
                self.set('enableAutoLogin', val.toString());
            }

            return self.get('enableAutoLogin') != 'false';
        };

        self.enableAutomaticBitrateDetection = function (val) {

            if (val != null) {
                self.set('enableAutomaticBitrateDetection', val.toString());
            }

            return self.get('enableAutomaticBitrateDetection') != 'false';
        };

        self.maxStreamingBitrate = function (val) {

            if (val != null) {
                self.set('preferredVideoBitrate', val);
            }

            return parseInt(self.get('preferredVideoBitrate') || '') || 1500000;
        };

        self.maxChromecastBitrate = function (val) {

            if (val != null) {
                self.set('chromecastBitrate1', val);
            }

            val = self.get('chromecastBitrate1');

            return val ? parseInt(val) : null;
        };

        self.syncOnlyOnWifi = function (val) {

            if (val != null) {
                self.set('syncOnlyOnWifi', val.toString());
            }

            return self.get('syncOnlyOnWifi') != 'false';
        };

        self.syncPath = function (val) {

            if (val != null) {
                self.set('syncPath', val);
            }

            return self.get('syncPath');
        };

        self.cameraUploadServers = function (val) {

            if (val != null) {
                self.set('cameraUploadServers', val.join(','));
            }

            val = self.get('cameraUploadServers');

            if (val) {
                return val.split(',');
            }

            return [];
        };

        self.set = function (name, value, userId) {

            var currentValue = self.get(name, userId);

            appStorage.setItem(getKey(name, userId), value);

            if (currentValue != value) {
                events.trigger(self, 'change', [name]);
            }
        };

        self.get = function (name, userId) {

            return appStorage.getItem(getKey(name, userId));
        };
    }();
});
define(['appStorage', 'events'], function (appStorage, events) {
    'use strict';

    function getKey(name, userId) {

        if (userId) {
            name = userId + '-' + name;
        }

        return name;
    }

    function AppSettings() {

    }

    AppSettings.prototype.enableAutoLogin = function (val) {

        if (val != null) {
            this.set('enableAutoLogin', val.toString());
        }

        return this.get('enableAutoLogin') !== 'false';
    };

    AppSettings.prototype.enableAutomaticBitrateDetection = function (isInNetwork, mediaType, val) {

        var key = 'enableautobitratebitrate-' + mediaType + '-' + isInNetwork;

        if (val != null) {

            if (isInNetwork && mediaType === 'Audio') {
                val = true;
            }

            this.set(key, val.toString());
        }

        if (isInNetwork && mediaType === 'Audio') {
            return true;
        } else {
            return this.get(key) !== 'false';
        }
    };

    AppSettings.prototype.maxStreamingBitrate = function (isInNetwork, mediaType, val) {

        var key = 'maxbitrate-' + mediaType + '-' + isInNetwork;

        if (val != null) {

            if (isInNetwork && mediaType === 'Audio') {
                //  nothing to do, this is always a max value
            } else {
                this.set(key, val);
            }
        }

        if (isInNetwork && mediaType === 'Audio') {
            // return a huge number so that it always direct plays
            return 150000000;
        } else {
            return parseInt(this.get(key) || '0') || 1500000;
        }
    };

    AppSettings.prototype.maxStaticMusicBitrate = function (val) {

        if (val !== undefined) {
            this.set('maxStaticMusicBitrate', val);
        }

        var defaultValue = 320000;
        return parseInt(this.get('maxStaticMusicBitrate') || defaultValue.toString()) || defaultValue;
    };

    AppSettings.prototype.maxChromecastBitrate = function (val) {

        if (val != null) {
            this.set('chromecastBitrate1', val);
        }

        val = this.get('chromecastBitrate1');

        return val ? parseInt(val) : null;
    };

    AppSettings.prototype.syncOnlyOnWifi = function (val) {

        if (val != null) {
            this.set('syncOnlyOnWifi', val.toString());
        }

        return this.get('syncOnlyOnWifi') !== 'false';
    };

    AppSettings.prototype.syncPath = function (val) {

        if (val != null) {
            this.set('syncPath', val);
        }

        return this.get('syncPath');
    };

    AppSettings.prototype.cameraUploadServers = function (val) {

        if (val != null) {
            this.set('cameraUploadServers', val.join(','));
        }

        val = this.get('cameraUploadServers');

        if (val) {
            return val.split(',');
        }

        return [];
    };

    AppSettings.prototype.runAtStartup = function (val) {

        if (val != null) {
            this.set('runatstartup', val.toString());
        }

        return this.get('runatstartup') === 'true';
    };

    AppSettings.prototype.set = function (name, value, userId) {

        var currentValue = this.get(name, userId);

        appStorage.setItem(getKey(name, userId), value);

        if (currentValue !== value) {
            events.trigger(this, 'change', [name]);
        }
    };

    AppSettings.prototype.get = function (name, userId) {

        return appStorage.getItem(getKey(name, userId));
    };

    AppSettings.prototype.enableSystemExternalPlayers = function (val) {

        if (val != null) {
            this.set('enableSystemExternalPlayers', val.toString());
        }

        return this.get('enableSystemExternalPlayers') === 'true';
    };

    return new AppSettings();
});
define(['appStorage', 'browser'], function (appStorage, browser) {

    function getDeviceProfile() {

        // TODO
        return null;
    }

    function getCapabilities() {

        var caps = {
            PlayableMediaTypes: ['Audio', 'Video'],

            SupportsPersistentIdentifier: false,
            DeviceProfile: getDeviceProfile()
        };

        return caps;
    }

    function generateDeviceId() {
        return new Promise(function (resolve, reject) {

            require(['fingerprintjs2'], function (Fingerprint2) {

                new Fingerprint2().get(function (result, components) {
                    console.log('Generated device id: ' + result); //a hash, representing your device fingerprint
                    resolve(result);
                });
            });
        });
    }

    function getDeviceId() {
        var key = '_deviceId2';
        var deviceId = appStorage.getItem(key);

        if (deviceId) {
            return Promise.resolve(deviceId);
        } else {
            return generateDeviceId().then(function (deviceId) {
                appStorage.setItem(key, deviceId);
                return deviceId;
            });
        }
    }

    function getDeviceName() {
        var deviceName;

        if (browser.chrome) {
            deviceName = "Chrome";
        } else if (browser.edge) {
            deviceName = "Edge";
        } else if (browser.firefox) {
            deviceName = "Firefox";
        } else if (browser.msie) {
            deviceName = "Internet Explorer";
        } else {
            deviceName = "Web Browser";
        }

        if (browser.version) {
            deviceName += " " + browser.version;
        }

        if (browser.ipad) {
            deviceName += " Ipad";
        } else if (browser.iphone) {
            deviceName += " Iphone";
        } else if (browser.android) {
            deviceName += " Android";
        }

        return deviceName;
    }

    function supportsVoiceInput() {
        return window.SpeechRecognition ||
               window.webkitSpeechRecognition ||
               window.mozSpeechRecognition ||
               window.oSpeechRecognition ||
               window.msSpeechRecognition;
    }

    var appInfo;
    var version = window.dashboardVersion || '3.0';

    return {
        getWindowState: function () {
            return document.windowState || 'Normal';
        },
        setWindowState: function (state) {
            alert('setWindowState is not supported and should not be called');
        },
        exit: function () {
            alert('exit is not supported and should not be called');
        },
        supports: function (command) {

            var features = [
                'filedownload',
                'externalpremium',
                'sharing'
            ];

            features.push('externallinks');

            if (supportsVoiceInput()) {
                features.push('voiceinput');
            }

            return features.indexOf(command.toLowerCase()) != -1;
        },
        appInfo: function () {

            if (appInfo) {
                return Promise.resolve(appInfo);
            }

            return getDeviceId().then(function (deviceId) {

                appInfo = {
                    deviceId: deviceId,
                    deviceName: getDeviceName(),
                    appName: 'Emby Mobile',
                    appVersion: version
                };

                return appInfo;
            });
        },
        capabilities: getCapabilities,

        moreIcon: browser.safari || browser.edge ? 'dots-horiz' : 'dots-vert'
    };
});
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

            require(["cryptojs-sha1"], function () {

                var keys = [];
                keys.push(navigator.userAgent);
                keys.push(new Date().getTime());

                resolve(CryptoJS.SHA1(keys.join('|')).toString());
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

        if (browser.tizen) {
            deviceName = "Samsung Smart TV";
        } else if (browser.web0S) {
            deviceName = "LG Smart TV";
        } else if (browser.operaTv) {
            deviceName = "Opera TV";
        } else if (browser.xboxOne) {
            deviceName = "Xbox One";
        } else if (browser.ps4) {
            deviceName = "Sony PS4";
        } else if (browser.chrome) {
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

        if (browser.tv) {
            return false;
        }

        return window.SpeechRecognition ||
               window.webkitSpeechRecognition ||
               window.mozSpeechRecognition ||
               window.oSpeechRecognition ||
               window.msSpeechRecognition;
    }

    function supportsFullscreen() {

        if (browser.tv) {
            return false;
        };

        var element = document.documentElement;

        return element.requestFullscreen ||
            element.mozRequestFullScreen ||
            element.webkitRequestFullscreen ||
            element.msRequestFullscreen;
    }

    var supportedFeatures = function () {

        var features = [
            'filedownload',
            'sharing',
            'externalpremium'
        ];

        if (browser.operaTv || browser.tizen || browser.web0s) {
            features.push('exit');
        } else {
            features.push('exitmenu');
        }

        if (!browser.operaTv) {
            features.push('externallinks');
        }

        if (supportsVoiceInput()) {
            features.push('voiceinput');
        }

        if (!browser.mobile || browser.edgeUwp) {
            features.push('htmlaudioautoplay');
            features.push('htmlvideoautoplay');
        }

        if (window.SyncRegistered) {
            //features.push('sync');
        }

        if (supportsFullscreen()) {
            features.push('fullscreen');
        }

        if (browser.chrome || (browser.safari && !browser.slow) || (browser.edge && !browser.slow)) {
            features.push('imageanalysis');
        }

        return features;
    }();

    var appInfo;
    var version = window.dashboardVersion || '3.0';

    return {
        dvrFeatureCode: 'dvr',
        getWindowState: function () {
            return document.windowState || 'Normal';
        },
        setWindowState: function (state) {
            alert('setWindowState is not supported and should not be called');
        },
        exit: function () {

            if (browser.tizen) {
                try {
                    tizen.application.getCurrentApplication().exit();
                } catch (err) {
                    console.log('error closing application: ' + err);
                }
                return;
            }

            window.close();
        },
        supports: function (command) {

            return supportedFeatures.indexOf(command.toLowerCase()) != -1;
        },
        unlockedFeatures: function () {

            var features = [];

            features.push('playback');
            features.push('livetv');

            return features;
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
        preferVisualCards: browser.android || browser.chrome,
        moreIcon: browser.safari || browser.edge ? 'dots-horiz' : 'dots-vert'
    };
});
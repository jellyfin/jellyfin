define(["appSettings", "browser", "events", "htmlMediaHelper"], function (appSettings, browser, events, htmlMediaHelper) {
    "use strict";

    function getBaseProfileOptions(item) {
        var disableHlsVideoAudioCodecs = [];

        if (item && htmlMediaHelper.enableHlsJsPlayer(item.RunTimeTicks, item.MediaType)) {
            if (browser.edge || browser.msie) {
                disableHlsVideoAudioCodecs.push("mp3");
            }

            disableHlsVideoAudioCodecs.push("ac3");
            disableHlsVideoAudioCodecs.push("eac3");
            disableHlsVideoAudioCodecs.push("opus");
        }

        return {
            enableMkvProgressive: false,
            disableHlsVideoAudioCodecs: disableHlsVideoAudioCodecs
        };
    }

    function getDeviceProfileForWindowsUwp(item) {
        return new Promise(function (resolve, reject) {
            require(["browserdeviceprofile", "environments/windows-uwp/mediacaps"], function (profileBuilder, uwpMediaCaps) {
                var profileOptions = getBaseProfileOptions(item);
                profileOptions.supportsDts = uwpMediaCaps.supportsDTS();
                profileOptions.supportsTrueHd = uwpMediaCaps.supportsDolby();
                profileOptions.audioChannels = uwpMediaCaps.getAudioChannels();
                resolve(profileBuilder(profileOptions));
            });
        });
    }

    function getDeviceProfile(item, options) {
        options = options || {};

        if (self.Windows) {
            return getDeviceProfileForWindowsUwp(item);
        }

        return new Promise(function (resolve, reject) {
            require(["browserdeviceprofile"], function (profileBuilder) {
                var profile = profileBuilder(getBaseProfileOptions(item));

                if (item && !options.isRetry && "allcomplexformats" !== appSettings.get("subtitleburnin")) {
                    if (!(browser.orsay || browser.tizen)) {
                        profile.SubtitleProfiles.push({
                            Format: "ass",
                            Method: "External"
                        });
                        profile.SubtitleProfiles.push({
                            Format: "ssa",
                            Method: "External"
                        });
                    }
                }

                resolve(profile);
            });
        });
    }

    function escapeRegExp(str) {
        return str.replace(/([.*+?^=!:${}()|\[\]\/\\])/g, "\\$1");
    }

    function replaceAll(originalString, strReplace, strWith) {
        var strReplace2 = escapeRegExp(strReplace);
        var reg = new RegExp(strReplace2, "ig");
        return originalString.replace(reg, strWith);
    }

    function generateDeviceId() {
        var keys = [];

        if (keys.push(navigator.userAgent), keys.push(new Date().getTime()), self.btoa) {
            var result = replaceAll(btoa(keys.join("|")), "=", "1");
            return Promise.resolve(result);
        }

        return Promise.resolve(new Date().getTime());
    }

    function getDeviceId() {
        var key = "_deviceId2";
        var deviceId = appSettings.get(key);

        if (deviceId) {
            return Promise.resolve(deviceId);
        }

        return generateDeviceId().then(function (deviceId) {
            appSettings.set(key, deviceId);
            return deviceId;
        });
    }

    function getDeviceName() {
        var deviceName = "Web Browser";

        if (browser.tizen) {
            deviceName = "Samsung Smart TV";
        }
        if (browser.web0s) {
            deviceName = "LG Smart TV";
        }
        if (browser.operaTv) {
            deviceName = "Opera TV";
        }
        if (browser.xboxOne) {
            deviceName = "Xbox One";
        }
        if (browser.ps4) {
            deviceName = "Sony PS4";
        }
        if (browser.chrome) {
            deviceName = "Chrome";
        }
        if (browser.edge) {
            deviceName = "Edge";
        }
        if (browser.firefox) {
            deviceName =  "Firefox";
        }
        if (browser.msie) {
            deviceName = "Internet Explorer";
        }
        if (browser.opera) {
            deviceName = "Opera";
        }

        if (browser.ipad) {
            deviceName += " Ipad";
        }
        if (browser.iphone) {
            deviceName += " Iphone";
        }
        if (browser.android) {
            deviceName += " Android";
        }

        return deviceName;
    }

    function supportsVoiceInput() {
        if (!browser.tv) {
            return window.SpeechRecognition ||
                window.webkitSpeechRecognition ||
                window.mozSpeechRecognition ||
                window.oSpeechRecognition ||
                window.msSpeechRecognition;
        }

        return false;
    }

    function supportsFullscreen() {
        if (browser.tv) {
            return false;
        }

        var element = document.documentElement;
        return !!(element.requestFullscreen || element.mozRequestFullScreen || element.webkitRequestFullscreen || element.msRequestFullscreen) || !!document.createElement("video").webkitEnterFullscreen;
    }

    function getSyncProfile() {
        return new Promise(function (resolve, reject) {
            require(["browserdeviceprofile", "appSettings"], function (profileBuilder, appSettings) {
                var profile = profileBuilder();
                profile.MaxStaticMusicBitrate = appSettings.maxStaticMusicBitrate();
                resolve(profile);
            });
        });
    }

    function getDefaultLayout() {
        return "desktop";
    }

    function supportsHtmlMediaAutoplay() {
        if (browser.edgeUwp || browser.tizen || browser.web0s || browser.orsay || browser.operaTv || browser.ps4 || browser.xboxOne) {
            return true;
        }

        if (browser.mobile) {
            return false;
        }

        var savedResult = appSettings.get(htmlMediaAutoplayAppStorageKey);
        return "true" === savedResult || "false" !== savedResult && null;
    }

    function cueSupported() {
        try {
            var video = document.createElement("video");
            var style = document.createElement("style");
            style.textContent = "video::cue {background: inherit}";
            document.body.appendChild(style);
            document.body.appendChild(video);
            var cue = window.getComputedStyle(video, "::cue").background;
            document.body.removeChild(style);
            document.body.removeChild(video);
            return !!cue.length;
        } catch (err) {
            console.log("Error detecting cue support:" + err);
            return false;
        }
    }

    function onAppVisible() {
        if (_isHidden) {
            _isHidden = false;
            console.log("triggering app resume event");
            events.trigger(appHost, "resume");
        }
    }

    function onAppHidden() {
        if (!_isHidden) {
            _isHidden = true;
            console.log("app is hidden");
        }
    }

    var htmlMediaAutoplayAppStorageKey = "supportshtmlmediaautoplay0";

    var supportedFeatures = function () {
        var features = [];

        if (navigator.share) {
            features.push("sharing");
        }

        if (!(browser.edgeUwp || browser.tv || browser.xboxOne || browser.ps4)) {
            features.push("filedownload");
        }

        if (browser.operaTv || browser.tizen || browser.orsay || browser.web0s) {
            features.push("exit");
        } else {
            features.push("exitmenu");
            features.push("plugins");
        }

        if (!(browser.operaTv || browser.tizen || browser.orsay || browser.web0s || browser.ps4)) {
            features.push("externallinks");
            features.push("externalpremium");
        }

        if (!browser.operaTv) {
            features.push("externallinkdisplay");
        }

        if (supportsVoiceInput()) {
            features.push("voiceinput");
        }

        if (!browser.tv && !browser.xboxOne) {
            browser.ps4;
        }

        if (supportsHtmlMediaAutoplay()) {
            features.push("htmlaudioautoplay");
            features.push("htmlvideoautoplay");
        }

        if (browser.edgeUwp) {
            features.push("sync");
        }

        if (supportsFullscreen()) {
            features.push("fullscreenchange");
        }

        if (browser.chrome || browser.edge && !browser.slow) {
            if (!(browser.noAnimation || browser.edgeUwp || browser.xboxOne)) {
                features.push("imageanalysis");
            }
        }

        if (Dashboard.isConnectMode()) {
            features.push("multiserver");
        }

        if (browser.tv || browser.xboxOne || browser.ps4 || browser.mobile) {
            features.push("physicalvolumecontrol");
        }

        if (!(browser.tv || browser.xboxOne || browser.ps4)) {
            features.push("remotecontrol");
        }

        if (!(browser.operaTv || browser.tizen || browser.orsay || browser.web0s || browser.edgeUwp)) {
            features.push("remotevideo");
        }

        features.push("otherapppromotions");
        features.push("targetblank");

        if (!(browser.orsay || browser.tizen || browser.msie || !(browser.firefox || browser.ps4 || browser.edge || cueSupported()))) {
            features.push("subtitleappearancesettings");
        }

        if (!(browser.orsay || browser.tizen)) {
            features.push("subtitleburnsettings");
        }

        if (!(browser.tv || browser.ps4 || browser.xboxOne)) {
            features.push("fileinput");
        }

        if (Dashboard.isConnectMode()) {
            features.push("displaylanguage");
        }

        if (browser.chrome) {
            features.push("chromecast");
        }

        return features;
    }();

    if (-1 === supportedFeatures.indexOf("htmlvideoautoplay") && false !== supportsHtmlMediaAutoplay()) {
        require(["autoPlayDetect"], function (autoPlayDetect) {
            autoPlayDetect.supportsHtmlMediaAutoplay().then(function () {
                appSettings.set(htmlMediaAutoplayAppStorageKey, "true");
                supportedFeatures.push("htmlvideoautoplay");
                supportedFeatures.push("htmlaudioautoplay");
            }, function () {
                appSettings.set(htmlMediaAutoplayAppStorageKey, "false");
            });
        });
    }

    var deviceId;
    var deviceName;
    var visibilityChange;
    var visibilityState;
    var appVersion = window.dashboardVersion || "3.0";
    var appHost = {
        getWindowState: function () {
            return document.windowState || "Normal";
        },
        setWindowState: function (state) {
            alert("setWindowState is not supported and should not be called");
        },
        exit: function () {
            if (browser.tizen) {
                try {
                    tizen.application.getCurrentApplication().exit();
                } catch (err) {
                    console.log("error closing application: " + err);
                }
            } else {
                window.close();
            }
        },
        supports: function (command) {
            return -1 !== supportedFeatures.indexOf(command.toLowerCase());
        },
        preferVisualCards: browser.android || browser.chrome,
        moreIcon: browser.android ? "dots-vert" : "dots-horiz",
        getSyncProfile: getSyncProfile,
        getDefaultLayout: getDefaultLayout,
        getDeviceProfile: getDeviceProfile,
        init: function () {
            deviceName = getDeviceName();
            return getDeviceId().then(function (resolvedDeviceId) {
                deviceId = resolvedDeviceId;
            });
        },
        deviceName: function () {
            return deviceName;
        },
        deviceId: function () {
            return deviceId;
        },
        appName: function () {
            return "Jellyfin Web";
        },
        appVersion: function () {
            return appVersion;
        },
        getPushTokenInfo: function () {
            return {};
        },
        setThemeColor: function (color) {
            var metaThemeColor = document.querySelector("meta[name=theme-color]");

            if (metaThemeColor) {
                metaThemeColor.setAttribute("content", color);
            }
        },
        setUserScalable: function (scalable) {
            if (!browser.tv) {
                var att = scalable ? "width=device-width, initial-scale=1, minimum-scale=1, user-scalable=yes" : "width=device-width, initial-scale=1, minimum-scale=1, maximum-scale=1, user-scalable=no";
                document.querySelector("meta[name=viewport]").setAttribute("content", att);
            }
        },
        // TODO: change urls
        deviceIconUrl: function () {
            if (browser.edgeUwp) {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/windowsrt.png";
            }

            if (browser.opera || browser.operaTv) {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/opera.png";
            }

            if (browser.orsay || browser.tizen) {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/samsungtv.png";
            }

            if (browser.web0s) {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/lgtv.png";
            }

            if (browser.ps4) {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/ps4.png";
            }

            if (browser.chromecast) {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/chromecast.png";
            }

            if (browser.chrome) {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/chrome.png";
            }

            if (browser.edge) {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/edge.png";
            }

            if (browser.firefox) {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/firefox.png";
            }

            if (browser.msie) {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/internetexplorer.png";
            }

            if (browser.safari) {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/safari.png";
            }

            return "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/html5.png";
        }
    };
    var doc = self.document;

    if (doc) {
        if (void 0 !== doc.visibilityState) {
            visibilityChange = "visibilitychange";
            visibilityState = "hidden";
        }
        if (void 0 !== doc.mozHidden) {
            visibilityChange = "mozvisibilitychange";
            visibilityState = "mozVisibilityState";
        }
        if (void 0 !== doc.msHidden) {
            visibilityChange = "msvisibilitychange";
            visibilityState = "msVisibilityState";
        }
        if (void 0 !== doc.webkitHidden) {
            visibilityChange = "webkitvisibilitychange";
            visibilityState = "webkitVisibilityState";
        }
    }

    var _isHidden = false;

    if (doc) {
        doc.addEventListener(visibilityChange, function () {
            if (document[visibilityState]) {
                onAppHidden();
            } else {
                onAppVisible();
            }
        });
    }

    if (self.addEventListener) {
        self.addEventListener("focus", onAppVisible);
        self.addEventListener("blur", onAppHidden);
    }

    return appHost;
});

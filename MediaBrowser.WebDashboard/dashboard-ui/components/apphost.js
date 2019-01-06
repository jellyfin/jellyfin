define(["appSettings", "browser", "events", "htmlMediaHelper"], function(appSettings, browser, events, htmlMediaHelper) {
    "use strict";

    function getBaseProfileOptions(item) {
        var disableHlsVideoAudioCodecs = [];
        return item && htmlMediaHelper.enableHlsJsPlayer(item.RunTimeTicks, item.MediaType) && ((browser.edge || browser.msie) && disableHlsVideoAudioCodecs.push("mp3"), disableHlsVideoAudioCodecs.push("ac3"), disableHlsVideoAudioCodecs.push("eac3"), disableHlsVideoAudioCodecs.push("opus")), {
            enableMkvProgressive: !1,
            disableHlsVideoAudioCodecs: disableHlsVideoAudioCodecs
        }
    }

    function getDeviceProfileForWindowsUwp(item) {
        return new Promise(function(resolve, reject) {
            require(["browserdeviceprofile", "environments/windows-uwp/mediacaps"], function(profileBuilder, uwpMediaCaps) {
                var profileOptions = getBaseProfileOptions(item);
                profileOptions.supportsDts = uwpMediaCaps.supportsDTS(), profileOptions.supportsTrueHd = uwpMediaCaps.supportsDolby(), profileOptions.audioChannels = uwpMediaCaps.getAudioChannels(), resolve(profileBuilder(profileOptions))
            })
        })
    }

    function getDeviceProfile(item, options) {
        return options = options || {}, self.Windows ? getDeviceProfileForWindowsUwp(item) : new Promise(function(resolve, reject) {
            require(["browserdeviceprofile"], function(profileBuilder) {
                var profile = profileBuilder(getBaseProfileOptions(item));
                item && !options.isRetry && "allcomplexformats" !== appSettings.get("subtitleburnin") && (browser.orsay || browser.tizen || (profile.SubtitleProfiles.push({
                    Format: "ass",
                    Method: "External"
                }), profile.SubtitleProfiles.push({
                    Format: "ssa",
                    Method: "External"
                }))), resolve(profile)
            })
        })
    }

    function escapeRegExp(str) {
        return str.replace(/([.*+?^=!:${}()|\[\]\/\\])/g, "\\$1")
    }

    function replaceAll(originalString, strReplace, strWith) {
        var strReplace2 = escapeRegExp(strReplace),
            reg = new RegExp(strReplace2, "ig");
        return originalString.replace(reg, strWith)
    }

    function generateDeviceId() {
        var keys = [];
        if (keys.push(navigator.userAgent), keys.push((new Date).getTime()), self.btoa) {
            var result = replaceAll(btoa(keys.join("|")), "=", "1");
            return Promise.resolve(result)
        }
        return Promise.resolve((new Date).getTime())
    }

    function getDeviceId() {
        var key = "_deviceId2",
            deviceId = appSettings.get(key);
        return deviceId ? Promise.resolve(deviceId) : generateDeviceId().then(function(deviceId) {
            return appSettings.set(key, deviceId), deviceId
        })
    }

    function getDeviceName() {
        var deviceName;
        return deviceName = browser.tizen ? "Samsung Smart TV" : browser.web0s ? "LG Smart TV" : browser.operaTv ? "Opera TV" : browser.xboxOne ? "Xbox One" : browser.ps4 ? "Sony PS4" : browser.chrome ? "Chrome" : browser.edge ? "Edge" : browser.firefox ? "Firefox" : browser.msie ? "Internet Explorer" : browser.opera ? "Opera" : "Web Browser", browser.ipad ? deviceName += " Ipad" : browser.iphone ? deviceName += " Iphone" : browser.android && (deviceName += " Android"), deviceName
    }

    function supportsVoiceInput() {
        return !browser.tv && (window.SpeechRecognition || window.webkitSpeechRecognition || window.mozSpeechRecognition || window.oSpeechRecognition || window.msSpeechRecognition)
    }

    function supportsFullscreen() {
        if (browser.tv) return !1;
        var element = document.documentElement;
        return !!(element.requestFullscreen || element.mozRequestFullScreen || element.webkitRequestFullscreen || element.msRequestFullscreen) || !!document.createElement("video").webkitEnterFullscreen
    }

    function getSyncProfile() {
        return new Promise(function(resolve, reject) {
            require(["browserdeviceprofile", "appSettings"], function(profileBuilder, appSettings) {
                var profile = profileBuilder();
                profile.MaxStaticMusicBitrate = appSettings.maxStaticMusicBitrate(), resolve(profile)
            })
        })
    }

    function getDefaultLayout() {
        return "desktop"
    }

    function supportsHtmlMediaAutoplay() {
        if (browser.edgeUwp || browser.tizen || browser.web0s || browser.orsay || browser.operaTv || browser.ps4 || browser.xboxOne) return !0;
        if (browser.mobile) return !1;
        var savedResult = appSettings.get(htmlMediaAutoplayAppStorageKey);
        return "true" === savedResult || "false" !== savedResult && null
    }

    function cueSupported() {
        try {
            var video = document.createElement("video"),
                style = document.createElement("style");
            style.textContent = "video::cue {background: inherit}", document.body.appendChild(style), document.body.appendChild(video);
            var cue = window.getComputedStyle(video, "::cue").background;
            return document.body.removeChild(style), document.body.removeChild(video), !!cue.length
        } catch (err) {
            return console.log("Error detecting cue support:" + err), !1
        }
    }

    function onAppVisible() {
        _isHidden && (_isHidden = !1, console.log("triggering app resume event"), events.trigger(appHost, "resume"))
    }

    function onAppHidden() {
        _isHidden || (_isHidden = !0, console.log("app is hidden"))
    }
    var htmlMediaAutoplayAppStorageKey = "supportshtmlmediaautoplay0",
        supportedFeatures = function() {
            var features = [];
            return navigator.share && features.push("sharing"), browser.edgeUwp || browser.tv || browser.xboxOne || browser.ps4 || features.push("filedownload"), browser.operaTv || browser.tizen || browser.orsay || browser.web0s ? features.push("exit") : (features.push("exitmenu"), features.push("plugins")), browser.operaTv || browser.tizen || browser.orsay || browser.web0s || browser.ps4 || (features.push("externallinks"), features.push("externalpremium")), browser.operaTv || features.push("externallinkdisplay"), supportsVoiceInput() && features.push("voiceinput"), !browser.tv && !browser.xboxOne && browser.ps4, supportsHtmlMediaAutoplay() && (features.push("htmlaudioautoplay"), features.push("htmlvideoautoplay")), browser.edgeUwp && features.push("sync"), supportsFullscreen() && features.push("fullscreenchange"), (browser.chrome || browser.edge && !browser.slow) && (browser.noAnimation || browser.edgeUwp || browser.xboxOne || features.push("imageanalysis")), AppInfo.isNativeApp && features.push("multiserver"), (browser.tv || browser.xboxOne || browser.ps4 || browser.mobile) && features.push("physicalvolumecontrol"), browser.tv || browser.xboxOne || browser.ps4 || features.push("remotecontrol"), browser.operaTv || browser.tizen || browser.orsay || browser.web0s || browser.edgeUwp || features.push("remotevideo"), features.push("otherapppromotions"), features.push("targetblank"), browser.orsay || browser.tizen || browser.msie || !(browser.firefox || browser.ps4 || browser.edge || cueSupported()) || features.push("subtitleappearancesettings"), browser.orsay || browser.tizen || features.push("subtitleburnsettings"), browser.tv || browser.ps4 || browser.xboxOne || features.push("fileinput"), AppInfo.isNativeApp && features.push("displaylanguage"), browser.chrome && features.push("chromecast"), features
        }(); - 1 === supportedFeatures.indexOf("htmlvideoautoplay") && !1 !== supportsHtmlMediaAutoplay() && require(["autoPlayDetect"], function(autoPlayDetect) {
        autoPlayDetect.supportsHtmlMediaAutoplay().then(function() {
            appSettings.set(htmlMediaAutoplayAppStorageKey, "true"), supportedFeatures.push("htmlvideoautoplay"), supportedFeatures.push("htmlaudioautoplay")
        }, function() {
            appSettings.set(htmlMediaAutoplayAppStorageKey, "false")
        })
    });
    var deviceId, deviceName, visibilityChange, visibilityState, appVersion = window.dashboardVersion || "3.0",
        appHost = {
            getWindowState: function() {
                return document.windowState || "Normal"
            },
            setWindowState: function(state) {
                alert("setWindowState is not supported and should not be called")
            },
            exit: function() {
                if (browser.tizen) try {
                    tizen.application.getCurrentApplication().exit()
                } catch (err) {
                    console.log("error closing application: " + err)
                } else window.close()
            },
            supports: function(command) {
                return -1 !== supportedFeatures.indexOf(command.toLowerCase())
            },
            preferVisualCards: browser.android || browser.chrome,
            moreIcon: browser.android ? "dots-vert" : "dots-horiz",
            getSyncProfile: getSyncProfile,
            getDefaultLayout: getDefaultLayout,
            getDeviceProfile: getDeviceProfile,
            init: function() {
                return deviceName = getDeviceName(), getDeviceId().then(function(resolvedDeviceId) {
                    deviceId = resolvedDeviceId
                })
            },
            deviceName: function() {
                return deviceName
            },
            deviceId: function() {
                return deviceId
            },
            appName: function() {
                return "Jellyfin Web"
            },
            appVersion: function() {
                return appVersion
            },
            getPushTokenInfo: function() {
                return {}
            },
            setThemeColor: function(color) {
                var metaThemeColor = document.querySelector("meta[name=theme-color]");
                metaThemeColor && metaThemeColor.setAttribute("content", color)
            },
            setUserScalable: function(scalable) {
                if (!browser.tv) {
                    var att = scalable ? "width=device-width, initial-scale=1, minimum-scale=1, user-scalable=yes" : "width=device-width, initial-scale=1, minimum-scale=1, maximum-scale=1, user-scalable=no";
                    document.querySelector("meta[name=viewport]").setAttribute("content", att)
                }
            },
            deviceIconUrl: function() {
                return browser.edgeUwp, browser.edgeUwp ? "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/windowsrt.png" : browser.opera || browser.operaTv ? "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/opera.png" : browser.orsay || browser.tizen ? "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/samsungtv.png" : browser.web0s ? "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/lgtv.png" : browser.ps4 ? "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/ps4.png" : browser.chromecast ? "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/chromecast.png" : browser.chrome ? "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/chrome.png" : browser.edge ? "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/edge.png" : browser.firefox ? "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/firefox.png" : browser.msie ? "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/internetexplorer.png" : browser.safari ? "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/safari.png" : "https://github.com/MediaBrowser/Emby.Resources/raw/master/images/devices/html5.png"
            }
        },
        doc = self.document;
    doc && (void 0 !== doc.visibilityState ? (visibilityChange = "visibilitychange", visibilityState = "hidden") : void 0 !== doc.mozHidden ? (visibilityChange = "mozvisibilitychange", visibilityState = "mozVisibilityState") : void 0 !== doc.msHidden ? (visibilityChange = "msvisibilitychange", visibilityState = "msVisibilityState") : void 0 !== doc.webkitHidden && (visibilityChange = "webkitvisibilitychange", visibilityState = "webkitVisibilityState"));
    var _isHidden = !1;
    return doc && doc.addEventListener(visibilityChange, function() {
        document[visibilityState] ? onAppHidden() : onAppVisible()
    }), self.addEventListener && (self.addEventListener("focus", onAppVisible), self.addEventListener("blur", onAppHidden)), appHost
});

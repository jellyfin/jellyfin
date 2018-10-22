define([], function() {
    "use strict";

    function supportsCssAnimation(allowPrefix) {
        if (allowPrefix) {
            if (!0 === _supportsCssAnimationWithPrefix || !1 === _supportsCssAnimationWithPrefix) return _supportsCssAnimationWithPrefix
        } else if (!0 === _supportsCssAnimation || !1 === _supportsCssAnimation) return _supportsCssAnimation;
        var animation = !1,
            domPrefixes = ["Webkit", "O", "Moz"],
            pfx = "",
            elm = document.createElement("div");
        if (void 0 !== elm.style.animationName && (animation = !0), !1 === animation && allowPrefix)
            for (var i = 0; i < domPrefixes.length; i++)
                if (void 0 !== elm.style[domPrefixes[i] + "AnimationName"]) {
                    pfx = domPrefixes[i], pfx + "Animation", "-" + pfx.toLowerCase() + "-", animation = !0;
                    break
                } return allowPrefix ? _supportsCssAnimationWithPrefix = animation : _supportsCssAnimation = animation
    }
    var _supportsCssAnimation, _supportsCssAnimationWithPrefix, userAgent = navigator.userAgent,
        matched = function(ua) {
            ua = ua.toLowerCase();
            var match = /(edge)[ \/]([\w.]+)/.exec(ua) || /(opera)[ \/]([\w.]+)/.exec(ua) || /(opr)[ \/]([\w.]+)/.exec(ua) || /(chrome)[ \/]([\w.]+)/.exec(ua) || /(safari)[ \/]([\w.]+)/.exec(ua) || /(firefox)[ \/]([\w.]+)/.exec(ua) || /(msie) ([\w.]+)/.exec(ua) || ua.indexOf("compatible") < 0 && /(mozilla)(?:.*? rv:([\w.]+)|)/.exec(ua) || [],
                versionMatch = /(version)[ \/]([\w.]+)/.exec(ua),
                platform_match = /(ipad)/.exec(ua) || /(iphone)/.exec(ua) || /(windows)/.exec(ua) || /(android)/.exec(ua) || [],
                browser = match[1] || "";
            "edge" === browser ? platform_match = [""] : -1 !== ua.indexOf("windows phone") || -1 !== ua.indexOf("iemobile") ? browser = "msie" : -1 !== ua.indexOf("like gecko") && -1 === ua.indexOf("webkit") && -1 === ua.indexOf("opera") && -1 === ua.indexOf("chrome") && -1 === ua.indexOf("safari") && (browser = "msie"), "opr" === browser && (browser = "opera");
            var version;
            versionMatch && versionMatch.length > 2 && (version = versionMatch[2]), version = version || match[2] || "0";
            var versionMajor = parseInt(version.split(".")[0]);
            return isNaN(versionMajor) && (versionMajor = 0), {
                browser: browser,
                version: version,
                platform: platform_match[0] || "",
                versionMajor: versionMajor
            }
        }(userAgent),
        browser = {};
    return matched.browser && (browser[matched.browser] = !0, browser.version = matched.version, browser.versionMajor = matched.versionMajor), matched.platform && (browser[matched.platform] = !0), browser.chrome || browser.msie || browser.edge || browser.opera || -1 === userAgent.toLowerCase().indexOf("webkit") || (browser.safari = !0), -1 !== userAgent.toLowerCase().indexOf("playstation 4") && (browser.ps4 = !0, browser.tv = !0),
        function(userAgent) {
            for (var terms = ["mobi", "ipad", "iphone", "ipod", "silk", "gt-p1000", "nexus 7", "kindle fire", "opera mini"], lower = userAgent.toLowerCase(), i = 0, length = terms.length; i < length; i++)
                if (-1 !== lower.indexOf(terms[i])) return !0;
            return !1
        }(userAgent) && (browser.mobile = !0), browser.xboxOne = -1 !== userAgent.toLowerCase().indexOf("xbox"), browser.animate = "undefined" != typeof document && null != document.documentElement.animate, browser.tizen = -1 !== userAgent.toLowerCase().indexOf("tizen") || null != self.tizen, browser.web0s = -1 !== userAgent.toLowerCase().indexOf("Web0S".toLowerCase()), browser.edgeUwp = browser.edge && (-1 !== userAgent.toLowerCase().indexOf("msapphost") || -1 !== userAgent.toLowerCase().indexOf("webview")), browser.tizen || (browser.orsay = -1 !== userAgent.toLowerCase().indexOf("smarthub")), browser.edgeUwp && (browser.edge = !0), browser.tv = function() {
            var userAgent = navigator.userAgent.toLowerCase();
            return -1 !== userAgent.indexOf("tv") || (-1 !== userAgent.indexOf("samsungbrowser") || (-1 !== userAgent.indexOf("nintendo") || (-1 !== userAgent.indexOf("viera") || -1 !== userAgent.indexOf("webos"))))
        }(), browser.operaTv = browser.tv && -1 !== userAgent.toLowerCase().indexOf("opr/"),
        function(prop, value) {
            if ("undefined" == typeof window) return !1;
            if (value = 2 === arguments.length ? value : "inherit", "CSS" in window && "supports" in window.CSS) return window.CSS.supports(prop, value);
            if ("supportsCSS" in window) return window.supportsCSS(prop, value);
            try {
                var camel = prop.replace(/-([a-z]|[0-9])/gi, function(all, letter) {
                        return (letter + "").toUpperCase()
                    }),
                    support = camel in el.style,
                    el = document.createElement("div");
                return el.style.cssText = prop + ":" + value, support && "" !== el.style[camel]
            } catch (err) {
                return !1
            }
        }("display", "flex") || (browser.noFlex = !0), (browser.mobile || browser.tv) && (browser.slow = !0), "undefined" != typeof document && ("ontouchstart" in window || window.DocumentTouch && document instanceof DocumentTouch) && (browser.touch = !0), browser.keyboard = function(browser) {
            return !!browser.touch || (!!browser.xboxOne || (!!browser.ps4 || (!!browser.edgeUwp || !!browser.tv)))
        }(browser), browser.supportsCssAnimation = supportsCssAnimation, browser.osx = -1 !== userAgent.toLowerCase().indexOf("os x"), browser.iOS = browser.ipad || browser.iphone || browser.ipod, browser.iOS && (browser.iOSVersion = function() {
            if (/iP(hone|od|ad)/.test(navigator.platform)) {
                var v = navigator.appVersion.match(/OS (\d+)_(\d+)_?(\d+)?/);
                return [parseInt(v[1], 10), parseInt(v[2], 10), parseInt(v[3] || 0, 10)]
            }
        }(), browser.iOSVersion = browser.iOSVersion[0] + browser.iOSVersion[1] / 10), browser.chromecast = browser.chrome && -1 !== userAgent.toLowerCase().indexOf("crkey"), browser
});
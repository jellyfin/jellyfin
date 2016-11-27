define(['events'], function (events) {
    'use strict';

    // LinkParser
    //
    // https://github.com/ravisorg/LinkParser
    //
    // Locate and extract almost any URL within a string. Handles protocol-less domains, IPv4 and 
    // IPv6, unrecognised TLDs, and more.
    //
    // This work is licensed under a Creative Commons Attribution-ShareAlike 4.0 International License.
    // http://creativecommons.org/licenses/by-sa/4.0/
    (function () {

        // Original URL regex from the Android android.text.util.Linkify function, found here:
        // http://stackoverflow.com/a/19696443
        // 
        // However there were problems with it, most probably related to the fact it was 
        // written in 2007, and it's been highly modified.
        // 
        // 1) I didn't like the fact that it was tied to specific TLDs, since new ones 
        // are being added all the time it wouldn't be reasonable to expect developer to
        // be continually updating their regular expressions.
        // 
        // 2) It didn't allow unicode characters in the domains which are now allowed in 
        // many languages, (including some IDN TLDs). Again these are constantly being
        // added to and it doesn't seem reasonable to hard-code them. Note this ended up
        // not being possible in standard JS due to the way it handles multibyte strings.
        // It is possible using XRegExp, however a big performance hit results. Disabled
        // for now.
        // 
        // 3) It didn't allow for IPv6 hostnames
        // IPv6 regex from http://stackoverflow.com/a/17871737
        //
        // 4) It was very poorly commented
        // 
        // 5) It wasn't as smart as it could have been about what should be part of a
        // URL and what should be part of human language.

        var protocols = "(?:(?:http|https|rtsp|ftp):\\/\\/)";
        var credentials = "(?:(?:[a-z0-9\\$\\-\\_\\.\\+\\!\\*\\'\\(\\)\\,\\;\\?\\&\\=]|(?:\\%[a-f0-9]{2})){1,64}" // username (1-64 normal or url escaped characters)
            + "(?:\\:(?:[a-z0-9\\$\\-\\_\\.\\+\\!\\*\\'\\(\\)\\,\\;\\?\\&\\=]|(?:\\%[a-f0-9]{2})){1,25})?" // followed by optional password (: + 1-25 normal or url escaped characters)
            + "\\@)";

        // IPv6 Regex http://forums.intermapper.com/viewtopic.php?t=452
        // by Dartware, LLC is licensed under a Creative Commons Attribution-ShareAlike 3.0 Unported License
        // http://intermapper.com/
        var ipv6 = "("
            + "(([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))"
            + "|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))"
            + "|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))"
            + "|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))"
            + "|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))"
            + "|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))"
            + "|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))"
            + "|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))"
            + ")(%.+)?";

        var ipv4 = "(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9])\\."
            + "(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9]|0)\\."
            + "(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9]|0)\\."
            + "(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[0-9])";

        // This would have been a lot cleaner if JS RegExp supported conditionals...
        var linkRegExpString =

            // begin match for protocol / username / password / host
            "(?:"

                // ============================
                // If we have a recognized protocol at the beginning of the URL, we're
                // more relaxed about what we accept, because we assume the user wants
                // this to be a URL, and we're not accidentally matching human language
                + protocols + "?"

                // optional username:password@
                + credentials + "?"

                // IP address (both v4 and v6)
                + "(?:"

                    // IPv6
                    + ipv6

                    // IPv4
                    + "|" + ipv4

                + ")"

            // end match for protocol / username / password / host
            + ")"

            // optional port number
            + "(?:\\:\\d{1,5})?"

            // plus optional path and query params (no unicode allowed here?)
            + "(?:"
                + "\\/(?:"
                    // some characters we'll accept because it's unlikely human language
                    // would use them after a URL unless they were part of the url
                    + "(?:[a-z0-9\\/\\@\\&\\#\\~\\*\\_\\-\\+])"
                    + "|(?:\\%[a-f0-9]{2})"
                    // some characters are much more likely to be used AFTER a url and
                    // were not intended to be included in the url itself. Mostly end
                    // of sentence type things. It's also likely that the URL would 
                    // still work if any of these characters were missing from the end 
                    // because we parsed it incorrectly. For these characters to be accepted
                    // they must be followed by another character that we're reasonably
                    // sure is part of the url
                    + "|(?:[\\;\\?\\:\\.\\!\\'\\(\\)\\,\\=]+(?=(?:[a-z0-9\\/\\@\\&\\#\\~\\*\\_\\-\\+])|(?:\\%[a-f0-9]{2})))"
                + ")*"
                + "|\\b|\$"
            + ")";

        // regex = XRegExp(regex,'gi');
        var linkRegExp = RegExp(linkRegExpString, 'gi');

        var protocolRegExp = RegExp('^' + protocols, 'i');

        // if url doesn't begin with a known protocol, add http by default
        function ensureProtocol(url) {
            if (!url.match(protocolRegExp)) {
                url = "http://" + url;
            }
            return url;
        }

        // look for links in the text
        var LinkParser = {

            parse: function (text) {
                var links = [];
                var match;

                while (match = linkRegExp.exec(text)) {
                    // console.log(matches);
                    var txt = match[0];
                    var pos = match['index'];
                    var len = txt.length;
                    var url = ensureProtocol(text);
                    links.push({ 'pos': pos, 'text': txt, 'len': len, 'url': url });
                }

                return links;
            }

        }

        window.LinkParser = LinkParser;
    })();

    var cache = {};

    function getEndpointInfo(apiClient) {

        return apiClient.getJSON(apiClient.getUrl('System/Endpoint'));
    }

    function isValidIpAddress(address) {

        var links = LinkParser.parse(address);

        return links.length == 1;
    }

    function isLocalIpAddress(address) {

        address = address.toLowerCase();

        if (address.indexOf('127.0.0.1') != -1) {
            return true;
        }
        if (address.indexOf('localhost') != -1) {
            return true;
        }

        return false;
    }

    function getServerAddress(apiClient) {

        var serverAddress = apiClient.serverAddress();

        if (isValidIpAddress(serverAddress) && !isLocalIpAddress(serverAddress)) {
            return Promise.resolve(serverAddress);
        }

        var cachedValue = getCachedValue(serverAddress);
        if (cachedValue) {
            return Promise.resolve(cachedValue);
        }

        return apiClient.getJSON(apiClient.getUrl('System/Endpoint')).then(function (endpoint) {
            if (endpoint.IsInNetwork) {
                return apiClient.getPublicSystemInfo().then(function (info) {
                    addToCache(serverAddress, info.LocalAddress);
                    return info.LocalAddress;
                });
            } else {
                addToCache(serverAddress, serverAddress);
                return serverAddress;
            }
        });
    }

    function clearCache() {
        cache = {};
    }

    function addToCache(key, value) {
        cache[key] = {
            value: value,
            time: new Date().getTime()
        };
    }

    function getCachedValue(key) {

        var obj = cache[key];

        if (obj && (new Date().getTime() - obj.time) < 180000) {
            return obj.value;
        }

        return null;
    }

    events.on(ConnectionManager, 'localusersignedin', clearCache);
    events.on(ConnectionManager, 'localusersignedout', clearCache);

    return {
        getServerAddress: getServerAddress
    };
});
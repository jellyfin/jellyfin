define(["events"], function(events) {
    "use strict";

    function isValidIpAddress(address) {
        return 1 == LinkParser.parse(address).length
    }

    function isLocalIpAddress(address) {
        return address = address.toLowerCase(), -1 !== address.indexOf("127.0.0.1") || -1 !== address.indexOf("localhost")
    }

    function getServerAddress(apiClient) {
        var serverAddress = apiClient.serverAddress();
        if (isValidIpAddress(serverAddress) && !isLocalIpAddress(serverAddress)) return Promise.resolve(serverAddress);
        var cachedValue = getCachedValue(serverAddress);
        return cachedValue ? Promise.resolve(cachedValue) : apiClient.getEndpointInfo().then(function(endpoint) {
            return endpoint.IsInNetwork ? apiClient.getPublicSystemInfo().then(function(info) {
                return addToCache(serverAddress, info.LocalAddress), info.LocalAddress
            }) : (addToCache(serverAddress, serverAddress), serverAddress)
        })
    }

    function clearCache() {
        cache = {}
    }

    function addToCache(key, value) {
        cache[key] = {
            value: value,
            time: (new Date).getTime()
        }
    }

    function getCachedValue(key) {
        var obj = cache[key];
        return obj && (new Date).getTime() - obj.time < 18e4 ? obj.value : null
    }! function() {
        function ensureProtocol(url) {
            return url.match(protocolRegExp) || (url = "http://" + url), url
        }
        var protocols = "(?:(?:http|https|rtsp|ftp):\\/\\/)",
            linkRegExp = RegExp("(?:(?:(?:http|https|rtsp|ftp):\\/\\/)?(?:(?:[a-z0-9\\$\\-\\_\\.\\+\\!\\*\\'\\(\\)\\,\\;\\?\\&\\=]|(?:\\%[a-f0-9]{2})){1,64}(?:\\:(?:[a-z0-9\\$\\-\\_\\.\\+\\!\\*\\'\\(\\)\\,\\;\\?\\&\\=]|(?:\\%[a-f0-9]{2})){1,25})?\\@)?(?:((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]d|1dd|[1-9]?d)(.(25[0-5]|2[0-4]d|1dd|[1-9]?d)){3})|:))|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]d|1dd|[1-9]?d)(.(25[0-5]|2[0-4]d|1dd|[1-9]?d)){3})|:))|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]d|1dd|[1-9]?d)(.(25[0-5]|2[0-4]d|1dd|[1-9]?d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]d|1dd|[1-9]?d)(.(25[0-5]|2[0-4]d|1dd|[1-9]?d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]d|1dd|[1-9]?d)(.(25[0-5]|2[0-4]d|1dd|[1-9]?d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]d|1dd|[1-9]?d)(.(25[0-5]|2[0-4]d|1dd|[1-9]?d)){3}))|:))|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]d|1dd|[1-9]?d)(.(25[0-5]|2[0-4]d|1dd|[1-9]?d)){3}))|:)))(%.+)?|(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9])\\.(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9]|0)\\.(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9]|0)\\.(?:25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[0-9])))(?:\\:\\d{1,5})?(?:\\/(?:(?:[a-z0-9\\/\\@\\&\\#\\~\\*\\_\\-\\+])|(?:\\%[a-f0-9]{2})|(?:[\\;\\?\\:\\.\\!\\'\\(\\)\\,\\=]+(?=(?:[a-z0-9\\/\\@\\&\\#\\~\\*\\_\\-\\+])|(?:\\%[a-f0-9]{2}))))*|\\b|$)", "gi"),
            protocolRegExp = RegExp("^" + protocols, "i"),
            LinkParser = {
                parse: function(text) {
                    for (var match, links = []; match = linkRegExp.exec(text);) {
                        var txt = match[0],
                            pos = match.index,
                            len = txt.length,
                            url = ensureProtocol(text);
                        links.push({
                            pos: pos,
                            text: txt,
                            len: len,
                            url: url
                        })
                    }
                    return links
                }
            };
        window.LinkParser = LinkParser
    }();
    var cache = {};
    return events.on(ConnectionManager, "localusersignedin", clearCache), events.on(ConnectionManager, "localusersignedout", clearCache), {
        getServerAddress: getServerAddress
    }
});
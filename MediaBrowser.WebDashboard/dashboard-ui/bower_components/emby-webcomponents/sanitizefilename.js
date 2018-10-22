define([], function() {
    "use strict";

    function isHighSurrogate(codePoint) {
        return codePoint >= 55296 && codePoint <= 56319
    }

    function isLowSurrogate(codePoint) {
        return codePoint >= 56320 && codePoint <= 57343
    }

    function getByteLength(string) {
        if ("string" != typeof string) throw new Error("Input must be string");
        for (var charLength = string.length, byteLength = 0, codePoint = null, prevCodePoint = null, i = 0; i < charLength; i++) codePoint = string.charCodeAt(i), isLowSurrogate(codePoint) ? null != prevCodePoint && isHighSurrogate(prevCodePoint) ? byteLength += 1 : byteLength += 3 : codePoint <= 127 ? byteLength += 1 : codePoint >= 128 && codePoint <= 2047 ? byteLength += 2 : codePoint >= 2048 && codePoint <= 65535 && (byteLength += 3), prevCodePoint = codePoint;
        return byteLength
    }

    function truncate(string, byteLength) {
        if ("string" != typeof string) throw new Error("Input must be string");
        for (var codePoint, segment, charLength = string.length, curByteLength = 0, i = 0; i < charLength; i += 1) {
            if (codePoint = string.charCodeAt(i), segment = string[i], isHighSurrogate(codePoint) && isLowSurrogate(string.charCodeAt(i + 1)) && (i += 1, segment += string[i]), (curByteLength += getByteLength(segment)) === byteLength) return string.slice(0, i + 1);
            if (curByteLength > byteLength) return string.slice(0, i - segment.length + 1)
        }
        return string
    }
    var illegalRe = /[\/\?<>\\:\*\|":]/g,
        controlRe = /[\x00-\x1f\x80-\x9f]/g,
        reservedRe = /^\.+$/,
        windowsReservedRe = /^(con|prn|aux|nul|com[0-9]|lpt[0-9])(\..*)?$/i,
        windowsTrailingRe = /[\. ]+$/;
    return {
        sanitize: function(input, replacement) {
            return truncate(input.replace(illegalRe, replacement).replace(controlRe, replacement).replace(reservedRe, replacement).replace(windowsReservedRe, replacement).replace(windowsTrailingRe, replacement), 255)
        }
    }
});
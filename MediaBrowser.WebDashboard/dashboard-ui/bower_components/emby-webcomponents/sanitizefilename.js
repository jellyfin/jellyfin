// From https://github.com/parshap/node-sanitize-filename

define([], function () {
    'use strict';

    var illegalRe = /[\/\?<>\\:\*\|":]/g;
    var controlRe = /[\x00-\x1f\x80-\x9f]/g;
    var reservedRe = /^\.+$/;
    var windowsReservedRe = /^(con|prn|aux|nul|com[0-9]|lpt[0-9])(\..*)?$/i;
    var windowsTrailingRe = /[\. ]+$/;

    function isHighSurrogate(codePoint) {
        return codePoint >= 0xd800 && codePoint <= 0xdbff;
    }

    function isLowSurrogate(codePoint) {
        return codePoint >= 0xdc00 && codePoint <= 0xdfff;
    }

    function getByteLength(string) {
        if (typeof string !== "string") {
            throw new Error("Input must be string");
        }

        var charLength = string.length;
        var byteLength = 0;
        var codePoint = null;
        var prevCodePoint = null;
        for (var i = 0; i < charLength; i++) {
            codePoint = string.charCodeAt(i);
            // handle 4-byte non-BMP chars
            // low surrogate
            if (isLowSurrogate(codePoint)) {
                // when parsing previous hi-surrogate, 3 is added to byteLength
                if (prevCodePoint != null && isHighSurrogate(prevCodePoint)) {
                    byteLength += 1;
                }
                else {
                    byteLength += 3;
                }
            }
            else if (codePoint <= 0x7f) {
                byteLength += 1;
            }
            else if (codePoint >= 0x80 && codePoint <= 0x7ff) {
                byteLength += 2;
            }
            else if (codePoint >= 0x800 && codePoint <= 0xffff) {
                byteLength += 3;
            }
            prevCodePoint = codePoint;
        }

        return byteLength;
    }

    function truncate(string, byteLength) {
        if (typeof string !== "string") {
            throw new Error("Input must be string");
        }

        var charLength = string.length;
        var curByteLength = 0;
        var codePoint;
        var segment;

        for (var i = 0; i < charLength; i += 1) {
            codePoint = string.charCodeAt(i);
            segment = string[i];

            if (isHighSurrogate(codePoint) && isLowSurrogate(string.charCodeAt(i + 1))) {
                i += 1;
                segment += string[i];
            }

            curByteLength += getByteLength(segment);

            if (curByteLength === byteLength) {
                return string.slice(0, i + 1);
            }
            else if (curByteLength > byteLength) {
                return string.slice(0, i - segment.length + 1);
            }
        }

        return string;
    }

    return {
        sanitize: function (input, replacement) {
            var sanitized = input
              .replace(illegalRe, replacement)
              .replace(controlRe, replacement)
              .replace(reservedRe, replacement)
              .replace(windowsReservedRe, replacement)
              .replace(windowsTrailingRe, replacement);
            return truncate(sanitized, 255);
        }
    };
});
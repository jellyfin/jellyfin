// Array Remove - By John Resig (MIT Licensed)
Array.prototype.remove = function (from, to) {
    var rest = this.slice((to || from) + 1 || this.length);
    this.length = from < 0 ? this.length + from : from;
    return this.push.apply(this, rest);
};

String.prototype.endsWith = function (suffix) {
    return this.indexOf(suffix, this.length - suffix.length) !== -1;
};

$.fn.checked = function (value) {
    if (value === true || value === false) {
        // Set the value of the checkbox
        return $(this).each(function () {
            this.checked = value;
        });
    } else {
        // Return check state
        return $(this).is(':checked');
    }
};

var WebNotifications = {

    show: function (data) {
        if (window.webkitNotifications) {
            if (!webkitNotifications.checkPermission()) {
                var notif = webkitNotifications.createNotification(data.icon, data.title, data.body);
                notif.show();

                if (data.timeout) {
                    setTimeout(function () {
                        notif.cancel();
                    }, data.timeout);
                }

                return notif;
            } else {
                webkitNotifications.requestPermission(function () {
                    return WebNotifications.show(data);
                });
            }
        }
        else if (window.Notification) {
            if (Notification.permissionLevel() === "granted") {
                var notif = new Notification(data.title, data);
                notif.show();

                if (data.timeout) {
                    setTimeout(function () {
                        notif.cancel();
                    }, data.timeout);
                }

                return notif;
            } else if (Notification.permissionLevel() === "default") {
                Notification.requestPermission(function () {
                    return WebNotifications.show(data);
                });
            }
        }
    },

    requestPermission: function () {
        if (window.webkitNotifications) {
            if (!webkitNotifications.checkPermission()) {
            } else {
                webkitNotifications.requestPermission(function () {
                });
            }
        }
        else if (window.Notification) {
            if (Notification.permissionLevel() === "granted") {
            } else if (Notification.permissionLevel() === "default") {
                Notification.requestPermission(function () {
                });
            }
        }
    }
};

/*
 * Javascript Humane Dates
 * Copyright (c) 2008 Dean Landolt (deanlandolt.com)
 * Re-write by Zach Leatherman (zachleat.com)
 *
 * Adopted from the John Resig's pretty.js
 * at http://ejohn.org/blog/javascript-pretty-date
 * and henrah's proposed modification
 * at http://ejohn.org/blog/javascript-pretty-date/#comment-297458
 *
 * Licensed under the MIT license.
 */

function humane_date(date_str) {
    var time_formats = [[90, 'a minute'], // 60*1.5
    [3600, 'minutes', 60], // 60*60, 60
    [5400, 'an hour'], // 60*60*1.5
    [86400, 'hours', 3600], // 60*60*24, 60*60
    [129600, 'a day'], // 60*60*24*1.5
    [604800, 'days', 86400], // 60*60*24*7, 60*60*24
    [907200, 'a week'], // 60*60*24*7*1.5
    [2628000, 'weeks', 604800], // 60*60*24*(365/12), 60*60*24*7
    [3942000, 'a month'], // 60*60*24*(365/12)*1.5
    [31536000, 'months', 2628000], // 60*60*24*365, 60*60*24*(365/12)
    [47304000, 'a year'], // 60*60*24*365*1.5
    [3153600000, 'years', 31536000] // 60*60*24*365*100, 60*60*24*365
    ];

    var dt = new Date;
    var date = parseISO8601Date(date_str, true);

    var seconds = ((dt - date) / 1000);
    var token = ' ago';
    var i = 0;
    var format;

    if (seconds < 0) {
        seconds = Math.abs(seconds);
        token = '';
    }

    while (format = time_formats[i++]) {
        if (seconds < format[0]) {
            if (format.length == 2) {
                return format[1] + token;
            } else {
                return Math.round(seconds / format[2]) + ' ' + format[1] + token;
            }
        }
    }

    // overflow for centuries
    if (seconds > 4730400000)
        return Math.round(seconds / 4730400000) + ' centuries' + token;

    return date_str;
};

function humane_elapsed(firstDateStr, secondDateStr) {
    var dt1 = new Date(firstDateStr);
    var dt2 = new Date(secondDateStr);
    var seconds = (dt2.getTime() - dt1.getTime()) / 1000;
    var numdays = Math.floor((seconds % 31536000) / 86400);
    var numhours = Math.floor(((seconds % 31536000) % 86400) / 3600);
    var numminutes = Math.floor((((seconds % 31536000) % 86400) % 3600) / 60);
    var numseconds = Math.round((((seconds % 31536000) % 86400) % 3600) % 60);

    var elapsedStr = '';
    elapsedStr += numdays == 1 ? numdays + ' day ' : '';
    elapsedStr += numdays > 1 ? numdays + ' days ' : '';
    elapsedStr += numhours == 1 ? numhours + ' hour ' : '';
    elapsedStr += numhours > 1 ? numhours + ' hours ' : '';
    elapsedStr += numminutes == 1 ? numminutes + ' minute ' : '';
    elapsedStr += numminutes > 1 ? numminutes + ' minutes ' : '';
    elapsedStr += elapsedStr.length > 0 ? 'and ' : '';
    elapsedStr += numseconds == 1 ? numseconds + ' second' : '';
    elapsedStr += numseconds == 0 || numseconds > 1 ? numseconds + ' seconds' : '';

    return elapsedStr;

}

function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
    var regexS = "[\\?&]" + name + "=([^&#]*)";
    var regex = new RegExp(regexS);
    var results = regex.exec(window.location.search);
    if (results == null)
        return "";
    else
        return decodeURIComponent(results[1].replace(/\+/g, " "));
}

function parseISO8601Date(s, toLocal) {

    // parenthese matches:
    // year month day    hours minutes seconds
    // dotmilliseconds
    // tzstring plusminus hours minutes
    var re = /(\d{4})-(\d\d)-(\d\d)T(\d\d):(\d\d):(\d\d)(\.\d+)?(Z|([+-])(\d\d):(\d\d))/;

    var d = [];
    d = s.match(re);

    // "2010-12-07T11:00:00.000-09:00" parses to:
    //  ["2010-12-07T11:00:00.000-09:00", "2010", "12", "07", "11",
    //     "00", "00", ".000", "-09:00", "-", "09", "00"]
    // "2010-12-07T11:00:00.000Z" parses to:
    //  ["2010-12-07T11:00:00.000Z",      "2010", "12", "07", "11",
    //     "00", "00", ".000", "Z", undefined, undefined, undefined]

    if (!d) {
        throw "Couldn't parse ISO 8601 date string '" + s + "'";
    }

    // parse strings, leading zeros into proper ints
    var a = [1, 2, 3, 4, 5, 6, 10, 11];
    for (var i in a) {
        d[a[i]] = parseInt(d[a[i]], 10);
    }
    d[7] = parseFloat(d[7]);

    // Date.UTC(year, month[, date[, hrs[, min[, sec[, ms]]]]])
    // note that month is 0-11, not 1-12
    // see https://developer.mozilla.org/en/JavaScript/Reference/Global_Objects/Date/UTC
    var ms = Date.UTC(d[1], d[2] - 1, d[3], d[4], d[5], d[6]);

    // if there are milliseconds, add them
    if (d[7] > 0) {
        ms += Math.round(d[7] * 1000);
    }

    // if there's a timezone, calculate it
    if (d[8] != "Z" && d[10]) {
        var offset = d[10] * 60 * 60 * 1000;
        if (d[11]) {
            offset += d[11] * 60 * 1000;
        }
        if (d[9] == "-") {
            ms -= offset;
        } else {
            ms += offset;
        }
    } else if (!toLocal) {
        ms += new Date().getTimezoneOffset() * 60000;
    }

    return new Date(ms);
};

/**
*
*  Secure Hash Algorithm (SHA1)
*  http://www.webtoolkit.info/
*
**/

function SHA1(msg) {

    function rotate_left(n, s) {
        var t4 = (n << s) | (n >>> (32 - s));
        return t4;
    };

    function lsb_hex(val) {
        var str = "";
        var i;
        var vh;
        var vl;

        for (i = 0; i <= 6; i += 2) {
            vh = (val >>> (i * 4 + 4)) & 0x0f;
            vl = (val >>> (i * 4)) & 0x0f;
            str += vh.toString(16) + vl.toString(16);
        }
        return str;
    };

    function cvt_hex(val) {
        var str = "";
        var i;
        var v;

        for (i = 7; i >= 0; i--) {
            v = (val >>> (i * 4)) & 0x0f;
            str += v.toString(16);
        }
        return str;
    };


    function Utf8Encode(string) {
        string = string.replace(/\r\n/g, "\n");
        var utftext = "";

        for (var n = 0; n < string.length; n++) {

            var c = string.charCodeAt(n);

            if (c < 128) {
                utftext += String.fromCharCode(c);
            }
            else if ((c > 127) && (c < 2048)) {
                utftext += String.fromCharCode((c >> 6) | 192);
                utftext += String.fromCharCode((c & 63) | 128);
            }
            else {
                utftext += String.fromCharCode((c >> 12) | 224);
                utftext += String.fromCharCode(((c >> 6) & 63) | 128);
                utftext += String.fromCharCode((c & 63) | 128);
            }

        }

        return utftext;
    };

    var blockstart;
    var i, j;
    var W = new Array(80);
    var H0 = 0x67452301;
    var H1 = 0xEFCDAB89;
    var H2 = 0x98BADCFE;
    var H3 = 0x10325476;
    var H4 = 0xC3D2E1F0;
    var A, B, C, D, E;
    var temp;

    msg = Utf8Encode(msg);

    var msg_len = msg.length;

    var word_array = new Array();
    for (i = 0; i < msg_len - 3; i += 4) {
        j = msg.charCodeAt(i) << 24 | msg.charCodeAt(i + 1) << 16 |
		msg.charCodeAt(i + 2) << 8 | msg.charCodeAt(i + 3);
        word_array.push(j);
    }

    switch (msg_len % 4) {
        case 0:
            i = 0x080000000;
            break;
        case 1:
            i = msg.charCodeAt(msg_len - 1) << 24 | 0x0800000;
            break;

        case 2:
            i = msg.charCodeAt(msg_len - 2) << 24 | msg.charCodeAt(msg_len - 1) << 16 | 0x08000;
            break;

        case 3:
            i = msg.charCodeAt(msg_len - 3) << 24 | msg.charCodeAt(msg_len - 2) << 16 | msg.charCodeAt(msg_len - 1) << 8 | 0x80;
            break;
    }

    word_array.push(i);

    while ((word_array.length % 16) != 14) word_array.push(0);

    word_array.push(msg_len >>> 29);
    word_array.push((msg_len << 3) & 0x0ffffffff);


    for (blockstart = 0; blockstart < word_array.length; blockstart += 16) {

        for (i = 0; i < 16; i++) W[i] = word_array[blockstart + i];
        for (i = 16; i <= 79; i++) W[i] = rotate_left(W[i - 3] ^ W[i - 8] ^ W[i - 14] ^ W[i - 16], 1);

        A = H0;
        B = H1;
        C = H2;
        D = H3;
        E = H4;

        for (i = 0; i <= 19; i++) {
            temp = (rotate_left(A, 5) + ((B & C) | (~B & D)) + E + W[i] + 0x5A827999) & 0x0ffffffff;
            E = D;
            D = C;
            C = rotate_left(B, 30);
            B = A;
            A = temp;
        }

        for (i = 20; i <= 39; i++) {
            temp = (rotate_left(A, 5) + (B ^ C ^ D) + E + W[i] + 0x6ED9EBA1) & 0x0ffffffff;
            E = D;
            D = C;
            C = rotate_left(B, 30);
            B = A;
            A = temp;
        }

        for (i = 40; i <= 59; i++) {
            temp = (rotate_left(A, 5) + ((B & C) | (B & D) | (C & D)) + E + W[i] + 0x8F1BBCDC) & 0x0ffffffff;
            E = D;
            D = C;
            C = rotate_left(B, 30);
            B = A;
            A = temp;
        }

        for (i = 60; i <= 79; i++) {
            temp = (rotate_left(A, 5) + (B ^ C ^ D) + E + W[i] + 0xCA62C1D6) & 0x0ffffffff;
            E = D;
            D = C;
            C = rotate_left(B, 30);
            B = A;
            A = temp;
        }

        H0 = (H0 + A) & 0x0ffffffff;
        H1 = (H1 + B) & 0x0ffffffff;
        H2 = (H2 + C) & 0x0ffffffff;
        H3 = (H3 + D) & 0x0ffffffff;
        H4 = (H4 + E) & 0x0ffffffff;

    }

    var temp = cvt_hex(H0) + cvt_hex(H1) + cvt_hex(H2) + cvt_hex(H3) + cvt_hex(H4);

    return temp.toLowerCase();

}

// jqm.page.params.js - version 0.1
// Copyright (c) 2011, Kin Blas
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the <organization> nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

(function ($, window, undefined) {

    // Given a query string, convert all the name/value pairs
    // into a property/value object. If a name appears more than
    // once in a query string, the value is automatically turned
    // into an array.
    function queryStringToObject(qstr) {
        var result = {}, nvPairs = ((qstr || "").replace(/^\?/, "").split(/&/)), i, pair, n, v;

        for (i = 0; i < nvPairs.length; i++) {
            var pstr = nvPairs[i];
            if (pstr) {
                pair = pstr.split(/=/);
                n = pair[0];
                v = pair[1];
                if (result[n] === undefined) {
                    result[n] = v;
                } else {
                    if (typeof result[n] !== "object") {
                        result[n] = [result[n]];
                    }
                    result[n].push(v);
                }
            }
        }

        return result;
    }

    // The idea here is to listen for any pagebeforechange notifications from
    // jQuery Mobile, and then muck with the toPage and options so that query
    // params can be passed to embedded/internal pages. So for example, if a
    // changePage() request for a URL like:
    //
    //    http://mycompany.com/myapp/#page-1?foo=1&bar=2
    //
    // is made, the page that will actually get shown is:
    //
    //    http://mycompany.com/myapp/#page-1
    //
    // The browser's location will still be updated to show the original URL.
    // The query params for the embedded page are also added as a property/value
    // object on the options object. You can access it from your page notifications
    // via data.options.pageData.
    $(document).bind("pagebeforechange", function (e, data) {

        // We only want to handle the case where we are being asked
        // to go to a page by URL, and only if that URL is referring
        // to an internal page by id.

        if (typeof data.toPage === "string") {
            var u = $.mobile.path.parseUrl(data.toPage);
            if ($.mobile.path.isEmbeddedPage(u)) {

                // The request is for an internal page, if the hash
                // contains query (search) params, strip them off the
                // toPage URL and then set options.dataUrl appropriately
                // so the location.hash shows the originally requested URL
                // that hash the query params in the hash.

                var u2 = $.mobile.path.parseUrl(u.hash.replace(/^#/, ""));
                if (u2.search) {
                    if (!data.options.dataUrl) {
                        data.options.dataUrl = data.toPage;
                    }
                    data.options.pageData = queryStringToObject(u2.search);
                    data.toPage = u.hrefNoHash + "#" + u2.pathname;
                }
            }
        }
    });

})(jQuery, window);
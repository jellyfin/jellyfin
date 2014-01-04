function IsStorageEnabled() {
    
    if (!window.localStorage) {
        return false;
    }
    try {
        window.localStorage.setItem("__test", "data");
    } catch (err) {
        if ((err.name).toUpperCase() == 'QUOTA_EXCEEDED_ERR') {
            return false;
        }
    }
    return true;
}

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

$.fn.buttonEnabled = function(enabled) {

    return enabled ? this.attr('disabled', '').removeAttr('disabled') : this.attr('disabled', 'disabled');
};

if (!Array.prototype.filter) {
    Array.prototype.filter = function (fun /*, thisp*/) {
        "use strict";

        if (this == null)
            throw new TypeError();

        var t = Object(this);
        var len = t.length >>> 0;
        if (typeof fun != "function")
            throw new TypeError();

        var res = [];
        var thisp = arguments[1];
        for (var i = 0; i < len; i++) {
            if (i in t) {
                var val = t[i]; // in case fun mutates this
                if (fun.call(thisp, val, i, t))
                    res.push(val);
            }
        }

        return res;
    };
}

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

            var level = Notification.permissionLevel ? Notification.permissionLevel() : Notification.permission;

            if (level === "granted") {
                var notif = new Notification(data.title, data);

                if (notif.show) {
                    notif.show();
                }

                if (data.timeout && notif.cancel) {
                    setTimeout(function () {
                        notif.cancel();
                    }, data.timeout);
                }

                return notif;
            } else if (level === "default") {
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

            var level = Notification.permissionLevel ? Notification.permissionLevel() : Notification.permission;

            if (level === "default") {
                Notification.requestPermission(function () {
                });
            }
        }
    },
    
    supported: function() {
        return window.Notification || window.webkitNotifications;
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
    var date = parseISO8601Date(date_str, { toLocal: true });

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

function getParameterByName(name, url) {
    name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
    var regexS = "[\\?&]" + name + "=([^&#]*)";
    var regex = new RegExp(regexS, "i");
    var results = regex.exec(url || window.location.search);
    if (results == null)
        return "";
    else
        return decodeURIComponent(results[1].replace(/\+/g, " "));
}

function parseISO8601Date(s, options) {

    options = options || {};

    // parenthese matches:
    // year month day    hours minutes seconds
    // dotmilliseconds
    // tzstring plusminus hours minutes
    var re = /(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(\.\d+)?(Z|([+-])(\d{2}):(\d{2}))?/;

    var d = s.match(re);

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
    } else if (!options.toLocal) {
        ms += new Date().getTimezoneOffset() * 60000;
    }

    return new Date(ms);
};

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


//convert Ticks to human hr:min:sec format
function ticks_to_human(str) {

    var in_seconds = (str / 10000000);
    var hours = Math.floor(in_seconds / 3600);
    var minutes = Math.floor((in_seconds - (hours * 3600)) / 60);
    var seconds = '0' + Math.round(in_seconds - (hours * 3600) - (minutes * 60));

    var time = '';

    if (hours > 0) time += hours + ":";
    if (minutes < 10 && hours == 0) time += minutes;
    else time += ('0' + minutes).substr(-2);
    time += ":" + seconds.substr(-2);

    return time;
};
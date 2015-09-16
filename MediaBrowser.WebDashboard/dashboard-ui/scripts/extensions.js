// Regular Expressions for parsing tags and attributes
var SURROGATE_PAIR_REGEXP = /[\uD800-\uDBFF][\uDC00-\uDFFF]/g,
  // Match everything outside of normal chars and " (quote character)
  NON_ALPHANUMERIC_REGEXP = /([^\#-~| |!])/g;

var hiddenPre = document.createElement("pre");
/**
 * decodes all entities into regular string
 * @param value
 * @returns {string} A string with decoded entities.
 */
function htmlDecode(value) {
    if (!value) { return ''; }

    hiddenPre.innerHTML = value.replace(/</g, "&lt;");
    // innerText depends on styling as it doesn't display hidden elements.
    // Therefore, it's better to use textContent not to cause unnecessary reflows.
    return hiddenPre.textContent;
}

/**
 * Escapes all potentially dangerous characters, so that the
 * resulting string can be safely inserted into attribute or
 * element text.
 * @param value
 * @returns {string} escaped text
 */
function htmlEncode(value) {
    return value.
      replace(/&/g, '&amp;').
      replace(SURROGATE_PAIR_REGEXP, function (value) {
          var hi = value.charCodeAt(0);
          var low = value.charCodeAt(1);
          return '&#' + (((hi - 0xD800) * 0x400) + (low - 0xDC00) + 0x10000) + ';';
      }).
      replace(NON_ALPHANUMERIC_REGEXP, function (value) {
          return '&#' + value.charCodeAt(0) + ';';
      }).
      replace(/</g, '&lt;').
      replace(/>/g, '&gt;');
}

// Array Remove - By John Resig (MIT Licensed)
Array.prototype.remove = function (from, to) {
    var rest = this.slice((to || from) + 1 || this.length);
    this.length = from < 0 ? this.length + from : from;
    return this.push.apply(this, rest);
};

$.fn.checked = function (value) {
    if (value === true || value === false) {
        // Set the value of the checkbox
        return $(this).each(function () {
            this.checked = value;
        });
    } else {
        // Return check state
        return this.length && this[0].checked;
    }
};

$.fn.buttonEnabled = function (enabled) {

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

        // Seeing crashes in android
        if (window.cordova && window.cordova.plugins && window.cordova.plugins.notification) {

            if (!WebNotifications.lastId) {
                // Cordova plugin will crash on android with long. need an int
                WebNotifications.lastId = new Date().getDate() + new Date().getMilliseconds();
            }

            WebNotifications.lastId++;

            window.cordova.plugins.notification.local.schedule({

                id: WebNotifications.lastId,

                title: data.title,
                text: data.body,
                //firstAt: monday_9_am,
                //every: "week",
                //sound: "file://sounds/reminder.mp3",
                //data: { meetingId: "123#fg8" },
                icon: data.icon
            });
        }
        else if (window.Notification) {

            var level = Notification.permissionLevel ? Notification.permissionLevel() : Notification.permission;

            if (level === "granted") {
                var notif = new Notification(data.title, data);

                if (notif.show) {
                    notif.show();
                }

                if (data.timeout) {
                    setTimeout(function () {

                        if (notif.close) {
                            notif.close();
                        }
                        else if (notif.cancel) {
                            notif.cancel();
                        }
                    }, data.timeout);
                }

                return notif;
            } else if (level === "default") {
                Notification.requestPermission(function () {
                    return WebNotifications.show(data);
                });
            }
        }

        else if (window.webkitNotifications) {
            if (!webkitNotifications.checkPermission()) {
                var notif = webkitNotifications.createNotification(data.icon, data.title, data.body);
                notif.show();

                if (data.timeout) {
                    setTimeout(function () {

                        if (notif.close) {
                            notif.close();
                        }
                        else if (notif.cancel) {
                            notif.cancel();
                        }
                    }, data.timeout);
                }

                return notif;
            } else {
                webkitNotifications.requestPermission(function () {
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
}

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

function getWindowUrl(win) {
    return (win || window).location.href;
}

function getWindowLocationSearch(win) {

    var search = (win || window).location.search;

    if (!search) {

        var index = window.location.href.indexOf('?');
        if (index != -1) {
            search = window.location.href.substring(index);
        }
    }

    return search || '';
}

function getParameterByName(name, url) {
    name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
    var regexS = "[\\?&]" + name + "=([^&#]*)";
    var regex = new RegExp(regexS, "i");

    var results = regex.exec(url || getWindowLocationSearch());
    if (results == null)
        return "";
    else
        return decodeURIComponent(results[1].replace(/\+/g, " "));
}

function replaceQueryString(url, param, value) {
    var re = new RegExp("([?|&])" + param + "=.*?(&|$)", "i");
    if (url.match(re))
        return url.replace(re, '$1' + param + "=" + value + '$2');
    else if (value) {

        if (url.indexOf('?') == -1) {
            return url + '?' + param + "=" + value;
        }

        return url + '&' + param + "=" + value;
    }

    return url;
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
}


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

(function () {
    var supportTouch = $.support.touch,
            scrollEvent = "touchmove scroll",
            touchStartEvent = supportTouch ? "touchstart" : "mousedown",
            touchStopEvent = supportTouch ? "touchend" : "mouseup",
            touchMoveEvent = supportTouch ? "touchmove" : "mousemove";
    $.event.special.swipeupdown = {
        setup: function () {
            var thisObject = this;
            var $this = $(thisObject);
            $this.bind(touchStartEvent, function (event) {
                var data = event.originalEvent.touches ?
                        event.originalEvent.touches[0] :
                        event,
                        start = {
                            time: (new Date).getTime(),
                            coords: [data.pageX, data.pageY],
                            origin: $(event.target)
                        },
                        stop;

                function moveHandler(event) {
                    if (!start) {
                        return;
                    }
                    var data = event.originalEvent.touches ?
                            event.originalEvent.touches[0] :
                            event;
                    stop = {
                        time: (new Date).getTime(),
                        coords: [data.pageX, data.pageY]
                    };

                    // prevent scrolling
                    if (Math.abs(start.coords[1] - stop.coords[1]) > 10) {
                        event.preventDefault();
                    }
                }
                $this
                        .bind(touchMoveEvent, moveHandler)
                        .one(touchStopEvent, function (event) {
                            $this.unbind(touchMoveEvent, moveHandler);
                            if (start && stop) {
                                if (stop.time - start.time < 1000 &&
                                        Math.abs(start.coords[1] - stop.coords[1]) > 100 &&
                                        Math.abs(start.coords[0] - stop.coords[0]) < 75) {
                                    start.origin
                                            .trigger("swipeupdown")
                                            .trigger(start.coords[1] > stop.coords[1] ? "swipeup" : "swipedown");
                                }
                            }
                            start = stop = undefined;
                        });
            });
        }
    };
    $.each({
        swipedown: "swipeupdown",
        swipeup: "swipeupdown"
    }, function (event, sourceEvent) {
        $.event.special[event] = {
            setup: function () {
                $(this).bind(sourceEvent, $.noop);
            }
        };
    });

})();

// This only exists because the polymer elements get distorted when using regular jquery show/hide
$.fn.visible = function (visible) {

    if (visible) {
        return this.removeClass('hide');
    }
    return this.addClass('hide');
};
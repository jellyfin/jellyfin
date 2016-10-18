define(['globalize'], function (globalize) {
    'use strict';

    function parseISO8601Date(s, toLocal) {

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
        if (d[8] !== "Z" && d[10]) {
            var offset = d[10] * 60 * 60 * 1000;
            if (d[11]) {
                offset += d[11] * 60 * 1000;
            }
            if (d[9] === "-") {
                ms -= offset;
            } else {
                ms += offset;
            }
        } else if (toLocal === false) {
            ms += new Date().getTimezoneOffset() * 60000;
        }

        return new Date(ms);
    }

    function getDisplayRunningTime(ticks) {
        var ticksPerHour = 36000000000;
        var ticksPerMinute = 600000000;
        var ticksPerSecond = 10000000;

        var parts = [];

        var hours = ticks / ticksPerHour;
        hours = Math.floor(hours);

        if (hours) {
            parts.push(hours);
        }

        ticks -= (hours * ticksPerHour);

        var minutes = ticks / ticksPerMinute;
        minutes = Math.floor(minutes);

        ticks -= (minutes * ticksPerMinute);

        if (minutes < 10 && hours) {
            minutes = '0' + minutes;
        }
        parts.push(minutes);

        var seconds = ticks / ticksPerSecond;
        seconds = Math.floor(seconds);

        if (seconds < 10) {
            seconds = '0' + seconds;
        }
        parts.push(seconds);

        return parts.join(':');
    }

    var toLocaleTimeStringSupportsLocales = function toLocaleTimeStringSupportsLocales() {
        try {
            new Date().toLocaleTimeString('i');
        } catch (e) {
            return e.name === 'RangeError';
        }
        return false;
    }();

    function getCurrentLocale() {
        var locale = globalize.getCurrentLocale();

        return locale;
    }

    function toLocaleString(date, options) {
        var currentLocale = getCurrentLocale();

        return currentLocale && toLocaleTimeStringSupportsLocales ?
            date.toLocaleString(currentLocale, options || {}) :
            date.toLocaleString();
    }

    function toLocaleDateString(date, options) {

        var currentLocale = getCurrentLocale();

        return currentLocale && toLocaleTimeStringSupportsLocales ?
            date.toLocaleDateString(currentLocale, options || {}) :
            date.toLocaleDateString();
    }

    function toLocaleTimeString(date, options) {

        var currentLocale = getCurrentLocale();

        return currentLocale && toLocaleTimeStringSupportsLocales ?
            date.toLocaleTimeString(currentLocale, options || {}).toLowerCase() :
            date.toLocaleTimeString().toLowerCase();
    }

    function getDisplayTime(date) {

        if ((typeof date).toString().toLowerCase() === 'string') {
            try {

                date = parseISO8601Date(date, true);

            } catch (err) {
                return date;
            }
        }

        if (toLocaleTimeStringSupportsLocales) {
            return toLocaleTimeString(date, {

                hour: 'numeric',
                minute: '2-digit'

            });
        }

        var time = toLocaleTimeString(date);

        var timeLower = time.toLowerCase();

        if (timeLower.indexOf('am') !== -1 || timeLower.indexOf('pm') !== -1) {

            time = timeLower;
            var hour = date.getHours() % 12;
            var suffix = date.getHours() > 11 ? 'pm' : 'am';
            if (!hour) {
                hour = 12;
            }
            var minutes = date.getMinutes();

            if (minutes < 10) {
                minutes = '0' + minutes;
            }

            minutes = ':' + minutes;
            time = hour + minutes + suffix;
        } else {

            var timeParts = time.split(':');

            // Trim off seconds
            if (timeParts.length > 2) {
                timeParts.length -= 1;
                time = timeParts.join(':');
            }
        }

        return time;
    }

    function isRelativeDay(date, offsetInDays) {
        var yesterday = new Date();
        var day = yesterday.getDate() + offsetInDays;

        yesterday.setDate(day); // automatically adjusts month/year appropriately

        return date.getFullYear() === yesterday.getFullYear() && date.getMonth() === yesterday.getMonth() && date.getDate() === day;
    }

    return {
        parseISO8601Date: parseISO8601Date,
        getDisplayRunningTime: getDisplayRunningTime,
        toLocaleDateString: toLocaleDateString,
        toLocaleString: toLocaleString,
        getDisplayTime: getDisplayTime,
        isRelativeDay: isRelativeDay
    };
});
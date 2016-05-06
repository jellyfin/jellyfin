define(['datetime'], function (datetime) {

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
        var date = datetime.parseISO8601Date(date_str, true);

        var seconds = ((dt - date) / 1000);
        var token = ' ago';
        var i = 0;
        var format;

        if (seconds < 0) {
            seconds = Math.abs(seconds);
            //token = '';
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

    window.humane_date = humane_date;

    return humane_date;
});
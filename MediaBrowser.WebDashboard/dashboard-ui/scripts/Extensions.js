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


/*
 JS for the quality selector in video.js player
 */

/*
 Define the base class for the quality selector button.
 Most of this code is copied from the _V_.TextTrackButton
 class.

 https://github.com/zencoder/video-js/blob/master/src/tracks.js#L560)
 */
_V_.ResolutionSelector = _V_.Button.extend({

    kind: "quality",
    className: "vjs-quality-button",

    init: function(player, options) {

        this._super(player, options);

        // Save the starting resolution as a property of the player object
        player.options.currentResolution = this.buttonText;

        this.menu = this.createMenu();

        if (this.items.length === 0) {
            this.hide();
        }
    },

    createMenu: function() {

        var menu = new _V_.Menu(this.player);

        // Add a title list item to the top
        menu.el.appendChild(_V_.createElement("li", {
            className: "vjs-menu-title",
            innerHTML: _V_.uc(this.kind)
        }));

        this.items = this.createItems();

        // Add menu items to the menu
        this.each(this.items, function(item){
            menu.addItem(item);
        });

        // Add list to element
        this.addComponent(menu);

        return menu;
    },

    // Override the default _V_.Button createElement so the button text isn't hidden
    createElement: function(type, attrs) {

        // Add standard Aria and Tabindex info
        attrs = _V_.merge({
            className: this.buildCSSClass(),
            innerHTML: '<div><span class="vjs-quality-text">' + this.buttonText + '</span></div>',
            role: "button",
            tabIndex: 0
        }, attrs);

        return this._super(type, attrs);
    },

    // Create a menu item for each text track
    createItems: function() {

        var items = [];

        this.each( this.availableRes, function( res ) {

            items.push( new _V_.ResolutionMenuItem( this.player, {

                label: res[0].res,
                src: res
            }));
        });

        return items;
    },

    buildCSSClass: function() {

        return this.className + " vjs-menu-button " + this._super();
    },

    // Focus - Add keyboard functionality to element
    onFocus: function() {

        // Show the menu, and keep showing when the menu items are in focus
        this.menu.lockShowing();
        this.menu.el.style.display = "block";

        // When tabbing through, the menu should hide when focus goes from the last menu item to the next tabbed element.
        _V_.one(this.menu.el.childNodes[this.menu.el.childNodes.length - 1], "blur", this.proxy(function() {

            this.menu.unlockShowing();
        }));
    },

    // Can't turn off list display that we turned on with focus, because list would go away.
    onBlur: function(){},

    onClick: function() {

        /*
         When you click the button it adds focus, which will show the menu indefinitely.
         So we'll remove focus when the mouse leaves the button.
         Focus is needed for tab navigation.
         */
        this.one( 'mouseout', this.proxy(function() {

            this.menu.unlockShowing();
            this.el.blur();
        }));
    }
});

/*
 Define the base class for the quality menu items
 */
_V_.ResolutionMenuItem = _V_.MenuItem.extend({

    init: function(player, options){

        // Modify options for parent MenuItem class's init.
        options.selected = ( options.label === player.options.currentResolution );
        this._super( player, options );

        this.player.addEvent( 'changeRes', _V_.proxy( this, this.update ) );
    },

    onClick: function() {

        // Check that we are changing to a new quality (not the one we are already on)
        if ( this.options.label === this.player.options.currentResolution )
            return;

        var resolutions = new Array();
        resolutions['high'] = 500000;
        resolutions['medium'] = 250000;
        resolutions['low'] = 50000;

        var current_time = this.player.currentTime();

        // Set the button text to the newly chosen quality
        jQuery( this.player.controlBar.el ).find( '.vjs-quality-text' ).html( this.options.label );

        // Change the source and make sure we don't start the video over
        var currentSrc = this.player.tag.src;
        var newSrc = currentSrc.replace("videoBitrate="+resolutions[this.player.options.currentResolution],"videoBitrate="+resolutions[this.options.src[0].res]);

        if (this.player.duration() == "Infinity")  {
            if (currentSrc.indexOf("StartTimeTicks") >= 0) {
                var startTimeTicks = newSrc.match(new RegExp("StartTimeTicks=[0-9]+","g"));
                var start_time = startTimeTicks[0].replace("StartTimeTicks=","");

                newSrc = newSrc.replace(new RegExp("StartTimeTicks=[0-9]+","g"),"StartTimeTicks="+(parseInt(start_time)+(10000000*current_time)));
            }else {
                newSrc += "&StartTimeTicks="+10000000*current_time;
            }

            this.player.src( newSrc ).one( 'loadedmetadata', function() {
                this.play();
            });
        }else {
            this.player.src( newSrc ).one( 'loadedmetadata', function() {
                this.currentTime( current_time );
                this.play();
            });
        }

        // Save the newly selected resolution in our player options property
        this.player.options.currentResolution = this.options.label;

        // Update the classes to reflect the currently selected resolution
        this.player.triggerEvent( 'changeRes' );
    },

    update: function() {

        if ( this.options.label === this.player.options.currentResolution ) {
            this.selected( true );
        } else {
            this.selected( false );
        }
    }
});


/*
 JS for the chapter selector in video.js player
 */

/*
 Define the base class for the chapter selector button.
  */
_V_.ChapterSelector = _V_.Button.extend({

    kind: "chapter",
    className: "vjs-chapter-button",

    init: function(player, options) {

        this._super(player, options);

        this.menu = this.createMenu();

        if (this.items.length === 0) {
            this.hide();
        }
    },

    createMenu: function() {

        var menu = new _V_.Menu(this.player);

        // Add a title list item to the top
        menu.el.appendChild(_V_.createElement("li", {
            className: "vjs-menu-title",
            innerHTML: _V_.uc(this.kind)
        }));

        this.items = this.createItems();

        // Add menu items to the menu
        this.each(this.items, function(item){
            menu.addItem(item);
        });

        // Add list to element
        this.addComponent(menu);

        return menu;
    },

    // Override the default _V_.Button createElement so the button text isn't hidden
    createElement: function(type, attrs) {

        // Add standard Aria and Tabindex info
        attrs = _V_.merge({
            className: this.buildCSSClass(),
            innerHTML: '<div><span class="vjs-chapter-text">' + this.buttonText + '</span></div>',
            role: "button",
            tabIndex: 0
        }, attrs);

        return this._super(type, attrs);
    },

    // Create a menu item for each chapter
    createItems: function() {

        var items = [];

        this.each( this.Chapters, function( chapter ) {

            items.push( new _V_.ChapterMenuItem( this.player, {
                label: chapter[0].Name,
                src: chapter
            }));
        });

        return items;
    },

    buildCSSClass: function() {

        return this.className + " vjs-menu-button " + this._super();
    },

    // Focus - Add keyboard functionality to element
    onFocus: function() {

        // Show the menu, and keep showing when the menu items are in focus
        this.menu.lockShowing();
        this.menu.el.style.display = "block";

        // When tabbing through, the menu should hide when focus goes from the last menu item to the next tabbed element.
        _V_.one(this.menu.el.childNodes[this.menu.el.childNodes.length - 1], "blur", this.proxy(function() {

            this.menu.unlockShowing();
        }));
    },

    // Can't turn off list display that we turned on with focus, because list would go away.
    onBlur: function(){},

    onClick: function() {

        /*
         When you click the button it adds focus, which will show the menu indefinitely.
         So we'll remove focus when the mouse leaves the button.
         Focus is needed for tab navigation.
         */
        this.one( 'mouseout', this.proxy(function() {

            this.menu.unlockShowing();
            this.el.blur();
        }));
    }
});

/*
 Define the base class for the chapter menu items
 */
_V_.ChapterMenuItem = _V_.MenuItem.extend({

    init: function(player, options){

        // Modify options for parent MenuItem class's init.
        //options.selected = ( options.label === player.options.currentResolution );
        this._super( player, options );

        this.player.addEvent( 'changeChapter', _V_.proxy( this, this.update ) );
    },

    onClick: function() {

        // Set the button text to the newly chosen chapter
        //jQuery( this.player.controlBar.el ).find( '.vjs-chapter-text' ).html( this.options.label );

        if (this.player.duration() == "Infinity") {
            var currentSrc = this.player.tag.src;

            if (currentSrc.indexOf("StartTimeTicks") >= 0) {
                var newSrc = currentSrc.replace(new RegExp("StartTimeTicks=[0-9]+","g"),"StartTimeTicks="+this.options.src[0].StartPositionTicks);
            }else {
                var newSrc = currentSrc += "&StartTimeTicks="+this.options.src[0].StartPositionTicks;
            }

            this.player.src( newSrc ).one( 'loadedmetadata', function() {
                this.play();
            });
        }else {
            //figure out the time from ticks
            var current_time = parseFloat(this.options.src[0].StartPositionTicks)/10000000;

            this.player.currentTime( current_time );
        }
   },

    update: function() {
    }
});

//convert Ticks to human hr:min:sec format
function ticks_to_human(str) {

    var in_seconds = (str / 10000000);
    var hours = Math.floor(in_seconds/3600);
    var minutes = '0'+Math.floor((in_seconds-(hours*3600))/60);
    var seconds = '0'+Math.round(in_seconds-(hours*3600)-(minutes*60));

    var time = '';

    if (hours > 0) time += hours+":";
    time += minutes.substr(-2) + ":" +seconds.substr(-2);

    return time;
};
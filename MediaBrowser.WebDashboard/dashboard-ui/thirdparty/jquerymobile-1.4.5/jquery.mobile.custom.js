/*
* jQuery Mobile v1.4.5
* http://jquerymobile.com
*
* Copyright 2010, 2014 jQuery Foundation, Inc. and other contributors
* Released under the MIT license.
* http://jquery.org/license
*
*/

(function (root, doc, factory) {
    // Browser globals
    factory(root.jQuery, root, doc);
}(this, document, function (jQuery, window, document, undefined) {/*!
 * jQuery hashchange event - v1.3 - 7/21/2010
 * http://benalman.com/projects/jquery-hashchange-plugin/
 * 
 * Copyright (c) 2010 "Cowboy" Ben Alman
 * Dual licensed under the MIT and GPL licenses.
 * http://benalman.com/about/license/
 */

    // Script: jQuery hashchange event
    //
    // *Version: 1.3, Last updated: 7/21/2010*
    // 
    // Project Home - http://benalman.com/projects/jquery-hashchange-plugin/
    // GitHub       - http://github.com/cowboy/jquery-hashchange/
    // Source       - http://github.com/cowboy/jquery-hashchange/raw/master/jquery.ba-hashchange.js
    // (Minified)   - http://github.com/cowboy/jquery-hashchange/raw/master/jquery.ba-hashchange.min.js (0.8kb gzipped)
    // 
    // About: License
    // 
    // Copyright (c) 2010 "Cowboy" Ben Alman,
    // Dual licensed under the MIT and GPL licenses.
    // http://benalman.com/about/license/
    // 
    // About: Examples
    // 
    // These working examples, complete with fully commented code, illustrate a few
    // ways in which this plugin can be used.
    // 
    // hashchange event - http://benalman.com/code/projects/jquery-hashchange/examples/hashchange/
    // document.domain - http://benalman.com/code/projects/jquery-hashchange/examples/document_domain/
    // 
    // About: Support and Testing
    // 
    // Information about what version or versions of jQuery this plugin has been
    // tested with, what browsers it has been tested in, and where the unit tests
    // reside (so you can test it yourself).
    // 
    // jQuery Versions - 1.2.6, 1.3.2, 1.4.1, 1.4.2
    // Browsers Tested - Internet Explorer 6-8, Firefox 2-4, Chrome 5-6, Safari 3.2-5,
    //                   Opera 9.6-10.60, iPhone 3.1, Android 1.6-2.2, BlackBerry 4.6-5.
    // Unit Tests      - http://benalman.com/code/projects/jquery-hashchange/unit/
    // 
    // About: Known issues
    // 
    // While this jQuery hashchange event implementation is quite stable and
    // robust, there are a few unfortunate browser bugs surrounding expected
    // hashchange event-based behaviors, independent of any JavaScript
    // window.onhashchange abstraction. See the following examples for more
    // information:
    // 
    // Chrome: Back Button - http://benalman.com/code/projects/jquery-hashchange/examples/bug-chrome-back-button/
    // Firefox: Remote XMLHttpRequest - http://benalman.com/code/projects/jquery-hashchange/examples/bug-firefox-remote-xhr/
    // WebKit: Back Button in an Iframe - http://benalman.com/code/projects/jquery-hashchange/examples/bug-webkit-hash-iframe/
    // Safari: Back Button from a different domain - http://benalman.com/code/projects/jquery-hashchange/examples/bug-safari-back-from-diff-domain/
    // 
    // Also note that should a browser natively support the window.onhashchange 
    // event, but not report that it does, the fallback polling loop will be used.
    // 
    // About: Release History
    // 
    // 1.3   - (7/21/2010) Reorganized IE6/7 Iframe code to make it more
    //         "removable" for mobile-only development. Added IE6/7 document.title
    //         support. Attempted to make Iframe as hidden as possible by using
    //         techniques from http://www.paciellogroup.com/blog/?p=604. Added 
    //         support for the "shortcut" format $(window).hashchange( fn ) and
    //         $(window).hashchange() like jQuery provides for built-in events.
    //         Renamed jQuery.hashchangeDelay to <jQuery.fn.hashchange.delay> and
    //         lowered its default value to 50. Added <jQuery.fn.hashchange.domain>
    //         and <jQuery.fn.hashchange.src> properties plus document-domain.html
    //         file to address access denied issues when setting document.domain in
    //         IE6/7.
    // 1.2   - (2/11/2010) Fixed a bug where coming back to a page using this plugin
    //         from a page on another domain would cause an error in Safari 4. Also,
    //         IE6/7 Iframe is now inserted after the body (this actually works),
    //         which prevents the page from scrolling when the event is first bound.
    //         Event can also now be bound before DOM ready, but it won't be usable
    //         before then in IE6/7.
    // 1.1   - (1/21/2010) Incorporated document.documentMode test to fix IE8 bug
    //         where browser version is incorrectly reported as 8.0, despite
    //         inclusion of the X-UA-Compatible IE=EmulateIE7 meta tag.
    // 1.0   - (1/9/2010) Initial Release. Broke out the jQuery BBQ event.special
    //         window.onhashchange functionality into a separate plugin for users
    //         who want just the basic event & back button support, without all the
    //         extra awesomeness that BBQ provides. This plugin will be included as
    //         part of jQuery BBQ, but also be available separately.

    (function ($, window, undefined) {
        '$:nomunge'; // Used by YUI compressor.

        // Reused string.
        var str_hashchange = 'hashchange',

          // Method / object references.
          doc = document,
          fake_onhashchange,
          special = $.event.special,

          // Does the browser support window.onhashchange? Note that IE8 running in
          // IE7 compatibility mode reports true for 'onhashchange' in window, even
          // though the event isn't supported, so also test document.documentMode.
          doc_mode = doc.documentMode,
          supports_onhashchange = 'on' + str_hashchange in window && (doc_mode === undefined || doc_mode > 7);

        // Get location.hash (or what you'd expect location.hash to be) sans any
        // leading #. Thanks for making this necessary, Firefox!
        function get_fragment(url) {
            url = url || location.href;
            return '#' + url.replace(/^[^#]*#?(.*)$/, '$1');
        };

        // Method: jQuery.fn.hashchange
        // 
        // Bind a handler to the window.onhashchange event or trigger all bound
        // window.onhashchange event handlers. This behavior is consistent with
        // jQuery's built-in event handlers.
        // 
        // Usage:
        // 
        // > jQuery(window).hashchange( [ handler ] );
        // 
        // Arguments:
        // 
        //  handler - (Function) Optional handler to be bound to the hashchange
        //    event. This is a "shortcut" for the more verbose form:
        //    jQuery(window).bind( 'hashchange', handler ). If handler is omitted,
        //    all bound window.onhashchange event handlers will be triggered. This
        //    is a shortcut for the more verbose
        //    jQuery(window).trigger( 'hashchange' ). These forms are described in
        //    the <hashchange event> section.
        // 
        // Returns:
        // 
        //  (jQuery) The initial jQuery collection of elements.

        // Allow the "shortcut" format $(elem).hashchange( fn ) for binding and
        // $(elem).hashchange() for triggering, like jQuery does for built-in events.
        $.fn[str_hashchange] = function (fn) {
            return fn ? this.bind(str_hashchange, fn) : this.trigger(str_hashchange);
        };

        // Property: jQuery.fn.hashchange.delay
        // 
        // The numeric interval (in milliseconds) at which the <hashchange event>
        // polling loop executes. Defaults to 50.

        // Property: jQuery.fn.hashchange.domain
        // 
        // If you're setting document.domain in your JavaScript, and you want hash
        // history to work in IE6/7, not only must this property be set, but you must
        // also set document.domain BEFORE jQuery is loaded into the page. This
        // property is only applicable if you are supporting IE6/7 (or IE8 operating
        // in "IE7 compatibility" mode).
        // 
        // In addition, the <jQuery.fn.hashchange.src> property must be set to the
        // path of the included "document-domain.html" file, which can be renamed or
        // modified if necessary (note that the document.domain specified must be the
        // same in both your main JavaScript as well as in this file).
        // 
        // Usage:
        // 
        // jQuery.fn.hashchange.domain = document.domain;

        // Property: jQuery.fn.hashchange.src
        // 
        // If, for some reason, you need to specify an Iframe src file (for example,
        // when setting document.domain as in <jQuery.fn.hashchange.domain>), you can
        // do so using this property. Note that when using this property, history
        // won't be recorded in IE6/7 until the Iframe src file loads. This property
        // is only applicable if you are supporting IE6/7 (or IE8 operating in "IE7
        // compatibility" mode).
        // 
        // Usage:
        // 
        // jQuery.fn.hashchange.src = 'path/to/file.html';

        $.fn[str_hashchange].delay = 50;
        /*
        $.fn[ str_hashchange ].domain = null;
        $.fn[ str_hashchange ].src = null;
        */

        // Event: hashchange event
        // 
        // Fired when location.hash changes. In browsers that support it, the native
        // HTML5 window.onhashchange event is used, otherwise a polling loop is
        // initialized, running every <jQuery.fn.hashchange.delay> milliseconds to
        // see if the hash has changed. In IE6/7 (and IE8 operating in "IE7
        // compatibility" mode), a hidden Iframe is created to allow the back button
        // and hash-based history to work.
        // 
        // Usage as described in <jQuery.fn.hashchange>:
        // 
        // > // Bind an event handler.
        // > jQuery(window).hashchange( function(e) {
        // >   var hash = location.hash;
        // >   ...
        // > });
        // > 
        // > // Manually trigger the event handler.
        // > jQuery(window).hashchange();
        // 
        // A more verbose usage that allows for event namespacing:
        // 
        // > // Bind an event handler.
        // > jQuery(window).bind( 'hashchange', function(e) {
        // >   var hash = location.hash;
        // >   ...
        // > });
        // > 
        // > // Manually trigger the event handler.
        // > jQuery(window).trigger( 'hashchange' );
        // 
        // Additional Notes:
        // 
        // * The polling loop and Iframe are not created until at least one handler
        //   is actually bound to the 'hashchange' event.
        // * If you need the bound handler(s) to execute immediately, in cases where
        //   a location.hash exists on page load, via bookmark or page refresh for
        //   example, use jQuery(window).hashchange() or the more verbose 
        //   jQuery(window).trigger( 'hashchange' ).
        // * The event can be bound before DOM ready, but since it won't be usable
        //   before then in IE6/7 (due to the necessary Iframe), recommended usage is
        //   to bind it inside a DOM ready handler.

        // Override existing $.event.special.hashchange methods (allowing this plugin
        // to be defined after jQuery BBQ in BBQ's source code).
        special[str_hashchange] = $.extend(special[str_hashchange], {

            // Called only when the first 'hashchange' event is bound to window.
            setup: function () {
                // If window.onhashchange is supported natively, there's nothing to do..
                if (supports_onhashchange) { return false; }

                // Otherwise, we need to create our own. And we don't want to call this
                // until the user binds to the event, just in case they never do, since it
                // will create a polling loop and possibly even a hidden Iframe.
                $(fake_onhashchange.start);
            },

            // Called only when the last 'hashchange' event is unbound from window.
            teardown: function () {
                // If window.onhashchange is supported natively, there's nothing to do..
                if (supports_onhashchange) { return false; }

                // Otherwise, we need to stop ours (if possible).
                $(fake_onhashchange.stop);
            }

        });

        // fake_onhashchange does all the work of triggering the window.onhashchange
        // event for browsers that don't natively support it, including creating a
        // polling loop to watch for hash changes and in IE 6/7 creating a hidden
        // Iframe to enable back and forward.
        fake_onhashchange = (function () {
            var self = {},
              timeout_id,

              // Remember the initial hash so it doesn't get triggered immediately.
              last_hash = get_fragment(),

              fn_retval = function (val) { return val; },
              history_set = fn_retval,
              history_get = fn_retval;

            // Start the polling loop.
            self.start = function () {
                timeout_id || poll();
            };

            // Stop the polling loop.
            self.stop = function () {
                timeout_id && clearTimeout(timeout_id);
                timeout_id = undefined;
            };

            // This polling loop checks every $.fn.hashchange.delay milliseconds to see
            // if location.hash has changed, and triggers the 'hashchange' event on
            // window when necessary.
            function poll() {
                var hash = get_fragment(),
                  history_hash = history_get(last_hash);

                if (hash !== last_hash) {
                    history_set(last_hash = hash, history_hash);

                    $(window).trigger(str_hashchange);

                } else if (history_hash !== last_hash) {
                    location.href = location.href.replace(/#.*/, '') + history_hash;
                }

                timeout_id = setTimeout(poll, $.fn[str_hashchange].delay);
            };

            return self;
        })();

    })(jQuery, this);

    (function ($) {
        $.mobile = {};
    }(jQuery));

    (function ($, window, undefined) {
        $.extend($.mobile, {

            // Version of the jQuery Mobile Framework
            version: "1.4.5",

            // Deprecated and no longer used in 1.4 remove in 1.5
            // Define the url parameter used for referencing widget-generated sub-pages.
            // Translates to example.html&ui-page=subpageIdentifier
            // hash segment before &ui-page= is used to make Ajax request
            subPageUrlKey: "ui-page",

            hideUrlBar: true,

            // Keepnative Selector
            keepNative: "[data-role='none']",

            // Deprecated in 1.4 remove in 1.5
            // Class assigned to page currently in view, and during transitions
            activePageClass: "ui-page-active",

            // Deprecated in 1.4 remove in 1.5
            // Class used for "active" button state, from CSS framework
            activeBtnClass: "ui-btn-active",

            // Deprecated in 1.4 remove in 1.5
            // Class used for "focus" form element state, from CSS framework
            focusClass: "ui-focus",

            // Automatically handle clicks and form submissions through Ajax, when same-domain
            ajaxEnabled: true,

            // disable to prevent jquery from bothering with links
            linkBindingEnabled: true,

            // Set default page transition - 'none' for no transitions
            defaultPageTransition: "fade",

            // Set maximum window width for transitions to apply - 'false' for no limit
            maxTransitionWidth: false,

            // Minimum scroll distance that will be remembered when returning to a page
            // Deprecated remove in 1.5
            minScrollBack: 0,

            // Set default dialog transition - 'none' for no transitions
            defaultDialogTransition: "pop",

            // For error messages, which theme does the box use?
            pageLoadErrorMessageTheme: "a",

            // replace calls to window.history.back with phonegaps navigation helper
            // where it is provided on the window object
            phonegapNavigationEnabled: false,

            // allows users to opt in to ignoring content by marking a parent element as
            // data-ignored
            ignoreContentEnabled: false,

            // disable the alteration of the dynamic base tag or links in the case
            // that a dynamic base tag isn't supported
            dynamicBaseEnabled: true,

            // default the property to remove dependency on assignment in init module
            pageContainer: $(),

            //enable cross-domain page support
            allowCrossDomainPages: false,

            dialogHashKey: "&ui-state=dialog"
        });
    })(jQuery, this);

    (function ($, window, undefined) {
        var nsNormalizeDict = {},
            oldFind = $.find,
            rbrace = /(?:\{[\s\S]*\}|\[[\s\S]*\])$/,
            jqmDataRE = /:jqmData\(([^)]*)\)/g;

        $.extend($.mobile, {

            // Namespace used framework-wide for data-attrs. Default is no namespace

            ns: "",

            // Retrieve an attribute from an element and perform some massaging of the value

            getAttribute: function (element, key) {
                var data;

                element = element.jquery ? element[0] : element;

                if (element && element.getAttribute) {
                    data = element.getAttribute("data-" + $.mobile.ns + key);
                }

                // Copied from core's src/data.js:dataAttr()
                // Convert from a string to a proper data type
                try {
                    data = data === "true" ? true :
                        data === "false" ? false :
                        data === "null" ? null :
                        // Only convert to a number if it doesn't change the string
                        +data + "" === data ? +data :
                        rbrace.test(data) ? JSON.parse(data) :
                        data;
                } catch (err) { }

                return data;
            },

            // Expose our cache for testing purposes.
            nsNormalizeDict: nsNormalizeDict,

            // Take a data attribute property, prepend the namespace
            // and then camel case the attribute string. Add the result
            // to our nsNormalizeDict so we don't have to do this again.
            nsNormalize: function (prop) {
                return nsNormalizeDict[prop] ||
                    (nsNormalizeDict[prop] = $.camelCase($.mobile.ns + prop));
            }

        });

        // Mobile version of data and removeData and hasData methods
        // ensures all data is set and retrieved using jQuery Mobile's data namespace
        $.fn.jqmData = function (prop, value) {
            var result;
            if (typeof prop !== "undefined") {
                if (prop) {
                    prop = $.mobile.nsNormalize(prop);
                }

                // undefined is permitted as an explicit input for the second param
                // in this case it returns the value and does not set it to undefined
                if (arguments.length < 2 || value === undefined) {
                    result = this.data(prop);
                } else {
                    result = this.data(prop, value);
                }
            }
            return result;
        };

        $.jqmData = function (elem, prop, value) {
            var result;
            if (typeof prop !== "undefined") {
                result = $.data(elem, prop ? $.mobile.nsNormalize(prop) : prop, value);
            }
            return result;
        };

        $.fn.jqmRemoveData = function (prop) {
            return this.removeData($.mobile.nsNormalize(prop));
        };

        $.jqmRemoveData = function (elem, prop) {
            return $.removeData(elem, $.mobile.nsNormalize(prop));
        };

        $.find = function (selector, context, ret, extra) {
            if (selector.indexOf(":jqmData") > -1) {
                selector = selector.replace(jqmDataRE, "[data-" + ($.mobile.ns || "") + "$1]");
            }

            return oldFind.call(this, selector, context, ret, extra);
        };

        $.extend($.find, oldFind);

    })(jQuery, this);

    /*!
     * jQuery UI Core c0ab71056b936627e8a7821f03c044aec6280a40
     * http://jqueryui.com
     *
     * Copyright 2013 jQuery Foundation and other contributors
     * Released under the MIT license.
     * http://jquery.org/license
     *
     * http://api.jqueryui.com/category/ui-core/
     */
    (function ($, undefined) {

        // $.ui might exist from components with no dependencies, e.g., $.ui.position
        $.ui = $.ui || {};

        $.extend($.ui, {
            version: "c0ab71056b936627e8a7821f03c044aec6280a40",

            keyCode: {
                BACKSPACE: 8,
                COMMA: 188,
                DELETE: 46,
                DOWN: 40,
                END: 35,
                ENTER: 13,
                ESCAPE: 27,
                HOME: 36,
                LEFT: 37,
                PAGE_DOWN: 34,
                PAGE_UP: 33,
                PERIOD: 190,
                RIGHT: 39,
                SPACE: 32,
                TAB: 9,
                UP: 38
            }
        });

        // deprecated
        $.ui.ie = !!/msie [\w.]+/.exec(navigator.userAgent.toLowerCase());

    })(jQuery);

    (function ($, window, undefined) {

        function parentWithClass(elem, className) {

            while (!elem.classList || !elem.classList.contains(className)) {
                elem = elem.parentNode;

                if (!elem) {
                    return null;
                }
            }

            return elem;
        }

        $.extend($.mobile, {
            // define the window and the document objects
            window: $(window),
            document: $(document),

            // TODO: Remove and use $.ui.keyCode directly
            keyCode: $.ui.keyCode,

            // Place to store various widget extensions
            behaviors: {},

            // Scroll page vertically: scroll to 0 to hide iOS address bar, or pass a Y value
            silentScroll: function (ypos) {
                if ($.type(ypos) !== "number") {
                    ypos = $.mobile.defaultHomeScroll;
                }

                window.scrollTo(0, ypos);
            },

            getClosestBaseUrl: function (ele) {

                var page = parentWithClass(ele, 'ui-page');

                // Find the closest page and extract out its url.
                var url = (page ? page.getAttribute("data-url") : null),
                    base = $.mobile.path.documentBase.hrefNoHash;

                if (!$.mobile.dynamicBaseEnabled || !url || !$.mobile.path.isPath(url)) {
                    url = base;
                }

                return $.mobile.path.makeUrlAbsolute(url, base);
            },
            removeActiveLinkClass: function (forceRemoval) {
            },

            getScreenHeight: function () {
                // Native innerHeight returns more accurate value for this across platforms,
                // jQuery version is here as a normalized fallback for platforms like Symbian
                return window.innerHeight || $.mobile.window.height();
            },

            //simply set the active page's minimum height to screen height, depending on orientation
            resetActivePageHeight: function (height) {

            }
        });

        // plugins
        $.fn.extend({
            // Enhance child elements
            enhanceWithin: function () {
                var index,
                    widgetElements = {},
                    that = this;

                // Enhance widgets
                $.each($.mobile.widgets, function (name, constructor) {

                    // If initSelector not false find elements
                    if (constructor.initSelector) {

                        // Filter elements that should not be enhanced based on parents
                        var elements = that[0].querySelectorAll(constructor.initSelector);

                        // Enhance whatever is left
                        if (elements.length > 0) {
                            widgetElements[constructor.prototype.widgetName] = $(elements);
                        }
                    }
                });

                for (index in widgetElements) {
                    widgetElements[index][index]();
                }

                return this;
            },

            // note that this helper doesn't attempt to handle the callback
            // or setting of an html element's text, its only purpose is
            // to return the html encoded version of the text in all cases. (thus the name)
            getEncodedText: function () {
                return $("<a>").text(this.text()).html();
            }
        });

        $.find.matches = function (expr, set) {
            return $.find(expr, null, null, set);
        };

        $.find.matchesSelector = function (node, expr) {
            return $.find(expr, null, null, [node]).length > 0;
        };

    })(jQuery, this);


    (function ($, undefined) {

        /*! matchMedia() polyfill - Test a CSS media type/query in JS. Authors & copyright (c) 2012: Scott Jehl, Paul Irish, Nicholas Zakas. Dual MIT/BSD license */
        window.matchMedia = window.matchMedia || (function (doc, undefined) {

            var bool,
                docElem = doc.documentElement,
                refNode = docElem.firstElementChild || docElem.firstChild,
                // fakeBody required for <FF4 when executed in <head>
                fakeBody = doc.createElement("body"),
                div = doc.createElement("div");

            div.id = "mq-test-1";
            div.style.cssText = "position:absolute;top:-100em";
            fakeBody.style.background = "none";
            fakeBody.appendChild(div);

            return function (q) {

                div.innerHTML = "&shy;<style media=\"" + q + "\"> #mq-test-1 { width: 42px; }</style>";

                docElem.insertBefore(fakeBody, refNode);
                bool = div.offsetWidth === 42;
                docElem.removeChild(fakeBody);

                return {
                    matches: bool,
                    media: q
                };

            };

        }(document));

        // $.mobile.media uses matchMedia to return a boolean.
        $.mobile.media = function (q) {
            return window.matchMedia(q).matches;
        };

    })(jQuery);

    (function ($, undefined) {
        var support = {
            touch: "ontouchend" in document
        };

        $.mobile.support = $.mobile.support || {};
        $.extend($.support, support);
        $.extend($.mobile.support, support);
    }(jQuery));

    (function ($, undefined) {
        $.extend($.support, {
            orientation: "orientation" in window && "onorientationchange" in window
        });
    }(jQuery));

    (function ($, undefined) {

        // thx Modernizr
        function propExists(prop) {
            var uc_prop = prop.charAt(0).toUpperCase() + prop.substr(1),
                props = (prop + " " + vendors.join(uc_prop + " ") + uc_prop).split(" "),
                v;

            for (v in props) {
                if (fbCSS[props[v]] !== undefined) {
                    return true;
                }
            }
        }

        var fakeBody = $("<body>").prependTo("html"),
            fbCSS = fakeBody[0].style,
            vendors = ["Webkit", "Moz", "O"],
            webos = "palmGetResource" in window, //only used to rule out scrollTop
            operamini = window.operamini && ({}).toString.call(window.operamini) === "[object OperaMini]",
            bb = window.blackberry && !propExists("-webkit-transform"), //only used to rule out box shadow, as it's filled opaque on BB 5 and lower
            nokiaLTE7_3;

        // inline SVG support test
        function inlineSVG() {
            // Thanks Modernizr & Erik Dahlstrom
            var w = window,
                svg = !!w.document.createElementNS && !!w.document.createElementNS("http://www.w3.org/2000/svg", "svg").createSVGRect && !(w.opera && navigator.userAgent.indexOf("Chrome") === -1),
                support = function (data) {
                    if (!(data && svg)) {
                        $("html").addClass("ui-nosvg");
                    }
                },
                img = new w.Image();

            img.onerror = function () {
                support(false);
            };
            img.onload = function () {
                support(img.width === 1 && img.height === 1);
            };
            img.src = "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==";
        }

        function transform3dTest() {
            var mqProp = "transform-3d",
                // Because the `translate3d` test below throws false positives in Android:
                ret = $.mobile.media("(-" + vendors.join("-" + mqProp + "),(-") + "-" + mqProp + "),(" + mqProp + ")"),
                el, transforms, t;

            if (ret) {
                return !!ret;
            }

            el = document.createElement("div");
            transforms = {
                // We’re omitting Opera for the time being; MS uses unprefixed.
                "MozTransform": "-moz-transform",
                "transform": "transform"
            };

            fakeBody.append(el);

            for (t in transforms) {
                if (el.style[t] !== undefined) {
                    el.style[t] = "translate3d( 100px, 1px, 1px )";
                    ret = window.getComputedStyle(el).getPropertyValue(transforms[t]);
                }
            }
            return (!!ret && ret !== "none");
        }

        // Test for dynamic-updating base tag support ( allows us to avoid href,src attr rewriting )
        function baseTagTest() {
            var fauxBase = location.protocol + "//" + location.host + location.pathname + "ui-dir/",
                base = $("head base"),
                fauxEle = null,
                href = "",
                link, rebase;

            if (!base.length) {
                base = fauxEle = $("<base>", { "href": fauxBase }).appendTo("head");
            } else {
                href = base.attr("href");
            }

            link = $("<a href='testurl' />").prependTo(fakeBody);
            rebase = link[0].href;
            base[0].href = href || location.pathname;

            if (fauxEle) {
                fauxEle.remove();
            }
            return rebase.indexOf(fauxBase) === 0;
        }

        // Thanks Modernizr
        function cssPointerEventsTest() {
            var element = document.createElement("x"),
                documentElement = document.documentElement,
                getComputedStyle = window.getComputedStyle,
                supports;

            if (!("pointerEvents" in element.style)) {
                return false;
            }

            element.style.pointerEvents = "auto";
            element.style.pointerEvents = "x";
            documentElement.appendChild(element);
            supports = getComputedStyle &&
            getComputedStyle(element, "").pointerEvents === "auto";
            documentElement.removeChild(element);
            return !!supports;
        }

        function boundingRect() {
            var div = document.createElement("div");
            return typeof div.getBoundingClientRect !== "undefined";
        }

        // non-UA-based IE version check by James Padolsey, modified by jdalton - from http://gist.github.com/527683
        // allows for inclusion of IE 6+, including Windows Mobile 7
        $.extend($.mobile, { browser: {} });
        $.mobile.browser.oldIE = (function () {
            var v = 3,
                div = document.createElement("div"),
                a = div.all || [];

            do {
                div.innerHTML = "<!--[if gt IE " + (++v) + "]><br><![endif]-->";
            } while (a[0]);

            return v > 4 ? v : !v;
        })();

        $.extend($.support, {
            mediaquery: $.mobile.media("only all"),
            cssPseudoElement: !!propExists("content"),
            touchOverflow: !!propExists("overflowScrolling"),
            cssTransform3d: transform3dTest(),
            fixedPosition: true,
            scrollTop: ("pageXOffset" in window ||
                "scrollTop" in document.documentElement ||
                "scrollTop" in fakeBody[0]) && !webos && !operamini,

            dynamicBaseTag: baseTagTest(),
            cssPointerEvents: cssPointerEventsTest(),
            boundingRect: boundingRect(),
            inlineSVG: inlineSVG
        });

        fakeBody.remove();

        // Support conditions that must be met in order to proceed
        // default enhanced qualifications are media query support OR IE 7+

        $.mobile.gradeA = function () {
            return (($.support.mediaquery && $.support.cssPseudoElement) || $.mobile.browser.oldIE && $.mobile.browser.oldIE >= 8) && ($.support.boundingRect || $.fn.jquery.match(/1\.[0-7+]\.[0-9+]?/) !== null);
        };

        $.mobile.ajaxBlacklist =
                    // BlackBerry browsers, pre-webkit
                    window.blackberry && !window.WebKitPoint ||
                    // Opera Mini
                    operamini;

    })(jQuery);


    (function ($, undefined) {
        var $win = $.mobile.window, self,
            dummyFnToInitNavigate = function () {
            };

        $.event.special.navigate = self = {
            bound: false,

            originalEventName: undefined,

            // TODO a lot of duplication between popstate and hashchange
            popstate: function (event) {
                var newEvent = new $.Event("navigate"),
                    state = event.originalEvent.state || {};

                if (event.historyState) {
                    $.extend(state, event.historyState);
                }

                // Make sure the original event is tracked for the end
                // user to inspect incase they want to do something special
                newEvent.originalEvent = event;

                // NOTE we let the current stack unwind because any assignment to
                //      location.hash will stop the world and run this event handler. By
                //      doing this we create a similar behavior to hashchange on hash
                //      assignment
                setTimeout(function () {
                    $win.trigger(newEvent, {
                        state: state
                    });
                }, 0);
            },

            hashchange: function (event /*, data */) {
                var newEvent = new $.Event("navigate");

                // Make sure the original event is tracked for the end
                // user to inspect incase they want to do something special
                newEvent.originalEvent = event;

                // Trigger the hashchange with state provided by the user
                // that altered the hash
                $win.trigger(newEvent, {
                    // Users that want to fully normalize the two events
                    // will need to do history management down the stack and
                    // add the state to the event before this binding is fired
                    // TODO consider allowing for the explicit addition of callbacks
                    //      to be fired before this value is set to avoid event timing issues
                    state: event.hashchangeState || {}
                });
            },

            // TODO We really only want to set this up once
            //      but I'm not clear if there's a beter way to achieve
            //      this with the jQuery special event structure
            setup: function ( /* data, namespaces */) {
                if (self.bound) {
                    return;
                }

                self.bound = true;

                self.originalEventName = "popstate";
                $win.bind("popstate.navigate", self.popstate);
            }
        };
    })(jQuery);

    (function ($, undefined) {
        var props = {
            "animation": {},
            "transition": {}
        },
            testElement = document.createElement("a"),
            vendorPrefixes = ["", "webkit-", "moz-", "o-"];

        $.each(["animation", "transition"], function (i, test) {

            // Get correct name for test
            var testName = (i === 0) ? test + "-" + "name" : test;

            $.each(vendorPrefixes, function (j, prefix) {
                if (testElement.style[$.camelCase(prefix + testName)] !== undefined) {
                    props[test]["prefix"] = prefix;
                    return false;
                }
            });

            // Set event and duration names for later use
            props[test]["duration"] =
                $.camelCase(props[test]["prefix"] + test + "-" + "duration");
            props[test]["event"] =
                $.camelCase(props[test]["prefix"] + test + "-" + "end");

            // All lower case if not a vendor prop
            if (props[test]["prefix"] === "") {
                props[test]["event"] = props[test]["event"].toLowerCase();
            }
        });

        // If a valid prefix was found then the it is supported by the browser
        $.support.cssTransitions = (props["transition"]["prefix"] !== undefined);
        $.support.cssAnimations = (props["animation"]["prefix"] !== undefined);

        // Remove the testElement
        $(testElement).remove();

        // Animation complete callback
        $.fn.animationComplete = function (callback, type, fallbackTime) {
            var timer, duration,
                that = this,
                eventBinding = function () {

                    // Clear the timer so we don't call callback twice
                    clearTimeout(timer);
                    callback.apply(this, arguments);
                },
                animationType = (!type || type === "animation") ? "animation" : "transition";

            // Make sure selected type is supported by browser
            if (($.support.cssTransitions && animationType === "transition") ||
                ($.support.cssAnimations && animationType === "animation")) {

                // If a fallback time was not passed set one
                if (fallbackTime === undefined) {

                    // Make sure the was not bound to document before checking .css
                    if ($(this).context !== document) {

                        // Parse the durration since its in second multiple by 1000 for milliseconds
                        // Multiply by 3 to make sure we give the animation plenty of time.
                        duration = parseFloat(
                            $(this).css(props[animationType].duration)
                        ) * 3000;
                    }

                    // If we could not read a duration use the default
                    if (duration === 0 || duration === undefined || isNaN(duration)) {
                        duration = $.fn.animationComplete.defaultDuration;
                    }
                }

                // Sets up the fallback if event never comes
                timer = setTimeout(function () {
                    $(that).off(props[animationType].event, eventBinding);
                    callback.apply(that);
                }, duration);

                // Bind the event
                return $(this).one(props[animationType].event, eventBinding);
            } else {

                // CSS animation / transitions not supported
                // Defer execution for consistency between webkit/non webkit
                setTimeout($.proxy(callback, this), 0);
                return $(this);
            }
        };

        // Allow default callback to be configured on mobileInit
        $.fn.animationComplete.defaultDuration = 1000;
    })(jQuery);

    /*!
     * jQuery UI Widget c0ab71056b936627e8a7821f03c044aec6280a40
     * http://jqueryui.com
     *
     * Copyright 2013 jQuery Foundation and other contributors
     * Released under the MIT license.
     * http://jquery.org/license
     *
     * http://api.jqueryui.com/jQuery.widget/
     */
    (function ($, undefined) {

        var uuid = 0,
            slice = Array.prototype.slice,
            _cleanData = $.cleanData;
        $.cleanData = function (elems) {
            for (var i = 0, elem; (elem = elems[i]) != null; i++) {
                try {
                    $(elem).triggerHandler("remove");
                    // http://bugs.jquery.com/ticket/8235
                } catch (e) { }
            }
            _cleanData(elems);
        };

        $.widget = function (name, base, prototype) {
            var fullName, existingConstructor, constructor, basePrototype,
                // proxiedPrototype allows the provided prototype to remain unmodified
                // so that it can be used as a mixin for multiple widgets (#8876)
                proxiedPrototype = {},
                namespace = name.split(".")[0];

            name = name.split(".")[1];
            fullName = namespace + "-" + name;

            if (!prototype) {
                prototype = base;
                base = $.Widget;
            }

            // create selector for plugin
            $.expr[":"][fullName.toLowerCase()] = function (elem) {
                return !!$.data(elem, fullName);
            };

            $[namespace] = $[namespace] || {};
            existingConstructor = $[namespace][name];
            constructor = $[namespace][name] = function (options, element) {
                // allow instantiation without "new" keyword
                if (!this._createWidget) {
                    return new constructor(options, element);
                }

                // allow instantiation without initializing for simple inheritance
                // must use "new" keyword (the code above always passes args)
                if (arguments.length) {
                    this._createWidget(options, element);
                }
            };
            // extend with the existing constructor to carry over any static properties
            $.extend(constructor, existingConstructor, {
                version: prototype.version,
                // copy the object used to create the prototype in case we need to
                // redefine the widget later
                _proto: $.extend({}, prototype),
                // track widgets that inherit from this widget in case this widget is
                // redefined after a widget inherits from it
                _childConstructors: []
            });

            basePrototype = new base();
            // we need to make the options hash a property directly on the new instance
            // otherwise we'll modify the options hash on the prototype that we're
            // inheriting from
            basePrototype.options = $.widget.extend({}, basePrototype.options);
            $.each(prototype, function (prop, value) {
                if (!$.isFunction(value)) {
                    proxiedPrototype[prop] = value;
                    return;
                }
                proxiedPrototype[prop] = (function () {
                    var _super = function () {
                        return base.prototype[prop].apply(this, arguments);
                    },
                        _superApply = function (args) {
                            return base.prototype[prop].apply(this, args);
                        };
                    return function () {
                        var __super = this._super,
                            __superApply = this._superApply,
                            returnValue;

                        this._super = _super;
                        this._superApply = _superApply;

                        returnValue = value.apply(this, arguments);

                        this._super = __super;
                        this._superApply = __superApply;

                        return returnValue;
                    };
                })();
            });
            constructor.prototype = $.widget.extend(basePrototype, {
                // TODO: remove support for widgetEventPrefix
                // always use the name + a colon as the prefix, e.g., draggable:start
                // don't prefix for widgets that aren't DOM-based
                widgetEventPrefix: existingConstructor ? (basePrototype.widgetEventPrefix || name) : name
            }, proxiedPrototype, {
                constructor: constructor,
                namespace: namespace,
                widgetName: name,
                widgetFullName: fullName
            });

            // If this widget is being redefined then we need to find all widgets that
            // are inheriting from it and redefine all of them so that they inherit from
            // the new version of this widget. We're essentially trying to replace one
            // level in the prototype chain.
            if (existingConstructor) {
                $.each(existingConstructor._childConstructors, function (i, child) {
                    var childPrototype = child.prototype;

                    // redefine the child widget using the same prototype that was
                    // originally used, but inherit from the new version of the base
                    $.widget(childPrototype.namespace + "." + childPrototype.widgetName, constructor, child._proto);
                });
                // remove the list of existing child constructors from the old constructor
                // so the old child constructors can be garbage collected
                delete existingConstructor._childConstructors;
            } else {
                base._childConstructors.push(constructor);
            }

            $.widget.bridge(name, constructor);

            return constructor;
        };

        $.widget.extend = function (target) {
            var input = slice.call(arguments, 1),
                inputIndex = 0,
                inputLength = input.length,
                key,
                value;
            for (; inputIndex < inputLength; inputIndex++) {
                for (key in input[inputIndex]) {
                    value = input[inputIndex][key];
                    if (input[inputIndex].hasOwnProperty(key) && value !== undefined) {
                        // Clone objects
                        if ($.isPlainObject(value)) {
                            target[key] = $.isPlainObject(target[key]) ?
                                $.widget.extend({}, target[key], value) :
                                // Don't extend strings, arrays, etc. with objects
                                $.widget.extend({}, value);
                            // Copy everything else by reference
                        } else {
                            target[key] = value;
                        }
                    }
                }
            }
            return target;
        };

        $.widget.bridge = function (name, object) {

            var fullName = object.prototype.widgetFullName || name;
            $.fn[name] = function (options) {
                var isMethodCall = typeof options === "string",
                    args = slice.call(arguments, 1),
                    returnValue = this;

                // allow multiple hashes to be passed on init
                options = !isMethodCall && args.length ?
                    $.widget.extend.apply(null, [options].concat(args)) :
                    options;

                if (isMethodCall) {
                    this.each(function () {
                        var methodValue,
                            instance = $.data(this, fullName);
                        if (options === "instance") {
                            returnValue = instance;
                            return false;
                        }
                        if (!instance) {
                            return $.error("cannot call methods on " + name + " prior to initialization; " +
                                "attempted to call method '" + options + "'");
                        }
                        if (!$.isFunction(instance[options]) || options.charAt(0) === "_") {
                            return $.error("no such method '" + options + "' for " + name + " widget instance");
                        }
                        methodValue = instance[options].apply(instance, args);
                        if (methodValue !== instance && methodValue !== undefined) {
                            returnValue = methodValue && methodValue.jquery ?
                                returnValue.pushStack(methodValue.get()) :
                                methodValue;
                            return false;
                        }
                    });
                } else {
                    this.each(function () {
                        var instance = $.data(this, fullName);
                        if (instance) {
                            instance.option(options || {})._init();
                        } else {
                            $.data(this, fullName, new object(options, this));
                        }
                    });
                }

                return returnValue;
            };
        };

        $.Widget = function ( /* options, element */) { };
        $.Widget._childConstructors = [];

        $.Widget.prototype = {
            widgetName: "widget",
            widgetEventPrefix: "",
            defaultElement: "<div>",
            options: {
                disabled: false,

                // callbacks
                create: null
            },
            _createWidget: function (options, element) {
                element = $(element || this.defaultElement || this)[0];
                this.element = $(element);
                this.uuid = uuid++;
                this.eventNamespace = "." + this.widgetName + this.uuid;
                this.options = $.widget.extend({},
                    this.options,
                    this._getCreateOptions(),
                    options);

                this.bindings = $();
                this.hoverable = $();
                this.focusable = $();

                if (element !== this) {
                    $.data(element, this.widgetFullName, this);
                    this._on(true, this.element, {
                        remove: function (event) {
                            if (event.target === element) {
                                this.destroy();
                            }
                        }
                    });
                    this.document = $(element.style ?
                        // element within the document
                        element.ownerDocument :
                        // element is window or document
                        element.document || element);
                    this.window = $(this.document[0].defaultView || this.document[0].parentWindow);
                }

                this._create();
                this._trigger("create", null, this._getCreateEventData());
                this._init();
            },
            _getCreateOptions: $.noop,
            _getCreateEventData: $.noop,
            _create: $.noop,
            _init: $.noop,

            destroy: function () {
                this._destroy();
                // we can probably remove the unbind calls in 2.0
                // all event bindings should go through this._on()
                this.element
                    .unbind(this.eventNamespace)
                    .removeData(this.widgetFullName)
                    // support: jquery <1.6.3
                    // http://bugs.jquery.com/ticket/9413
                    .removeData($.camelCase(this.widgetFullName));
                this.widget()
                    .unbind(this.eventNamespace)
                    .removeAttr("aria-disabled")
                    .removeClass(
                        this.widgetFullName + "-disabled " +
                        "ui-state-disabled");

                // clean up events and states
                this.bindings.unbind(this.eventNamespace);
                this.hoverable.removeClass("ui-state-hover");
                this.focusable.removeClass("ui-state-focus");
            },
            _destroy: $.noop,

            widget: function () {
                return this.element;
            },

            option: function (key, value) {
                var options = key,
                    parts,
                    curOption,
                    i;

                if (arguments.length === 0) {
                    // don't return a reference to the internal hash
                    return $.widget.extend({}, this.options);
                }

                if (typeof key === "string") {
                    // handle nested keys, e.g., "foo.bar" => { foo: { bar: ___ } }
                    options = {};
                    parts = key.split(".");
                    key = parts.shift();
                    if (parts.length) {
                        curOption = options[key] = $.widget.extend({}, this.options[key]);
                        for (i = 0; i < parts.length - 1; i++) {
                            curOption[parts[i]] = curOption[parts[i]] || {};
                            curOption = curOption[parts[i]];
                        }
                        key = parts.pop();
                        if (value === undefined) {
                            return curOption[key] === undefined ? null : curOption[key];
                        }
                        curOption[key] = value;
                    } else {
                        if (value === undefined) {
                            return this.options[key] === undefined ? null : this.options[key];
                        }
                        options[key] = value;
                    }
                }

                this._setOptions(options);

                return this;
            },
            _setOptions: function (options) {
                var key;

                for (key in options) {
                    this._setOption(key, options[key]);
                }

                return this;
            },
            _setOption: function (key, value) {
                this.options[key] = value;

                if (key === "disabled") {
                    this.widget()
                        .toggleClass(this.widgetFullName + "-disabled", !!value);
                    this.hoverable.removeClass("ui-state-hover");
                    this.focusable.removeClass("ui-state-focus");
                }

                return this;
            },

            enable: function () {
                return this._setOptions({ disabled: false });
            },
            disable: function () {
                return this._setOptions({ disabled: true });
            },

            _on: function (suppressDisabledCheck, element, handlers) {
                var delegateElement,
                    instance = this;

                // no suppressDisabledCheck flag, shuffle arguments
                if (typeof suppressDisabledCheck !== "boolean") {
                    handlers = element;
                    element = suppressDisabledCheck;
                    suppressDisabledCheck = false;
                }

                // no element argument, shuffle and use this.element
                if (!handlers) {
                    handlers = element;
                    element = this.element;
                    delegateElement = this.widget();
                } else {
                    // accept selectors, DOM elements
                    element = delegateElement = $(element);
                    this.bindings = this.bindings.add(element);
                }

                $.each(handlers, function (event, handler) {
                    function handlerProxy() {
                        // allow widgets to customize the disabled handling
                        // - disabled as an array instead of boolean
                        // - disabled class as method for disabling individual parts
                        if (!suppressDisabledCheck &&
                                (instance.options.disabled === true ||
                                    $(this).hasClass("ui-state-disabled"))) {
                            return;
                        }
                        return (typeof handler === "string" ? instance[handler] : handler)
                            .apply(instance, arguments);
                    }

                    // copy the guid so direct unbinding works
                    if (typeof handler !== "string") {
                        handlerProxy.guid = handler.guid =
                            handler.guid || handlerProxy.guid || $.guid++;
                    }

                    var match = event.match(/^(\w+)\s*(.*)$/),
                        eventName = match[1] + instance.eventNamespace,
                        selector = match[2];
                    if (selector) {
                        delegateElement.delegate(selector, eventName, handlerProxy);
                    } else {
                        element.bind(eventName, handlerProxy);
                    }
                });
            },

            _off: function (element, eventName) {
                eventName = (eventName || "").split(" ").join(this.eventNamespace + " ") + this.eventNamespace;
                element.unbind(eventName).undelegate(eventName);
            },

            _delay: function (handler, delay) {
                function handlerProxy() {
                    return (typeof handler === "string" ? instance[handler] : handler)
                        .apply(instance, arguments);
                }
                var instance = this;
                return setTimeout(handlerProxy, delay || 0);
            },

            _hoverable: function (element) {
                this.hoverable = this.hoverable.add(element);
                this._on(element, {
                    mouseenter: function (event) {
                        $(event.currentTarget).addClass("ui-state-hover");
                    },
                    mouseleave: function (event) {
                        $(event.currentTarget).removeClass("ui-state-hover");
                    }
                });
            },

            _focusable: function (element) {
                this.focusable = this.focusable.add(element);
                this._on(element, {
                    focusin: function (event) {
                        $(event.currentTarget).addClass("ui-state-focus");
                    },
                    focusout: function (event) {
                        $(event.currentTarget).removeClass("ui-state-focus");
                    }
                });
            },

            _trigger: function (type, event, data) {
                var prop, orig,
                    callback = this.options[type];

                data = data || {};
                event = $.Event(event);
                event.type = (type === this.widgetEventPrefix ?
                    type :
                    this.widgetEventPrefix + type).toLowerCase();
                // the original event may come from any element
                // so we need to reset the target on the new event
                event.target = this.element[0];

                // copy original event properties over to the new event
                orig = event.originalEvent;
                if (orig) {
                    for (prop in orig) {
                        if (!(prop in event)) {
                            event[prop] = orig[prop];
                        }
                    }
                }

                this.element.trigger(event, data);
                return !($.isFunction(callback) &&
                    callback.apply(this.element[0], [event].concat(data)) === false ||
                    event.isDefaultPrevented());
            }
        };

        $.each({ show: "fadeIn", hide: "fadeOut" }, function (method, defaultEffect) {
            $.Widget.prototype["_" + method] = function (element, options, callback) {
                if (typeof options === "string") {
                    options = { effect: options };
                }
                var hasOptions,
                    effectName = !options ?
                    method :
                        options === true || typeof options === "number" ?
                    defaultEffect :
                            options.effect || defaultEffect;
                options = options || {};
                if (typeof options === "number") {
                    options = { duration: options };
                }
                hasOptions = !$.isEmptyObject(options);
                options.complete = callback;
                if (options.delay) {
                    element.delay(options.delay);
                }
                if (hasOptions && $.effects && $.effects.effect[effectName]) {
                    element[method](options);
                } else if (effectName !== method && element[effectName]) {
                    element[effectName](options.duration, options.easing, callback);
                } else {
                    element.queue(function (next) {
                        $(this)[method]();
                        if (callback) {
                            callback.call(element[0]);
                        }
                        next();
                    });
                }
            };
        });

    })(jQuery);

    (function ($, undefined) {

        var rcapitals = /[A-Z]/g,
            replaceFunction = function (c) {
                return "-" + c.toLowerCase();
            };

        $.extend($.Widget.prototype, {
            _getCreateOptions: function () {
                var option, value,
                    elem = this.element[0],
                    options = {};

                //
                if (!$.mobile.getAttribute(elem, "defaults")) {
                    for (option in this.options) {
                        value = $.mobile.getAttribute(elem, option.replace(rcapitals, replaceFunction));

                        if (value != null) {
                            options[option] = value;
                        }
                    }
                }

                return options;
            }
        });

        //TODO: Remove in 1.5 for backcompat only
        $.mobile.widget = $.Widget;

    })(jQuery);

    (function ($, undefined) {
        $.mobile.widgets = {};

        var originalWidget = $.widget,

            // Record the original, non-mobileinit-modified version of $.mobile.keepNative
            // so we can later determine whether someone has modified $.mobile.keepNative
            keepNativeFactoryDefault = $.mobile.keepNative;

        $.widget = (function (orig) {
            return function () {
                var constructor = orig.apply(this, arguments),
                    name = constructor.prototype.widgetName;

                constructor.initSelector = ((constructor.prototype.initSelector !== undefined) ?
                    constructor.prototype.initSelector : "*[data-role='" + name + "']:not([data-role='none'])");

                $.mobile.widgets[name] = constructor;

                return constructor;
            };
        })($.widget);

        // Make sure $.widget still has bridge and extend methods
        $.extend($.widget, originalWidget);

        // For backcompat remove in 1.5
        $.mobile.document.on("create", function (event) {
            $(event.target).enhanceWithin();
        });

        $.widget("mobile.page", {
            options: {
                theme: "a",
                domCache: false,

                // Deprecated in 1.4 remove in 1.5
                keepNativeDefault: $.mobile.keepNative,

                // Deprecated in 1.4 remove in 1.5
                contentTheme: null,
                enhanced: false
            },

            // DEPRECATED for > 1.4
            // TODO remove at 1.5
            _createWidget: function () {

                $.Widget.prototype._createWidget.apply(this, arguments);
                this._trigger("init");
            },

            _create: function () {

                if (!this.options.enhanced) {
                    this._enhance();
                }

                this._on(this.element, {
                    pagebeforehide: "removeContainerBackground",
                    pagebeforeshow: "_handlePageBeforeShow"
                });

                this.element.enhanceWithin();
            },

            _enhance: function () {
                var attrPrefix = "data-" + $.mobile.ns,
                    self = this;

                var element = this.element[0];

                if (this.options.role) {
                    element.setAttribute("data-" + $.mobile.ns + "role", this.options.role);
                }

                element.setAttribute("tabindex", "0");
                element.classList.add("ui-page");
                element.classList.add("ui-page-theme-" + this.options.theme);

                var contents = element.querySelectorAll("div[data-role='content']");

                for (var i = 0, length = contents.length; i < length; i++) {
                    var content = contents[i];
                    var theme = content.getAttribute(attrPrefix + "theme") || undefined;
                    self.options.contentTheme = theme || self.options.contentTheme || (self.options.dialog && self.options.theme) || (self.element.jqmData("role") === "dialog" && self.options.theme);
                    content.classList.add("ui-content");
                    if (self.options.contentTheme) {
                        content.classList.add("ui-body-" + (self.options.contentTheme));
                    }
                    // Add ARIA role
                    content.setAttribute("role", "main");
                    content.classList.add("ui-content");
                }
            },

            _setOptions: function (o) {

                var elem = this.element[0];
                if (o.theme !== undefined) {
                    elem.classList.remove("ui-page-theme-" + this.options.theme);
                    elem.classList.add("ui-page-theme-" + o.theme);
                }

                if (o.contentTheme !== undefined) {
                    var elems = elem.querySelectorAll("*[data-" + $.mobile.ns + "='content']");
                    for (var i = 0, length = elems.length; i < length; i++) {
                        var el = elems[i];
                        el.classList.remove("ui-body-" + this.options.contentTheme);
                        el.classList.add("ui-body-" + o.contentTheme);
                    }
                }
            },

            _handlePageBeforeShow: function (/* e */) {
                this.setContainerBackground();
            },
            // Deprecated in 1.4 remove in 1.5
            removeContainerBackground: function () {
                $(this.element[0].parentNode).pagecontainer({ "theme": "none" });
            },
            // Deprecated in 1.4 remove in 1.5
            // set the page container background to the page theme
            setContainerBackground: function (theme) {
                $(this.element[0].parentNode).pagecontainer({ "theme": theme || this.options.theme });
            },
            // Deprecated in 1.4 remove in 1.5
            keepNativeSelector: function () {
                var options = this.options,
                    keepNative = $.trim(options.keepNative || ""),
                    globalValue = $.trim($.mobile.keepNative),
                    optionValue = $.trim(options.keepNativeDefault),

                    // Check if $.mobile.keepNative has changed from the factory default
                    newDefault = (keepNativeFactoryDefault === globalValue ?
                        "" : globalValue),

                    // If $.mobile.keepNative has not changed, use options.keepNativeDefault
                    oldDefault = (newDefault === "" ? optionValue : "");

                // Concatenate keepNative selectors from all sources where the value has
                // changed or, if nothing has changed, return the default
                return ((keepNative ? [keepNative] : [])
                    .concat(newDefault ? [newDefault] : [])
                    .concat(oldDefault ? [oldDefault] : [])
                    .join(", "));
            }
        });
    })(jQuery);


    (function ($, undefined) {
        var path, $base, dialogHashKey = "&ui-state=dialog";

        $.mobile.path = path = {
            uiStateKey: "&ui-state",

            // This scary looking regular expression parses an absolute URL or its relative
            // variants (protocol, site, document, query, and hash), into the various
            // components (protocol, host, path, query, fragment, etc that make up the
            // URL as well as some other commonly used sub-parts. When used with RegExp.exec()
            // or String.match, it parses the URL into a results array that looks like this:
            //
            //     [0]: http://jblas:password@mycompany.com:8080/mail/inbox?msg=1234&type=unread#msg-content
            //     [1]: http://jblas:password@mycompany.com:8080/mail/inbox?msg=1234&type=unread
            //     [2]: http://jblas:password@mycompany.com:8080/mail/inbox
            //     [3]: http://jblas:password@mycompany.com:8080
            //     [4]: http:
            //     [5]: //
            //     [6]: jblas:password@mycompany.com:8080
            //     [7]: jblas:password
            //     [8]: jblas
            //     [9]: password
            //    [10]: mycompany.com:8080
            //    [11]: mycompany.com
            //    [12]: 8080
            //    [13]: /mail/inbox
            //    [14]: /mail/
            //    [15]: inbox
            //    [16]: ?msg=1234&type=unread
            //    [17]: #msg-content
            //
            urlParseRE: /^\s*(((([^:\/#\?]+:)?(?:(\/\/)((?:(([^:@\/#\?]+)(?:\:([^:@\/#\?]+))?)@)?(([^:\/#\?\]\[]+|\[[^\/\]@#?]+\])(?:\:([0-9]+))?))?)?)?((\/?(?:[^\/\?#]+\/+)*)([^\?#]*)))?(\?[^#]+)?)(#.*)?/,

            // Abstraction to address xss (Issue #4787) by removing the authority in
            // browsers that auto-decode it. All references to location.href should be
            // replaced with a call to this method so that it can be dealt with properly here
            getLocation: function (url) {
                var parsedUrl = this.parseUrl(url || location.href),
					uri = url ? parsedUrl : location,

					// Make sure to parse the url or the location object for the hash because using
					// location.hash is autodecoded in firefox, the rest of the url should be from
					// the object (location unless we're testing) to avoid the inclusion of the
					// authority
					hash = parsedUrl.hash;

                // mimic the browser with an empty string when the hash is empty
                hash = hash === "#" ? "" : hash;

                return uri.protocol +
					parsedUrl.doubleSlash +
					uri.host +

					// The pathname must start with a slash if there's a protocol, because you
					// can't have a protocol followed by a relative path. Also, it's impossible to
					// calculate absolute URLs from relative ones if the absolute one doesn't have
					// a leading "/".
					((uri.protocol !== "" && uri.pathname.substring(0, 1) !== "/") ?
						"/" : "") +
					uri.pathname +
					uri.search +
					hash;
            },

            //return the original document url
            getDocumentUrl: function (asParsedObject) {
                return asParsedObject ? $.extend({}, path.documentUrl) : path.documentUrl.href;
            },

            parseLocation: function () {
                return this.parseUrl(this.getLocation());
            },

            //Parse a URL into a structure that allows easy access to
            //all of the URL components by name.
            parseUrl: function (url) {
                // If we're passed an object, we'll assume that it is
                // a parsed url object and just return it back to the caller.
                if ($.type(url) === "object") {
                    return url;
                }

                var matches = path.urlParseRE.exec(url || "") || [];

                // Create an object that allows the caller to access the sub-matches
                // by name. Note that IE returns an empty string instead of undefined,
                // like all other browsers do, so we normalize everything so its consistent
                // no matter what browser we're running on.
                return {
                    href: matches[0] || "",
                    hrefNoHash: matches[1] || "",
                    hrefNoSearch: matches[2] || "",
                    domain: matches[3] || "",
                    protocol: matches[4] || "",
                    doubleSlash: matches[5] || "",
                    authority: matches[6] || "",
                    username: matches[8] || "",
                    password: matches[9] || "",
                    host: matches[10] || "",
                    hostname: matches[11] || "",
                    port: matches[12] || "",
                    pathname: matches[13] || "",
                    directory: matches[14] || "",
                    filename: matches[15] || "",
                    search: matches[16] || "",
                    hash: matches[17] || ""
                };
            },

            //Turn relPath into an asbolute path. absPath is
            //an optional absolute path which describes what
            //relPath is relative to.
            makePathAbsolute: function (relPath, absPath) {
                var absStack,
					relStack,
					i, d;

                if (relPath && relPath.charAt(0) === "/") {
                    return relPath;
                }

                relPath = relPath || "";
                absPath = absPath ? absPath.replace(/^\/|(\/[^\/]*|[^\/]+)$/g, "") : "";

                absStack = absPath ? absPath.split("/") : [];
                relStack = relPath.split("/");

                for (i = 0; i < relStack.length; i++) {
                    d = relStack[i];
                    switch (d) {
                        case ".":
                            break;
                        case "..":
                            if (absStack.length) {
                                absStack.pop();
                            }
                            break;
                        default:
                            absStack.push(d);
                            break;
                    }
                }
                return "/" + absStack.join("/");
            },

            //Returns true if both urls have the same domain.
            isSameDomain: function (absUrl1, absUrl2) {
                return path.parseUrl(absUrl1).domain.toLowerCase() ===
					path.parseUrl(absUrl2).domain.toLowerCase();
            },

            //Returns true for any relative variant.
            isRelativeUrl: function (url) {
                // All relative Url variants have one thing in common, no protocol.
                return path.parseUrl(url).protocol === "";
            },

            //Returns true for an absolute url.
            isAbsoluteUrl: function (url) {
                return path.parseUrl(url).protocol !== "";
            },

            //Turn the specified realtive URL into an absolute one. This function
            //can handle all relative variants (protocol, site, document, query, fragment).
            makeUrlAbsolute: function (relUrl, absUrl) {
                if (!path.isRelativeUrl(relUrl)) {
                    return relUrl;
                }

                if (absUrl === undefined) {
                    absUrl = this.documentBase;
                }

                var relObj = path.parseUrl(relUrl),
					absObj = path.parseUrl(absUrl),
					protocol = relObj.protocol || absObj.protocol,
					doubleSlash = relObj.protocol ? relObj.doubleSlash : (relObj.doubleSlash || absObj.doubleSlash),
					authority = relObj.authority || absObj.authority,
					hasPath = relObj.pathname !== "",
					pathname = path.makePathAbsolute(relObj.pathname || absObj.filename, absObj.pathname),
					search = relObj.search || (!hasPath && absObj.search) || "",
					hash = relObj.hash;

                return protocol + doubleSlash + authority + pathname + search + hash;
            },

            //Add search (aka query) params to the specified url.
            addSearchParams: function (url, params) {
                var u = path.parseUrl(url),
					p = (typeof params === "object") ? $.param(params) : params,
					s = u.search || "?";
                return u.hrefNoSearch + s + (s.charAt(s.length - 1) !== "?" ? "&" : "") + p + (u.hash || "");
            },

            convertUrlToDataUrl: function (absUrl) {
                var result = absUrl,
					u = path.parseUrl(absUrl);

                if (path.isEmbeddedPage(u)) {
                    // For embedded pages, remove the dialog hash key as in getFilePath(),
                    // and remove otherwise the Data Url won't match the id of the embedded Page.
                    result = u.hash
						.split(dialogHashKey)[0]
						.replace(/^#/, "")
						.replace(/\?.*$/, "");
                } else if (path.isSameDomain(u, this.documentBase)) {
                    result = u.hrefNoHash.replace(this.documentBase.domain, "").split(dialogHashKey)[0];
                }

                return window.decodeURIComponent(result);
            },

            //get path from current hash, or from a file path
            get: function (newPath) {
                if (newPath === undefined) {
                    newPath = path.parseLocation().hash;
                }
                return path.stripHash(newPath).replace(/[^\/]*\.[^\/*]+$/, "");
            },

            //set location hash to path
            set: function (path) {
                location.hash = path;
            },

            //test if a given url (string) is a path
            //NOTE might be exceptionally naive
            isPath: function (url) {
                return (/\//).test(url);
            },

            //return a url path with the window's location protocol/hostname/pathname removed
            clean: function (url) {
                return url.replace(this.documentBase.domain, "");
            },

            //just return the url without an initial #
            stripHash: function (url) {
                return url.replace(/^#/, "");
            },

            stripQueryParams: function (url) {
                return url.replace(/\?.*$/, "");
            },

            //remove the preceding hash, any query params, and dialog notations
            cleanHash: function (hash) {
                return path.stripHash(hash.replace(/\?.*$/, "").replace(dialogHashKey, ""));
            },

            isHashValid: function (hash) {
                return (/^#[^#]+$/).test(hash);
            },

            //check whether a url is referencing the same domain, or an external domain or different protocol
            //could be mailto, etc
            isExternal: function (url) {
                var u = path.parseUrl(url);

                return !!(u.protocol &&
					(u.domain.toLowerCase() !== this.documentUrl.domain.toLowerCase()));
            },

            hasProtocol: function (url) {
                return (/^(:?\w+:)/).test(url);
            },

            isEmbeddedPage: function (url) {
                var u = path.parseUrl(url);

                //if the path is absolute, then we need to compare the url against
                //both the this.documentUrl and the documentBase. The main reason for this
                //is that links embedded within external documents will refer to the
                //application document, whereas links embedded within the application
                //document will be resolved against the document base.
                if (u.protocol !== "") {
                    return (!this.isPath(u.hash) && u.hash && (u.hrefNoHash === this.documentUrl.hrefNoHash || (this.documentBaseDiffers && u.hrefNoHash === this.documentBase.hrefNoHash)));
                }
                return (/^#/).test(u.href);
            },

            squash: function (url, resolutionUrl) {
                var href, cleanedUrl, search, stateIndex, docUrl,
					isPath = this.isPath(url),
					uri = this.parseUrl(url),
					preservedHash = uri.hash,
					uiState = "";

                // produce a url against which we can resolve the provided path
                if (!resolutionUrl) {
                    if (isPath) {
                        resolutionUrl = path.getLocation();
                    } else {
                        docUrl = path.getDocumentUrl(true);
                        if (path.isPath(docUrl.hash)) {
                            resolutionUrl = path.squash(docUrl.href);
                        } else {
                            resolutionUrl = docUrl.href;
                        }
                    }
                }

                // If the url is anything but a simple string, remove any preceding hash
                // eg #foo/bar -> foo/bar
                //    #foo -> #foo
                cleanedUrl = isPath ? path.stripHash(url) : url;

                // If the url is a full url with a hash check if the parsed hash is a path
                // if it is, strip the #, and use it otherwise continue without change
                cleanedUrl = path.isPath(uri.hash) ? path.stripHash(uri.hash) : cleanedUrl;

                // Split the UI State keys off the href
                stateIndex = cleanedUrl.indexOf(this.uiStateKey);

                // store the ui state keys for use
                if (stateIndex > -1) {
                    uiState = cleanedUrl.slice(stateIndex);
                    cleanedUrl = cleanedUrl.slice(0, stateIndex);
                }

                // make the cleanedUrl absolute relative to the resolution url
                href = path.makeUrlAbsolute(cleanedUrl, resolutionUrl);

                // grab the search from the resolved url since parsing from
                // the passed url may not yield the correct result
                search = this.parseUrl(href).search;

                // TODO all this crap is terrible, clean it up
                if (isPath) {
                    // reject the hash if it's a path or it's just a dialog key
                    if (path.isPath(preservedHash) || preservedHash.replace("#", "").indexOf(this.uiStateKey) === 0) {
                        preservedHash = "";
                    }

                    // Append the UI State keys where it exists and it's been removed
                    // from the url
                    if (uiState && preservedHash.indexOf(this.uiStateKey) === -1) {
                        preservedHash += uiState;
                    }

                    // make sure that pound is on the front of the hash
                    if (preservedHash.indexOf("#") === -1 && preservedHash !== "") {
                        preservedHash = "#" + preservedHash;
                    }

                    // reconstruct each of the pieces with the new search string and hash
                    href = path.parseUrl(href);
                    href = href.protocol + href.doubleSlash + href.host + href.pathname + search +
						preservedHash;
                } else {
                    href += href.indexOf("#") > -1 ? uiState : "#" + uiState;
                }

                return href;
            },

            isPreservableHash: function (hash) {
                return hash.replace("#", "").indexOf(this.uiStateKey) === 0;
            },

            // Escape weird characters in the hash if it is to be used as a selector
            hashToSelector: function (hash) {
                var hasHash = (hash.substring(0, 1) === "#");
                if (hasHash) {
                    hash = hash.substring(1);
                }
                return (hasHash ? "#" : "") + hash.replace(/([!"#$%&'()*+,./:;<=>?@[\]^`{|}~])/g, "\\$1");
            },

            // return the substring of a filepath before the dialogHashKey, for making a server
            // request
            getFilePath: function (path) {
                return path && path.split(dialogHashKey)[0];
            },

            // check if the specified url refers to the first page in the main
            // application document.
            isFirstPageUrl: function (url) {
                // We only deal with absolute paths.
                var u = path.parseUrl(path.makeUrlAbsolute(url, this.documentBase)),

					// Does the url have the same path as the document?
					samePath = u.hrefNoHash === this.documentUrl.hrefNoHash ||
						(this.documentBaseDiffers &&
							u.hrefNoHash === this.documentBase.hrefNoHash),

					// Get the first page element.
					fp = $.mobile.firstPage,

					// Get the id of the first page element if it has one.
					fpId = fp && fp[0] ? fp[0].id : undefined;

                // The url refers to the first page if the path matches the document and
                // it either has no hash value, or the hash is exactly equal to the id
                // of the first page element.
                return samePath &&
					(!u.hash ||
						u.hash === "#" ||
						(fpId && u.hash.replace(/^#/, "") === fpId));
            },

            // Some embedded browsers, like the web view in Phone Gap, allow
            // cross-domain XHR requests if the document doing the request was loaded
            // via the file:// protocol. This is usually to allow the application to
            // "phone home" and fetch app specific data. We normally let the browser
            // handle external/cross-domain urls, but if the allowCrossDomainPages
            // option is true, we will allow cross-domain http/https requests to go
            // through our page loading logic.
            isPermittedCrossDomainRequest: function (docUrl, reqUrl) {
                return $.mobile.allowCrossDomainPages &&
					(docUrl.protocol === "file:" || docUrl.protocol === "content:") &&
					reqUrl.search(/^https?:/) !== -1;
            }
        };

        path.documentUrl = path.parseLocation();

        $base = $("head").find("base");

        path.documentBase = $base.length ?
			path.parseUrl(path.makeUrlAbsolute($base.attr("href"), path.documentUrl.href)) :
			path.documentUrl;

        path.documentBaseDiffers = (path.documentUrl.hrefNoHash !== path.documentBase.hrefNoHash);

        //return the original document base url
        path.getDocumentBase = function (asParsedObject) {
            return asParsedObject ? $.extend({}, path.documentBase) : path.documentBase.href;
        };

        // DEPRECATED as of 1.4.0 - remove in 1.5.0
        $.extend($.mobile, {

            //return the original document url
            getDocumentUrl: path.getDocumentUrl,

            //return the original document base url
            getDocumentBase: path.getDocumentBase
        });
    })(jQuery);



    (function ($, undefined) {
        $.mobile.History = function (stack, index) {
            this.stack = stack || [];
            this.activeIndex = index || 0;
        };

        $.extend($.mobile.History.prototype, {
            getActive: function () {
                return this.stack[this.activeIndex];
            },

            getLast: function () {
                return this.stack[this.previousIndex];
            },

            getNext: function () {
                return this.stack[this.activeIndex + 1];
            },

            getPrev: function () {
                return this.stack[this.activeIndex - 1];
            },

            // addNew is used whenever a new page is added
            add: function (url, data) {
                data = data || {};

                //if there's forward history, wipe it
                if (this.getNext()) {
                    this.clearForward();
                }

                // if the hash is included in the data make sure the shape
                // is consistent for comparison
                if (data.hash && data.hash.indexOf("#") === -1) {
                    data.hash = "#" + data.hash;
                }

                data.url = url;
                this.stack.push(data);
                this.activeIndex = this.stack.length - 1;
            },

            //wipe urls ahead of active index
            clearForward: function () {
                this.stack = this.stack.slice(0, this.activeIndex + 1);
            },

            find: function (url, stack, earlyReturn) {
                stack = stack || this.stack;

                var entry, i, length = stack.length, index;

                for (i = 0; i < length; i++) {
                    entry = stack[i];

                    if (decodeURIComponent(url) === decodeURIComponent(entry.url) ||
                        decodeURIComponent(url) === decodeURIComponent(entry.hash)) {
                        index = i;

                        if (earlyReturn) {
                            return index;
                        }
                    }
                }

                return index;
            },

            closest: function (url) {
                var closest, a = this.activeIndex;

                // First, take the slice of the history stack before the current index and search
                // for a url match. If one is found, we'll avoid avoid looking through forward history
                // NOTE the preference for backward history movement is driven by the fact that
                //      most mobile browsers only have a dedicated back button, and users rarely use
                //      the forward button in desktop browser anyhow
                closest = this.find(url, this.stack.slice(0, a));

                // If nothing was found in backward history check forward. The `true`
                // value passed as the third parameter causes the find method to break
                // on the first match in the forward history slice. The starting index
                // of the slice must then be added to the result to get the element index
                // in the original history stack :( :(
                //
                // TODO this is hyper confusing and should be cleaned up (ugh so bad)
                if (closest === undefined) {
                    closest = this.find(url, this.stack.slice(a), true);
                    closest = closest === undefined ? closest : closest + a;
                }

                return closest;
            },

            direct: function (opts) {
                var newActiveIndex = this.closest(opts.url), a = this.activeIndex;

                // save new page index, null check to prevent falsey 0 result
                // record the previous index for reference
                if (newActiveIndex !== undefined) {
                    this.activeIndex = newActiveIndex;
                    this.previousIndex = a;
                }

                // invoke callbacks where appropriate
                //
                // TODO this is also convoluted and confusing
                if (newActiveIndex < a) {
                    (opts.present || opts.back || $.noop)(this.getActive(), "back");
                } else if (newActiveIndex > a) {
                    (opts.present || opts.forward || $.noop)(this.getActive(), "forward");
                } else if (newActiveIndex === undefined && opts.missing) {
                    opts.missing(this.getActive());
                }
            }
        });
    })(jQuery);



    (function ($, undefined) {
        var path = $.mobile.path,
            initialHref = location.href;

        $.mobile.Navigator = function (history) {
            this.history = history;
            this.ignoreInitialHashChange = true;

            $.mobile.window.bind({
                "popstate.history": $.proxy(this.popstate, this)
            });
        };

        $.extend($.mobile.Navigator.prototype, {
            squash: function (url, data) {
                var state, href, hash = path.isPath(url) ? path.stripHash(url) : url;

                href = path.squash(url);

                // make sure to provide this information when it isn't explicitly set in the
                // data object that was passed to the squash method
                state = $.extend({
                    hash: hash,
                    url: href
                }, data);

                // replace the current url with the new href and store the state
                // Note that in some cases we might be replacing an url with the
                // same url. We do this anyways because we need to make sure that
                // all of our history entries have a state object associated with
                // them. This allows us to work around the case where $.mobile.back()
                // is called to transition from an external page to an embedded page.
                // In that particular case, a hashchange event is *NOT* generated by the browser.
                // Ensuring each history entry has a state object means that onPopState()
                // will always trigger our hashchange callback even when a hashchange event
                // is not fired.
                window.history.replaceState(state, state.title || document.title, href);

                return state;
            },

            hash: function (url, href) {
                var parsed, loc, hash, resolved;

                // Grab the hash for recording. If the passed url is a path
                // we used the parsed version of the squashed url to reconstruct,
                // otherwise we assume it's a hash and store it directly
                parsed = path.parseUrl(url);
                loc = path.parseLocation();

                if (loc.pathname + loc.search === parsed.pathname + parsed.search) {
                    // If the pathname and search of the passed url is identical to the current loc
                    // then we must use the hash. Otherwise there will be no event
                    // eg, url = "/foo/bar?baz#bang", location.href = "http://example.com/foo/bar?baz"
                    hash = parsed.hash ? parsed.hash : parsed.pathname + parsed.search;
                } else if (path.isPath(url)) {
                    resolved = path.parseUrl(href);
                    // If the passed url is a path, make it domain relative and remove any trailing hash
                    hash = resolved.pathname + resolved.search + (path.isPreservableHash(resolved.hash) ? resolved.hash.replace("#", "") : "");
                } else {
                    hash = url;
                }

                return hash;
            },

            // TODO reconsider name
            go: function (url, data, noEvents) {
                var state, href, hash, popstateEvent;

                // Get the url as it would look squashed on to the current resolution url
                href = path.squash(url);

                // sort out what the hash sould be from the url
                hash = this.hash(url, href);

                // Here we prevent the next hash change or popstate event from doing any
                // history management. In the case of hashchange we don't swallow it
                // if there will be no hashchange fired (since that won't reset the value)
                // and will swallow the following hashchange
                if (noEvents && hash !== path.stripHash(path.parseLocation().hash)) {
                    this.preventNextHashChange = noEvents;
                }

                // IMPORTANT in the case where popstate is supported the event will be triggered
                //      directly, stopping further execution - ie, interupting the flow of this
                //      method call to fire bindings at this expression. Below the navigate method
                //      there is a binding to catch this event and stop its propagation.
                //
                //      We then trigger a new popstate event on the window with a null state
                //      so that the navigate events can conclude their work properly
                //
                // if the url is a path we want to preserve the query params that are available on
                // the current url.
                this.preventHashAssignPopState = true;
                window.location.hash = hash;

                // If popstate is enabled and the browser triggers `popstate` events when the hash
                // is set (this often happens immediately in browsers like Chrome), then the
                // this flag will be set to false already. If it's a browser that does not trigger
                // a `popstate` on hash assignement or `replaceState` then we need avoid the branch
                // that swallows the event created by the popstate generated by the hash assignment
                // At the time of this writing this happens with Opera 12 and some version of IE
                this.preventHashAssignPopState = false;

                state = $.extend({
                    url: href,
                    hash: hash,
                    title: document.title
                }, data);

                popstateEvent = new $.Event("popstate");
                popstateEvent.originalEvent = {
                    type: "popstate",
                    state: null
                };

                this.squash(url, state);

                // Trigger a new faux popstate event to replace the one that we
                // caught that was triggered by the hash setting above.
                if (!noEvents) {
                    this.ignorePopState = true;
                    $.mobile.window.trigger(popstateEvent);
                }

                // record the history entry so that the information can be included
                // in hashchange event driven navigate events in a similar fashion to
                // the state that's provided by popstate
                this.history.add(state.url, state);
            },

            // This binding is intended to catch the popstate events that are fired
            // when execution of the `$.navigate` method stops at window.location.hash = url;
            // and completely prevent them from propagating. The popstate event will then be
            // retriggered after execution resumes
            //
            // TODO grab the original event here and use it for the synthetic event in the
            //      second half of the navigate execution that will follow this binding
            popstate: function (event) {
                var hash, state;

                // If this is the popstate triggered by the actual alteration of the hash
                // prevent it completely. History is tracked manually
                if (this.preventHashAssignPopState) {
                    this.preventHashAssignPopState = false;
                    event.stopImmediatePropagation();
                    return;
                }

                // if this is the popstate triggered after the `replaceState` call in the go
                // method, then simply ignore it. The history entry has already been captured
                if (this.ignorePopState) {
                    this.ignorePopState = false;
                    return;
                }

                // If there is no state, and the history stack length is one were
                // probably getting the page load popstate fired by browsers like chrome
                // avoid it and set the one time flag to false.
                // TODO: Do we really need all these conditions? Comparing location hrefs
                // should be sufficient.
                if (!event.originalEvent.state &&
                    this.history.stack.length === 1 &&
                    this.ignoreInitialHashChange) {
                    this.ignoreInitialHashChange = false;

                    if (location.href === initialHref) {
                        event.preventDefault();
                        return;
                    }
                }

                // account for direct manipulation of the hash. That is, we will receive a popstate
                // when the hash is changed by assignment, and it won't have a state associated. We
                // then need to squash the hash. See below for handling of hash assignment that
                // matches an existing history entry
                // TODO it might be better to only add to the history stack
                //      when the hash is adjacent to the active history entry
                hash = path.parseLocation().hash;
                if (!event.originalEvent.state && hash) {
                    // squash the hash that's been assigned on the URL with replaceState
                    // also grab the resulting state object for storage
                    state = this.squash(hash);

                    // record the new hash as an additional history entry
                    // to match the browser's treatment of hash assignment
                    this.history.add(state.url, state);

                    // pass the newly created state information
                    // along with the event
                    event.historyState = state;

                    // do not alter history, we've added a new history entry
                    // so we know where we are
                    return;
                }

                // If all else fails this is a popstate that comes from the back or forward buttons
                // make sure to set the state of our history stack properly, and record the directionality
                this.history.direct({
                    url: (event.originalEvent.state || {}).url || hash,

                    // When the url is either forward or backward in history include the entry
                    // as data on the event object for merging as data in the navigate event
                    present: function (historyEntry, direction) {
                        // make sure to create a new object to pass down as the navigate event data
                        event.historyState = $.extend({}, historyEntry);
                        event.historyState.direction = direction;
                    }
                });
            }
        });
    })(jQuery);



    (function ($, undefined) {
        // TODO consider queueing navigation activity until previous activities have completed
        //      so that end users don't have to think about it. Punting for now
        // TODO !! move the event bindings into callbacks on the navigate event
        $.mobile.navigate = function (url, data, noEvents) {
            $.mobile.navigate.navigator.go(url, data, noEvents);
        };

        // expose the history on the navigate method in anticipation of full integration with
        // existing navigation functionalty that is tightly coupled to the history information
        $.mobile.navigate.history = new $.mobile.History();

        // instantiate an instance of the navigator for use within the $.navigate method
        $.mobile.navigate.navigator = new $.mobile.Navigator($.mobile.navigate.history);

        var loc = $.mobile.path.parseLocation();
        $.mobile.navigate.history.add(loc.href, { hash: loc.hash });
    })(jQuery);



    (function ($, undefined) {

        // existing base tag?
        var baseElement = document.querySelector('head base');

        if (!baseElement) {
            baseElement = $("<base>", { href: $.mobile.path.documentBase.hrefNoHash }).prependTo($("head"));
            baseElement = baseElement[0];
        }
        // base element management, defined depending on dynamic base tag support
        // TODO move to external widget
        var base = {

            // define base element, for use in routing asset urls that are referenced
            // in Ajax-requested markup
            element: baseElement,

            linkSelector: "*[src], link[href], a[rel='external'], *[data-ajax='false'], a[target]",

            // set the generated BASE element's href to a new page's base path
            set: function (href) {

                // we should do nothing if the user wants to manage their url base
                // manually
                if (!$.mobile.dynamicBaseEnabled) {
                    return;
                }

                // we should use the base tag if we can manipulate it dynamically
                if ($.support.dynamicBaseTag) {
                    base.element.setAttribute("href",
                        $.mobile.path.makeUrlAbsolute(href, $.mobile.path.documentBase));
                }
            },

            rewrite: function (href, page) {
            },

            // set the generated BASE element's href to a new page's base path
            reset: function (/* href */) {
                base.element.setAttribute("href", $.mobile.path.documentBase.hrefNoSearch);
            }
        };

        $.mobile.base = base;

    })(jQuery);

    (function ($, undefined) {

        var pageCache = {};

        $.widget("mobile.pagecontainer", {
            options: {
                theme: "a"
            },

            initSelector: false,

            _create: function () {

                // TODO consider moving the navigation handler OUT of widget into
                //      some other object as glue between the navigate event and the
                //      content widget load and change methods
                this._on(this.window, { navigate: "_filterNavigateEvents" });
            },

            _setOptions: function (options) {
                this._super(options);
            },

            _filterNavigateEvents: function (e, data) {
                var url;

                if (e.originalEvent && e.originalEvent.isDefaultPrevented()) {
                    return;
                }

                url = e.originalEvent.type.indexOf("hashchange") > -1 ? data.state.hash : data.state.url;

                if (!url) {
                    url = this._getHash();
                }

                if (!url || url === "#" || url.indexOf("#" + $.mobile.path.uiStateKey) === 0) {
                    url = location.href;
                }

                this._handleNavigate(url, data.state);
            },

            _getHash: function () {
                return $.mobile.path.parseLocation().hash;
            },

            // TODO active page should be managed by the container (ie, it should be a property)
            getActivePage: function () {
                return this.activePage;
            },

            // TODO the first page should be a property set during _create using the logic
            //      that currently resides in init
            _getInitialContent: function () {
                return $.mobile.firstPage;
            },

            // TODO each content container should have a history object
            _getHistory: function () {
                return $.mobile.navigate.history;
            },

            _getActiveHistory: function () {
                return this._getHistory().getActive();
            },

            // TODO the document base should be determined at creation
            _getDocumentBase: function () {
                return $.mobile.path.documentBase;
            },

            back: function () {
                this.go(-1);
            },

            forward: function () {
                this.go(1);
            },

            go: function (steps) {

                window.history.go(steps);
            },

            // TODO rename _handleDestination
            _handleDestination: function (to) {
                var history;

                // clean the hash for comparison if it's a url
                if ($.type(to) === "string") {
                    to = $.mobile.path.stripHash(to);
                }

                if (to) {
                    history = this._getHistory();

                    // At this point, 'to' can be one of 3 things, a cached page
                    // element from a history stack entry, an id, or site-relative /
                    // absolute URL. If 'to' is an id, we need to resolve it against
                    // the documentBase, not the location.href, since the hashchange
                    // could've been the result of a forward/backward navigation
                    // that crosses from an external page/dialog to an internal
                    // page/dialog.
                    //
                    // TODO move check to history object or path object?
                    to = !$.mobile.path.isPath(to) ? ($.mobile.path.makeUrlAbsolute("#" + to, this._getDocumentBase())) : to;
                }
                return to || this._getInitialContent();
            },

            _transitionFromHistory: function (direction, defaultTransition) {
                var history = this._getHistory(),
                    entry = (direction === "back" ? history.getLast() : history.getActive());

                return (entry && entry.transition) || defaultTransition;
            },

            _handleDialog: function (changePageOptions, data) {
                var to, active, activeContent = this.getActivePage();

                // If current active page is not a dialog skip the dialog and continue
                // in the same direction
                // Note: The dialog widget is deprecated as of 1.4.0 and will be removed in 1.5.0.
                // Thus, as of 1.5.0 activeContent.data( "mobile-dialog" ) will always evaluate to
                // falsy, so the second condition in the if-statement below can be removed altogether.
                if (activeContent && !activeContent.data("mobile-dialog")) {
                    // determine if we're heading forward or backward and continue
                    // accordingly past the current dialog
                    if (data.direction === "back") {
                        this.back();
                    } else {
                        this.forward();
                    }

                    // prevent changePage call
                    return false;
                } else {
                    // if the current active page is a dialog and we're navigating
                    // to a dialog use the dialog objected saved in the stack
                    to = data.pageUrl;
                    active = this._getActiveHistory();

                    // make sure to set the role, transition and reversal
                    // as most of this is lost by the domCache cleaning
                    $.extend(changePageOptions, {
                        role: active.role,
                        transition: this._transitionFromHistory(
                            data.direction,
                            changePageOptions.transition),
                        reverse: data.direction === "back"
                    });
                }

                return to;
            },

            _handleNavigate: function (url, data) {
                //find first page via hash
                // TODO stripping the hash twice with handleUrl
                var to = $.mobile.path.stripHash(url), history = this._getHistory(),

                    // transition is false if it's the first page, undefined
                    // otherwise (and may be overridden by default)
                    transition = history.stack.length === 0 ? "none" :
                        this._transitionFromHistory(data.direction),

                    // default options for the changPage calls made after examining
                    // the current state of the page and the hash, NOTE that the
                    // transition is derived from the previous history entry
                    changePageOptions = {
                        changeHash: false,
                        fromHashChange: true,
                        reverse: data.direction === "back"
                    };

                $.extend(changePageOptions, data, {
                    transition: transition
                });

                // TODO move to _handleDestination ?
                // If this isn't the first page, if the current url is a dialog hash
                // key, and the initial destination isn't equal to the current target
                // page, use the special dialog handling
                if (history.activeIndex > 0 &&
                    to.indexOf($.mobile.dialogHashKey) > -1) {

                    to = this._handleDialog(changePageOptions, data);

                    if (to === false) {
                        return;
                    }
                }

                this._changeContent(this._handleDestination(to), changePageOptions);
            },

            _changeContent: function (to, opts) {
                $.mobile.changePage(to, opts);
            },

            _getBase: function () {
                return $.mobile.base;
            },

            _getNs: function () {
                return $.mobile.ns;
            },

            _enhance: function (content, role) {
                // TODO consider supporting a custom callback, and passing in
                // the settings which includes the role
                return content.page({ role: role });
            },

            _include: function (page, jPage, settings) {

                // append to page and enhance
                jPage.appendTo(this.element);
                //alert(jPage[0].parentNode == this.element[0]);
                //this.element[0].appendChild(page);

                // use the page widget to enhance
                this._enhance(jPage, settings.role);
            },

            _find: function (absUrl) {
                // TODO consider supporting a custom callback
                var fileUrl = this._createFileUrl(absUrl),
                    dataUrl = this._createDataUrl(absUrl),
                    page, initialContent = this._getInitialContent();

                // Check to see if the page already exists in the DOM.
                // NOTE do _not_ use the :jqmData pseudo selector because parenthesis
                //      are a valid url char and it breaks on the first occurence
                page = this.element
                    .children("[data-" + this._getNs() +
                        "url='" + $.mobile.path.hashToSelector(dataUrl) + "']");

                // If we failed to find the page, check to see if the url is a
                // reference to an embedded page. If so, it may have been dynamically
                // injected by a developer, in which case it would be lacking a
                // data-url attribute and in need of enhancement.
                if (page.length === 0 && dataUrl && !$.mobile.path.isPath(dataUrl)) {
                    page = this.element.children($.mobile.path.hashToSelector("#" + dataUrl))
                        .attr("data-" + this._getNs() + "url", dataUrl)
                        .jqmData("url", dataUrl);
                }

                // If we failed to find a page in the DOM, check the URL to see if it
                // refers to the first page in the application. Also check to make sure
                // our cached-first-page is actually in the DOM. Some user deployed
                // apps are pruning the first page from the DOM for various reasons.
                // We check for this case here because we don't want a first-page with
                // an id falling through to the non-existent embedded page error case.
                if (page.length === 0 &&
                    $.mobile.path.isFirstPageUrl(fileUrl) &&
                    initialContent &&
                    initialContent.parent().length) {
                    page = $(initialContent);
                }

                return page;
            },

            _parse: function (html, fileUrl) {
                // TODO consider allowing customization of this method. It's very JQM specific
                var page, all = document.createElement('div');

                //workaround to allow scripts to execute when included in page divs
                all.innerHTML = html;

                page = all.querySelector("div[data-role='page']");

                //if page elem couldn't be found, create one and insert the body element's contents
                if (!page) {
                    page = $("<div data-" + this._getNs() + "role='page'>" +
                        (html.split(/<\/?body[^>]*>/gmi)[1] || "") +
                        "</div>")[0];
                }

                // TODO tagging a page with external to make sure that embedded pages aren't
                // removed by the various page handling code is bad. Having page handling code
                // in many places is bad. Solutions post 1.0
                page.setAttribute("data-" + this._getNs() + "url", this._createDataUrl(fileUrl));
                page.setAttribute("data-" + this._getNs() + "external-page", true);

                return page;
            },

            _setLoadedTitle: function (page, html) {
                //page title regexp
                if (!page.jqmData("title")) {

                    var newPageTitle = html.match(/<title[^>]*>([^<]*)/) && RegExp.$1;

                    if (newPageTitle) {
                        page.jqmData("title", newPageTitle);
                    }
                }
            },

            _isRewritableBaseTag: function () {
                return $.mobile.dynamicBaseEnabled && !$.support.dynamicBaseTag;
            },

            _createDataUrl: function (absoluteUrl) {
                return $.mobile.path.convertUrlToDataUrl(absoluteUrl);
            },

            _createFileUrl: function (absoluteUrl) {
                return $.mobile.path.getFilePath(absoluteUrl);
            },

            _triggerWithDeprecated: function (name, data, page) {
                var deprecatedEvent = $.Event("page" + name),
                    newEvent = $.Event(this.widgetName + name);

                // DEPRECATED
                // trigger the old deprecated event on the page if it's provided
                (page || this.element).trigger(deprecatedEvent, data);

                // use the widget trigger method for the new content* event
                this._trigger(name, newEvent, data);

                return {
                    deprecatedEvent: deprecatedEvent,
                    event: newEvent
                };
            },

            // TODO it would be nice to split this up more but everything appears to be "one off"
            //      or require ordering such that other bits are sprinkled in between parts that
            //      could be abstracted out as a group
            _loadSuccess: function (absUrl, triggerData, settings, deferred) {
                var fileUrl = this._createFileUrl(absUrl);

                return $.proxy(function (html, wasCached) {

                    if (!wasCached || typeof (wasCached) != 'boolean') {
                        if ($.mobile.filterHtml) {
                            html = $.mobile.filterHtml(html);
                        }
                        if (absUrl.toLowerCase().indexOf('/configurationpage?') == -1) {
                            pageCache[absUrl.split('?')[0]] = html;
                        }
                    }

                    //dont update the base tag if we are prefetching
                    if (settings.prefetch === undefined) {
                        this._getBase().set(fileUrl);
                    }

                    var contentElem = this._parse(html, fileUrl);
                    var content = $(contentElem);

                    this._setLoadedTitle(content, html);

                    // DEPRECATED
                    triggerData.page = content;

                    triggerData.content = content;

                    triggerData.toPage = content;

                    // rewrite src and href attrs to use a base url if the base tag won't work
                    if (this._isRewritableBaseTag() && content) {
                        this._getBase().rewrite(fileUrl, content);
                    }

                    var dependencies = contentElem.getAttribute('data-require');
                    dependencies = dependencies ? dependencies.split(',') : null;

                    if (contentElem.classList.contains('type-interior')) {
                        dependencies = dependencies || [];
                        dependencies.push('jqmpopup');
                        dependencies.push('jqmlistview');
                        dependencies.push('jqmcollapsible');
                        dependencies.push('jqmcontrolgroup');
                        dependencies.push('jqmcheckbox');
                    }

                    var currentSelf = this;

                    require(dependencies, function () {
                        currentSelf._include(contentElem, content, settings);
                        deferred.resolve(absUrl, settings, content);
                    });

                }, this);
            },

            _loadDefaults: {
                type: "get",
                data: undefined,

                // DEPRECATED
                reloadPage: false,

                reload: false,

                // By default we rely on the role defined by the @data-role attribute.
                role: undefined,

                showLoadMsg: false,

                // This delay allows loads that pull from browser cache to
                // occur without showing the loading message.
                loadMsgDelay: 50
            },

            load: function (url, options) {
                // This function uses deferred notifications to let callers
                // know when the content is done loading, or if an error has occurred.
                var deferred = (options && options.deferred) || $.Deferred(),

                    // Examining the option "reloadPage" passed by the user is deprecated as of 1.4.0
                    // and will be removed in 1.5.0.
                    // Copy option "reloadPage" to "reload", but only if option "reload" is not present
                    reloadOptionExtension =
                        ((options && options.reload === undefined &&
                            options.reloadPage !== undefined) ?
							{ reload: options.reloadPage } : {}),

                    // The default load options with overrides specified by the caller.
                    settings = $.extend({}, this._loadDefaults, options, reloadOptionExtension),

                    // The DOM element for the content after it has been loaded.
                    content = null,

                    // The absolute version of the URL passed into the function. This
                    // version of the URL may contain dialog/subcontent params in it.
                    absUrl = $.mobile.path.makeUrlAbsolute(url, this._findBaseWithDefault()),
                    fileUrl, dataUrl, pblEvent, triggerData;

                // If the caller provided data, and we're using "get" request,
                // append the data to the URL.
                if (settings.data && settings.type === "get") {
                    absUrl = $.mobile.path.addSearchParams(absUrl, settings.data);
                    settings.data = undefined;
                }

                // If the caller is using a "post" request, reload must be true
                if (settings.data && settings.type === "post") {
                    settings.reload = true;
                }

                // The absolute version of the URL minus any dialog/subcontent params.
                // In otherwords the real URL of the content to be loaded.
                fileUrl = this._createFileUrl(absUrl);

                // The version of the Url actually stored in the data-url attribute of
                // the content. For embedded content, it is just the id of the page. For
                // content within the same domain as the document base, it is the site
                // relative path. For cross-domain content (Phone Gap only) the entire
                // absolute Url is used to load the content.
                dataUrl = this._createDataUrl(absUrl);

                content = this._find(absUrl);

                // If it isn't a reference to the first content and refers to missing
                // embedded content reject the deferred and return
                if (content.length === 0 &&
                    $.mobile.path.isEmbeddedPage(fileUrl) &&
                    !$.mobile.path.isFirstPageUrl(fileUrl)) {
                    deferred.reject(absUrl, settings);
                    return deferred.promise();
                }

                // Reset base to the default document base
                // TODO figure out why we doe this
                this._getBase().reset();

                // If the content we are interested in is already in the DOM,
                // and the caller did not indicate that we should force a
                // reload of the file, we are done. Resolve the deferrred so that
                // users can bind to .done on the promise
                if (content.length && !settings.reload) {
                    this._enhance(content, settings.role);
                    deferred.resolve(absUrl, settings, content);

                    //if we are reloading the content make sure we update
                    // the base if its not a prefetch
                    if (!settings.prefetch) {
                        this._getBase().set(url);
                    }

                    return deferred.promise();
                }

                triggerData = {
                    url: url,
                    absUrl: absUrl,
                    toPage: url,
                    prevPage: options ? options.fromPage : undefined,
                    dataUrl: dataUrl,
                    deferred: deferred,
                    options: settings
                };

                // Reset base to the default document base.
                // only reset if we are not prefetching
                if (settings.prefetch === undefined) {
                    this._getBase().reset();
                }

                if (!($.mobile.allowCrossDomainPages ||
                    $.mobile.path.isSameDomain($.mobile.path.documentUrl, absUrl))) {
                    deferred.reject(absUrl, settings);
                    return deferred.promise();
                }

                var successFn = this._loadSuccess(absUrl, triggerData, settings, deferred);
                var cachedResult = pageCache[absUrl.split('?')[0]];
                if (cachedResult) {
                    successFn(cachedResult, true);
                    return deferred.promise();
                }

                // Load the new content.
                $.ajax({
                    url: fileUrl,
                    type: settings.type,
                    data: settings.data,
                    contentType: settings.contentType,
                    dataType: "html",
                    success: successFn,
                    error: this._loadError(absUrl, triggerData, settings, deferred)
                });

                return deferred.promise();
            },

            _loadError: function (absUrl, triggerData, settings, deferred) {
                return $.proxy(function (xhr, textStatus, errorThrown) {
                    //set base back to current path
                    this._getBase().set($.mobile.path.get());

                    // Add error info to our triggerData.
                    triggerData.xhr = xhr;
                    triggerData.textStatus = textStatus;
                    triggerData.errorThrown = errorThrown;

                    // Let listeners know the page load failed.
                    var plfEvent = this._triggerWithDeprecated("loadfailed", triggerData);

                    // If the default behavior is prevented, stop here!
                    // Note that it is the responsibility of the listener/handler
                    // that called preventDefault(), to resolve/reject the
                    // deferred object within the triggerData.
                    if (plfEvent.deprecatedEvent.isDefaultPrevented() ||
                        plfEvent.event.isDefaultPrevented()) {
                        return;
                    }

                    deferred.reject(absUrl, settings);
                }, this);
            },

            // TODO move into transition handlers?
            _triggerCssTransitionEvents: function (to, from, prefix) {
                var samePage = false;

                prefix = prefix || "";

                // TODO decide if these events should in fact be triggered on the container
                if (from) {

                    //Check if this is a same page transition and tell the handler in page
                    if (to[0] === from[0]) {
                        samePage = true;
                    }

                    //trigger before show/hide events
                    // TODO deprecate nextPage in favor of next
                    this._triggerWithDeprecated(prefix + "hide", {

                        // Deprecated in 1.4 remove in 1.5
                        nextPage: to,
                        toPage: to,
                        prevPage: from,
                        samePage: samePage
                    }, from);
                }

                // TODO deprecate prevPage in favor of previous
                if (!prefix && $.browser.msie) {

                    // Add a delay for IE because it seems to be having issues with web components
                    var curr = this;
                    setTimeout(function () {
                        curr._triggerWithDeprecated(prefix + "show", {
                            prevPage: from || $(""),
                            toPage: to
                        }, to);
                    }, 50);

                } else {
                    this._triggerWithDeprecated(prefix + "show", {
                        prevPage: from || $(""),
                        toPage: to
                    }, to);
                }
            },

            // TODO make private once change has been defined in the widget
            _cssTransition: function (to, from, options) {

                this._triggerCssTransitionEvents(to, from, "before");

                if (from) {
                    from[0].style.display = 'none';
                    var pages = this.element[0].childNodes;
                    for (var i = 0, length = pages.length; i < length; i++) {
                        var pg = pages[i];
                        if (pg.getAttribute && pg.getAttribute('data-role') == 'page') {
                            pg.style.display = 'none';
                        }
                    }
                }

                var toPage = to[0];

                toPage.style.display = 'block';

                //var transition = toPage.getAttribute('data-transition');
                //if (transition == 'zoom') {
                //    zoomIn(toPage);
                //}

                this._triggerCssTransitionEvents(to, from);
            },

            _removeActiveLinkClass: function (force) {
                //clear out the active button state
                $.mobile.removeActiveLinkClass(force);
            },

            _loadUrl: function (to, triggerData, settings) {
                // preserve the original target as the dataUrl value will be
                // simplified eg, removing ui-state, and removing query params
                // from the hash this is so that users who want to use query
                // params have access to them in the event bindings for the page
                // life cycle See issue #5085
                settings.target = to;
                settings.deferred = $.Deferred();

                this.load(to, settings);

                settings.deferred.done($.proxy(function (url, options, content) {

                    // store the original absolute url so that it can be provided
                    // to events in the triggerData of the subsequent changePage call
                    options.absUrl = triggerData.absUrl;

                    this.transition(content, triggerData, options);
                }, this));

                settings.deferred.fail($.proxy(function (/* url, options */) {
                    this._removeActiveLinkClass(true);
                    this._triggerWithDeprecated("changefailed", triggerData);
                }, this));
            },

            change: function (to, options) {

                var settings = $.extend({}, $.mobile.changePage.defaults, options),
                    triggerData = {};

                // Make sure we have a fromPage.
                settings.fromPage = settings.fromPage || this.activePage;

                triggerData.prevPage = this.activePage;
                $.extend(triggerData, {
                    toPage: to,
                    options: settings
                });

                // If the caller passed us a url, call loadPage()
                // to make sure it is loaded into the DOM. We'll listen
                // to the promise object it returns so we know when
                // it is done loading or if an error ocurred.
                if ($.type(to) === "string") {
                    // if the toPage is a string simply convert it
                    triggerData.absUrl = $.mobile.path.makeUrlAbsolute(to, this._findBaseWithDefault());

                    this._loadUrl(to, triggerData, settings);
                } else {
                    // if the toPage is a jQuery object grab the absolute url stored
                    // in the loadPage callback where it exists
                    triggerData.absUrl = settings.absUrl;

                    this.transition(to, triggerData, settings);
                }
            },

            transition: function (toPage, triggerData, settings) {
                var fromPage, url, pageUrl, fileUrl,
                    active, activeIsInitialPage,
                    historyDir, pageTitle, isDialog,
                    alreadyThere, newPageTitle,
                    params;

                triggerData.prevPage = settings.fromPage;

                // If we are going to the first-page of the application, we need to make
                // sure settings.dataUrl is set to the application document url. This allows
                // us to avoid generating a document url with an id hash in the case where the
                // first-page of the document has an id attribute specified.
                if (toPage[0] === $.mobile.firstPage[0] && !settings.dataUrl) {
                    settings.dataUrl = $.mobile.path.documentUrl.hrefNoHash;
                }

                // The caller passed us a real page DOM element. Update our
                // internal state and then trigger a transition to the page.
                fromPage = settings.fromPage;
                url = (settings.dataUrl && $.mobile.path.convertUrlToDataUrl(settings.dataUrl)) ||
                    toPage.jqmData("url");

                // The pageUrl var is usually the same as url, except when url is obscured
                // as a dialog url. pageUrl always contains the file path
                pageUrl = url;
                fileUrl = $.mobile.path.getFilePath(url);
                active = $.mobile.navigate.history.getActive();
                activeIsInitialPage = $.mobile.navigate.history.activeIndex === 0;
                historyDir = 0;
                pageTitle = document.title;
                isDialog = (settings.role === "dialog" ||
                    toPage.jqmData("role") === "dialog") &&
                    toPage.jqmData("dialog") !== true;

                // By default, we prevent changePage requests when the fromPage and toPage
                // are the same element, but folks that generate content
                // manually/dynamically and reuse pages want to be able to transition to
                // the same page. To allow this, they will need to change the default
                // value of allowSamePageTransition to true, *OR*, pass it in as an
                // option when they manually call changePage(). It should be noted that
                // our default transition animations assume that the formPage and toPage
                // are different elements, so they may behave unexpectedly. It is up to
                // the developer that turns on the allowSamePageTransitiona option to
                // either turn off transition animations, or make sure that an appropriate
                // animation transition is used.
                if (fromPage && fromPage[0] === toPage[0]) {

                    this._triggerWithDeprecated("transition", triggerData);
                    this._triggerWithDeprecated("change", triggerData);

                    // Even if there is no page change to be done, we should keep the
                    // urlHistory in sync with the hash changes
                    if (settings.fromHashChange) {
                        $.mobile.navigate.history.direct({ url: url });
                    }

                    return;
                }

                // We need to make sure the page we are given has already been enhanced.
                toPage.page({ role: settings.role });

                // If the changePage request was sent from a hashChange event, check to
                // see if the page is already within the urlHistory stack. If so, we'll
                // assume the user hit the forward/back button and will try to match the
                // transition accordingly.
                if (settings.fromHashChange) {
                    historyDir = settings.direction === "back" ? -1 : 1;
                }

                // Record whether we are at a place in history where a dialog used to be -
                // if so, do not add a new history entry and do not change the hash either
                alreadyThere = false;

                // If we're displaying the page as a dialog, we don't want the url
                // for the dialog content to be used in the hash. Instead, we want
                // to append the dialogHashKey to the url of the current page.
                if (isDialog && active) {
                    // on the initial page load active.url is undefined and in that case
                    // should be an empty string. Moving the undefined -> empty string back
                    // into urlHistory.addNew seemed imprudent given undefined better
                    // represents the url state

                    // If we are at a place in history that once belonged to a dialog, reuse
                    // this state without adding to urlHistory and without modifying the
                    // hash. However, if a dialog is already displayed at this point, and
                    // we're about to display another dialog, then we must add another hash
                    // and history entry on top so that one may navigate back to the
                    // original dialog
                    if (active.url &&
                        active.url.indexOf($.mobile.dialogHashKey) > -1 &&
                        this.activePage &&
                        !this.activePage.hasClass("ui-dialog") &&
                        $.mobile.navigate.history.activeIndex > 0) {

                        settings.changeHash = false;
                        alreadyThere = true;
                    }

                    // Normally, we tack on a dialog hash key, but if this is the location
                    // of a stale dialog, we reuse the URL from the entry
                    url = (active.url || "");

                    // account for absolute urls instead of just relative urls use as hashes
                    if (!alreadyThere && url.indexOf("#") > -1) {
                        url += $.mobile.dialogHashKey;
                    } else {
                        url += "#" + $.mobile.dialogHashKey;
                    }
                }

                // if title element wasn't found, try the page div data attr too
                // If this is a deep-link or a reload ( active === undefined ) then just
                // use pageTitle
                newPageTitle = (!active) ? pageTitle : toPage.jqmData("title");
                if (!!newPageTitle && pageTitle === document.title) {
                    pageTitle = newPageTitle;
                }
                if (!toPage.jqmData("title")) {
                    toPage.jqmData("title", pageTitle);
                }

                //add page to history stack if it's not back or forward
                if (!historyDir && alreadyThere) {
                    $.mobile.navigate.history.getActive().pageUrl = pageUrl;
                }

                // Set the location hash.
                if (url && !settings.fromHashChange) {

                    // rebuilding the hash here since we loose it earlier on
                    // TODO preserve the originally passed in path
                    if (!$.mobile.path.isPath(url) && url.indexOf("#") < 0) {
                        url = "#" + url;
                    }

                    // TODO the property names here are just silly
                    params = {
                        title: pageTitle,
                        pageUrl: pageUrl,
                        role: settings.role
                    };

                    if (settings.changeHash !== false) {
                        $.mobile.navigate(this.window[0].encodeURI(url), params, true);
                    } else if (toPage[0] !== $.mobile.firstPage[0]) {
                        $.mobile.navigate.history.add(url, params);
                    }
                }

                //set page title
                document.title = pageTitle;

                //set "toPage" as activePage deprecated in 1.4 remove in 1.5
                $.mobile.activePage = toPage;

                //new way to handle activePage
                this.activePage = toPage;

                // If we're navigating back in the URL history, set reverse accordingly.
                settings.reverse = settings.reverse || historyDir < 0;

                this._cssTransition(toPage, fromPage, {
                    transition: settings.transition,
                    reverse: settings.reverse
                });

                $.mobile.removeActiveLinkClass();
            },

            // determine the current base url
            _findBaseWithDefault: function () {

                var closestBase = (this.activePage &&
                $.mobile.getClosestBaseUrl(this.activePage[0]));
                return closestBase || $.mobile.path.documentBase.hrefNoHash;
            }
        });

        function zoomIn(elem) {
            var keyframes = [{ transform: 'scale3d(.3, .3, .3)  ', opacity: '0', offset: 0 },
              { transform: 'none', opacity: '1', offset: 1 }];
            var timing = { duration: 200, iterations: 1 };
            return elem.animate(keyframes, timing);
        }

        // The following handlers should be bound after mobileinit has been triggered
        // the following deferred is resolved in the init file
        $.mobile.navreadyDeferred = $.Deferred();

    })(jQuery);

    (function ($, undefined) {

        // resolved on domready
        var domreadyDeferred = $.Deferred(),

            // resolved and nulled on window.load()
            loadDeferred = $.Deferred(),

            // function that resolves the above deferred
            pageIsFullyLoaded = function () {

                // Resolve and null the deferred
                loadDeferred.resolve();
                loadDeferred = null;
            },

            documentUrl = $.mobile.path.documentUrl;

        $.mobile.loadPage = function (url, opts) {
            var container;

            opts = opts || {};
            container = (opts.pageContainer || $.mobile.pageContainer);

            // create the deferred that will be supplied to loadPage callers
            // and resolved by the content widget's load method
            opts.deferred = $.Deferred();

            // Preferring to allow exceptions for uninitialized opts.pageContainer
            // widgets so we know if we need to force init here for users
            container.pagecontainer("load", url, opts);

            // provide the deferred
            return opts.deferred.promise();
        };

        //define vars for interal use

        /* internal utility functions */

        // NOTE Issue #4950 Android phonegap doesn't navigate back properly
        //      when a full page refresh has taken place. It appears that hashchange
        //      and replacestate history alterations work fine but we need to support
        //      both forms of history traversal in our code that uses backward history
        //      movement
        $.mobile.back = function () {
            var nav = window.navigator;

            // if the setting is on and the navigator object is
            // available use the phonegap navigation capability
            if (this.phonegapNavigationEnabled &&
                nav &&
                nav.app &&
                nav.app.backHistory) {
                nav.app.backHistory();
            } else {
                $.mobile.pageContainer.pagecontainer("back");
            }
        };

        // Direct focus to the page title, or otherwise first focusable element
        $.mobile.focusPage = function (page) {
        };

        // Exposed $.mobile methods

        $.mobile.changePage = function (to, options) {
            $.mobile.pageContainer.pagecontainer("change", to, options);
        };

        $.mobile.changePage.defaults = {
            reverse: false,
            changeHash: true,
            fromHashChange: false,
            role: undefined, // By default we rely on the role defined by the @data-role attribute.
            duplicateCachedPage: undefined,
            pageContainer: undefined,
            showLoadMsg: true, //loading message shows by default when pages are being fetched during changePage
            dataUrl: undefined,
            fromPage: undefined
        };

        function parentWithTag(elem, tagName) {

            while (elem.tagName != tagName) {
                elem = elem.parentNode;

                if (!elem) {
                    return null;
                }
            }

            return elem;
        }

        $.mobile._registerInternalEvents = function () {

            // click routing - direct to HTTP or Ajax, accordingly
            $.mobile.document.bind("click", function (event) {
                if (!$.mobile.linkBindingEnabled || event.isDefaultPrevented()) {
                    return;
                }

                var link = parentWithTag(event.target, 'A');

                var $link = $(link),

                    baseUrl, href,
                    useDefaultUrlHandling, isExternal,
                    reverse, role;

                // If there is no link associated with the click or its not a left
                // click we want to ignore the click
                // TODO teach $.mobile.hijackable to operate on raw dom elements so the link wrapping
                // can be avoided
                if (!link || event.which > 1) {
                    return;
                }

                //if there's a data-rel=back attr, go back in history
                if (link.getAttribute('data-rel') == 'back') {
                    $.mobile.back();
                    return false;
                }

                baseUrl = $.mobile.getClosestBaseUrl(link);

                //get href, if defined, otherwise default to empty hash
                href = $.mobile.path.makeUrlAbsolute(link.getAttribute("href") || "#", baseUrl);

                // XXX_jblas: Ideally links to application pages should be specified as
                //            an url to the application document with a hash that is either
                //            the site relative path or id to the page. But some of the
                //            internal code that dynamically generates sub-pages for nested
                //            lists and select dialogs, just write a hash in the link they
                //            create. This means the actual URL path is based on whatever
                //            the current value of the base tag is at the time this code
                //            is called.
                if (href.search("#") !== -1 &&
                    !($.mobile.path.isExternal(href) && $.mobile.path.isAbsoluteUrl(href))) {

                    href = href.replace(/[^#]*#/, "");
                    if (!href) {
                        //link was an empty hash meant purely
                        //for interaction, so we ignore it.
                        event.preventDefault();
                        return;
                    } else if ($.mobile.path.isPath(href)) {
                        //we have apath so make it the href we want to load.
                        href = $.mobile.path.makeUrlAbsolute(href, baseUrl);
                    } else {
                        //we have a simple id so use the documentUrl as its base.
                        href = $.mobile.path.makeUrlAbsolute("#" + href, documentUrl.hrefNoHash);
                    }
                }

                // Should we handle this link, or let the browser deal with it?
                useDefaultUrlHandling = link.getAttribute("rel") == "external" || link.getAttribute("data-ajax") == "false" || link.getAttribute('target');

                // Some embedded browsers, like the web view in Phone Gap, allow cross-domain XHR
                // requests if the document doing the request was loaded via the file:// protocol.
                // This is usually to allow the application to "phone home" and fetch app specific
                // data. We normally let the browser handle external/cross-domain urls, but if the
                // allowCrossDomainPages option is true, we will allow cross-domain http/https
                // requests to go through our page loading logic.

                //check for protocol or rel and its not an embedded page
                //TODO overlap in logic from isExternal, rel=external check should be
                //     moved into more comprehensive isExternalLink
                isExternal = useDefaultUrlHandling || ($.mobile.path.isExternal(href) && !$.mobile.path.isPermittedCrossDomainRequest(documentUrl, href));

                if (isExternal) {
                    //use default click handling
                    return;
                }

                //use ajax
                reverse = $link.jqmData("direction") === "reverse" ||
                            // deprecated - remove by 1.0
                            $link.jqmData("back");

                //this may need to be more specific as we use data-rel more
                role = link.getAttribute("data-" + $.mobile.ns + "rel") || undefined;

                $.mobile.changePage(href, { reverse: reverse, role: role, link: $link });
                event.preventDefault();
            });

            // TODO ensure that the navigate binding in the content widget happens at the right time
            $.mobile.pageContainer.pagecontainer();

            function removePage(page) {

                page.parentNode.removeChild(page);
            }

            function cleanPages(newPage) {

                var pages = document.querySelectorAll("div[data-role='page']");
                if (pages.length < 5) {
                    //return;
                }

                for (var i = 0, length = pages.length; i < length; i++) {
                    var page = pages[i];
                    if (page != newPage) {
                        removePage(page);
                    }
                }
            }

            //set page min-heights to be device specific
            $.mobile.document.bind("pagehide", function (e, data) {

                var toPage = data.toPage ? data.toPage[0] : null;

                if (toPage) {
                    if (toPage.getAttribute('data-dom-cache')) {
                        cleanPages(toPage);
                    }
                    else if ((toPage.getAttribute('data-url') || '').toLowerCase().indexOf('/configurationpage') != -1) {
                        // plugin config pages are not built to handle the dom caching
                        cleanPages(toPage);
                    }
                }
            });

        };//navreadyDeferred done callback

        $(function () { domreadyDeferred.resolve(); });

        // Account for the possibility that the load event has already fired
        if (document.readyState === "complete") {
            pageIsFullyLoaded();
        } else {
            $.mobile.window.load(pageIsFullyLoaded);
        }

        $.when(domreadyDeferred, $.mobile.navreadyDeferred).done(function () { $.mobile._registerInternalEvents(); });
    })(jQuery);


    (function ($, window, undefined) {
        var $window = $.mobile.window;

        // support conditions
        // if device support condition(s) aren't met, leave things as they are -> a basic, usable experience,
        // otherwise, proceed with the enhancements
        if (!$.mobile.gradeA()) {
            return;
        }

        // override ajaxEnabled on platforms that have known conflicts with hash history updates
        // or generally work better browsing in regular http for full page refreshes (BB5, Opera Mini)
        if ($.mobile.ajaxBlacklist) {
            $.mobile.ajaxEnabled = false;
        }

        $.extend($.mobile, {
            // find and enhance the pages in the dom and transition to the first page.
            initializePage: function () {
                // find present pages
                var path = $.mobile.path,
                    $pages = $(document.querySelectorAll("div[data-role='page']")),
                    hash = path.stripHash(path.stripQueryParams(path.parseLocation().hash)),
                    theLocation = $.mobile.path.parseLocation(),
                    hashPage = hash ? document.getElementById(hash) : undefined;

                // add dialogs, set data-url attrs
                $pages.each(function () {
                    var $this = $(this);

                    // unless the data url is already set set it to the pathname
                    if (!$this[0].getAttribute("data-" + $.mobile.ns + "url")) {
                        $this.attr("data-" + $.mobile.ns + "url", $this.attr("id") ||
                            path.convertUrlToDataUrl(theLocation.pathname + theLocation.search));
                    }
                });

                // define first page in dom case one backs out to the directory root (not always the first page visited, but defined as fallback)
                $.mobile.firstPage = $pages.first();

                // define page container
                $.mobile.pageContainer = $.mobile.firstPage
                    .parent()
                    .addClass("ui-mobile-viewport")
                    .pagecontainer();

                // initialize navigation events now, after mobileinit has occurred and the page container
                // has been created but before the rest of the library is alerted to that fact
                $.mobile.navreadyDeferred.resolve();

                // if hashchange listening is disabled, there's no hash deeplink,
                // the hash is not valid (contains more than one # or does not start with #)
                // or there is no page with that hash, change to the first page in the DOM
                // Remember, however, that the hash can also be a path!
                if (!($.mobile.path.isHashValid(location.hash) &&
                    ($(hashPage).is("[data-role='page']") ||
                        $.mobile.path.isPath(hash) ||
                        hash === $.mobile.dialogHashKey))) {

                    // make sure to set initial popstate state if it exists
                    // so that navigation back to the initial page works properly
                    $.mobile.navigate.navigator.squash(path.parseLocation().href);

                    $.mobile.changePage($.mobile.firstPage, {
                        reverse: true,
                        changeHash: false,
                        fromHashChange: true
                    });
                } else {
                    // TODO figure out how to simplify this interaction with the initial history entry
                    // at the bottom js/navigate/navigate.js
                    $.mobile.navigate.history.stack = [];
                    $.mobile.navigate($.mobile.path.isPath(location.hash) ? location.hash : location.href);
                }
            }
        });

        $(function () {
            //Run inlineSVG support test
            $.support.inlineSVG();

            // check which scrollTop value should be used by scrolling to 1 immediately at domready
            // then check what the scroll top is. Android will report 0... others 1
            // note that this initial scroll won't hide the address bar. It's just for the check.

            // hide iOS browser chrome on load if hideUrlBar is true this is to try and do it as soon as possible
            if ($.mobile.hideUrlBar) {
                window.scrollTo(0, 1);
            }

            // if defaultHomeScroll hasn't been set yet, see if scrollTop is 1
            // it should be 1 in most browsers, but android treats 1 as 0 (for hiding addr bar)
            // so if it's 1, use 0 from now on
            $.mobile.defaultHomeScroll = (!$.support.scrollTop || $.mobile.window.scrollTop() === 1) ? 0 : 1;

            // window load event
            // hide iOS browser chrome on load if hideUrlBar is true this is as fall back incase we were too early before
            if ($.mobile.hideUrlBar) {
                $window.load($.mobile.silentScroll);
            }

            if (!$.support.cssPointerEvents) {
                // IE and Opera don't support CSS pointer-events: none that we use to disable link-based buttons
                // by adding the 'ui-disabled' class to them. Using a JavaScript workaround for those browser.
                // https://github.com/jquery/jquery-mobile/issues/3558

                // DEPRECATED as of 1.4.0 - remove ui-disabled after 1.4.0 release
                // only ui-state-disabled should be present thereafter
                $.mobile.document.delegate(".ui-state-disabled,.ui-disabled", "click",
                    function (e) {
                        e.preventDefault();
                        e.stopImmediatePropagation();
                    }
                );
            }
        });
    }(jQuery, this));

    (function ($, undefined) {

        $.fn.selectmenu = function () {
            return this;
        };
    })(jQuery);

}));
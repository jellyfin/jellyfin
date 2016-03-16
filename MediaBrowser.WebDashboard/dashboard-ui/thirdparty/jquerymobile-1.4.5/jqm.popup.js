define(['jqmwidget'], function () {

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
        };

        // Allow default callback to be configured on mobileInit
        $.fn.animationComplete.defaultDuration = 1000;
    })(jQuery);

    (function ($, undefined) {

        function fitSegmentInsideSegment(windowSize, segmentSize, offset, desired) {
            var returnValue = desired;

            if (windowSize < segmentSize) {
                // Center segment if it's bigger than the window
                returnValue = offset + (windowSize - segmentSize) / 2;
            } else {
                // Otherwise center it at the desired coordinate while keeping it completely inside the window
                returnValue = Math.min(Math.max(offset, desired - segmentSize / 2), offset + windowSize - segmentSize);
            }

            return returnValue;
        }

        function getWindowCoordinates(theWindow) {
            return {
                x: theWindow.scrollLeft(),
                y: theWindow.scrollTop(),
                cx: (theWindow[0].innerWidth || theWindow.width()),
                cy: (theWindow[0].innerHeight || theWindow.height())
            };
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

        $.widget("mobile.popup", {
            options: {
                wrapperClass: null,
                theme: null,
                overlayTheme: null,
                shadow: true,
                corners: true,
                transition: "none",
                positionTo: "origin",
                tolerance: null,
                closeLinkSelector: "a[data-rel='back']",
                closeLinkEvents: "click.popup",
                navigateEvents: "navigate.popup",
                closeEvents: "navigate.popup pagebeforechange.popup",
                dismissible: true,
                enhanced: false,

                // NOTE Windows Phone 7 has a scroll position caching issue that
                //      requires us to disable popup history management by default
                //      https://github.com/jquery/jquery-mobile/issues/4784
                //
                // NOTE this option is modified in _create!
                history: !$.mobile.browser.oldIE
            },

            // When the user depresses the mouse/finger on an element inside the popup while the popup is
            // open, we ignore resize events for a short while. This prevents #6961.
            _handleDocumentVmousedown: function (theEvent) {
                if (this._isOpen && $.contains(this._ui.container[0], theEvent.target)) {
                    this._ignoreResizeEvents();
                }
            },

            _create: function () {
                var theElement = this.element,
                    myId = theElement.attr("id"),
                    currentOptions = this.options;

                // We need to adjust the history option to be false if there's no AJAX nav.
                // We can't do it in the option declarations because those are run before
                // it is determined whether there shall be AJAX nav.
                currentOptions.history = currentOptions.history && $.mobile.ajaxEnabled && $.mobile.hashListeningEnabled;

                this._on(this.document, {
                    "mousedown": "_handleDocumentVmousedown"
                });

                // Define instance variables
                $.extend(this, {
                    _scrollTop: 0,
                    _page: theElement.parents("div[data-role='page']"),
                    _ui: null,
                    _fallbackTransition: "",
                    _currentTransition: false,
                    _prerequisites: null,
                    _isOpen: false,
                    _tolerance: null,
                    _resizeData: null,
                    _ignoreResizeTo: 0,
                    _orientationchangeInProgress: false
                });

                if (this._page.length === 0) {
                    this._page = $("body");
                }

                if (currentOptions.enhanced) {
                    this._ui = {
                        container: theElement.parent(),
                        screen: theElement.parent().prev(),
                        placeholder: $(this.document[0].getElementById(myId + "-placeholder"))
                    };
                } else {
                    this._ui = this._enhance(theElement, myId);
                    this._applyTransition(currentOptions.transition);
                }
                this
                    ._setTolerance(currentOptions.tolerance)
                    ._ui.focusElement = this._ui.container;

                // Event handlers
                this._on(this._ui.screen, { "click": "_eatEventAndClose" });
                this._on(this.window, {
                    orientationchange: $.proxy(this, "_handleWindowOrientationchange"),
                    resize: $.proxy(this, "_handleWindowResize")
                });
                this._on(this.document, { "focusin": "_handleDocumentFocusIn" });
            },

            _delay: function (handler, delay) {
                function handlerProxy() {
                    return (typeof handler === "string" ? instance[handler] : handler)
                        .apply(instance, arguments);
                }
                var instance = this;
                return setTimeout(handlerProxy, delay || 0);
            },

            _enhance: function (theElement, myId) {
                var currentOptions = this.options,
                    wrapperClass = currentOptions.wrapperClass,
                    ui = {
                        screen: $("<div class='ui-screen-hidden ui-popup-screen " +
                        this._themeClassFromOption("ui-overlay-", currentOptions.overlayTheme) + "'></div>"),
                        placeholder: $("<div style='display: none;'><!-- placeholder --></div>"),
                        container: $("<div class='ui-popup-container ui-popup-hidden ui-popup-truncate" +
                            (wrapperClass ? (" " + wrapperClass) : "") + "'></div>")
                    },
                    fragment = this.document[0].createDocumentFragment();

                fragment.appendChild(ui.screen[0]);
                fragment.appendChild(ui.container[0]);

                if (myId) {
                    ui.screen.attr("id", myId + "-screen");
                    ui.container.attr("id", myId + "-popup");
                    ui.placeholder
                        .attr("id", myId + "-placeholder")
                        .html("<!-- placeholder for " + myId + " -->");
                }

                // Apply the proto
                this._page[0].appendChild(fragment);
                // Leave a placeholder where the element used to be
                ui.placeholder.insertAfter(theElement);
                theElement
                    .detach()
                    .addClass("ui-popup " +
                        this._themeClassFromOption("ui-body-", currentOptions.theme) + " " +
                        (currentOptions.shadow ? "ui-overlay-shadow " : "") +
                        (currentOptions.corners ? "ui-corner-all " : ""))
                    .appendTo(ui.container);

                return ui;
            },

            _eatEventAndClose: function (theEvent) {
                theEvent.preventDefault();
                theEvent.stopImmediatePropagation();
                if (this.options.dismissible) {
                    this.close();
                }
                return false;
            },

            // Make sure the screen covers the entire document - CSS is sometimes not
            // enough to accomplish this.
            _resizeScreen: function () {
                var screen = this._ui.screen,
                    popupHeight = this._ui.container.outerHeight(true),
                    screenHeight = screen.removeAttr("style").height(),

                    // Subtracting 1 here is necessary for an obscure Andrdoid 4.0 bug where
                    // the browser hangs if the screen covers the entire document :/
                    documentHeight = this.document.height() - 1;

                if (screenHeight < documentHeight) {
                    screen.height(documentHeight);
                } else if (popupHeight > screenHeight) {
                    screen.height(popupHeight);
                }
            },

            _expectResizeEvent: function () {
                var windowCoordinates = getWindowCoordinates(this.window);

                if (this._resizeData) {
                    if (windowCoordinates.x === this._resizeData.windowCoordinates.x &&
                        windowCoordinates.y === this._resizeData.windowCoordinates.y &&
                        windowCoordinates.cx === this._resizeData.windowCoordinates.cx &&
                        windowCoordinates.cy === this._resizeData.windowCoordinates.cy) {
                        // timeout not refreshed
                        return false;
                    } else {
                        // clear existing timeout - it will be refreshed below
                        clearTimeout(this._resizeData.timeoutId);
                    }
                }

                this._resizeData = {
                    timeoutId: this._delay("_resizeTimeout", 200),
                    windowCoordinates: windowCoordinates
                };

                return true;
            },

            _resizeTimeout: function () {
                if (this._isOpen) {
                    if (!this._expectResizeEvent()) {
                        if (this._ui.container.hasClass("ui-popup-hidden")) {
                            // effectively rapid-open the popup while leaving the screen intact
                            this._ui.container.removeClass("ui-popup-hidden ui-popup-truncate");
                            this.reposition({ positionTo: "window" });
                            this._ignoreResizeEvents();
                        }

                        this._resizeScreen();
                        this._resizeData = null;
                        this._orientationchangeInProgress = false;
                    }
                } else {
                    this._resizeData = null;
                    this._orientationchangeInProgress = false;
                }
            },

            _stopIgnoringResizeEvents: function () {
                this._ignoreResizeTo = 0;
            },

            _ignoreResizeEvents: function () {
                if (this._ignoreResizeTo) {
                    clearTimeout(this._ignoreResizeTo);
                }
                this._ignoreResizeTo = this._delay("_stopIgnoringResizeEvents", 1000);
            },

            _handleWindowResize: function (/* theEvent */) {
                if (this._isOpen && this._ignoreResizeTo === 0) {
                    if ((this._expectResizeEvent() || this._orientationchangeInProgress) &&
                        !this._ui.container.hasClass("ui-popup-hidden")) {
                        // effectively rapid-close the popup while leaving the screen intact
                        this._ui.container
                            .addClass("ui-popup-hidden ui-popup-truncate")
                            .removeAttr("style");
                    }
                }
            },

            _handleWindowOrientationchange: function (/* theEvent */) {
                if (!this._orientationchangeInProgress && this._isOpen && this._ignoreResizeTo === 0) {
                    this._expectResizeEvent();
                    this._orientationchangeInProgress = true;
                }
            },

            // When the popup is open, attempting to focus on an element that is not a
            // child of the popup will redirect focus to the popup
            _handleDocumentFocusIn: function (theEvent) {
                var target,
                    targetElement = theEvent.target,
                    ui = this._ui;

                if (!this._isOpen) {
                    return;
                }

                if (targetElement !== ui.container[0]) {
                    target = $(targetElement);
                    if (!$.contains(ui.container[0], targetElement)) {
                        $(this.document[0].activeElement).one("focus", $.proxy(function () {
                            this._safelyBlur(targetElement);
                        }, this));
                        ui.focusElement.focus();
                        theEvent.preventDefault();
                        theEvent.stopImmediatePropagation();
                        return false;
                    } else if (ui.focusElement[0] === ui.container[0]) {
                        ui.focusElement = target;
                    }
                }

                this._ignoreResizeEvents();
            },

            _themeClassFromOption: function (prefix, value) {
                return (value ? (value === "none" ? "" : (prefix + value)) : (prefix + "inherit"));
            },

            _applyTransition: function (value) {
                if (value) {
                    this._ui.container.removeClass(this._fallbackTransition);
                    if (value !== "none") {
                        this._fallbackTransition = $.mobile._maybeDegradeTransition(value);
                        if (this._fallbackTransition === "none") {
                            this._fallbackTransition = "";
                        }
                        this._ui.container.addClass(this._fallbackTransition);
                    }
                }

                return this;
            },

            _setOptions: function (newOptions) {
                var currentOptions = this.options,
                    theElement = this.element,
                    screen = this._ui.screen;

                if (newOptions.wrapperClass !== undefined) {
                    this._ui.container
                        .removeClass(currentOptions.wrapperClass)
                        .addClass(newOptions.wrapperClass);
                }

                if (newOptions.theme !== undefined) {
                    theElement
                        .removeClass(this._themeClassFromOption("ui-body-", currentOptions.theme))
                        .addClass(this._themeClassFromOption("ui-body-", newOptions.theme));
                }

                if (newOptions.overlayTheme !== undefined) {
                    screen
                        .removeClass(this._themeClassFromOption("ui-overlay-", currentOptions.overlayTheme))
                        .addClass(this._themeClassFromOption("ui-overlay-", newOptions.overlayTheme));

                    if (this._isOpen) {
                        screen.addClass("in");
                    }
                }

                if (newOptions.shadow !== undefined) {
                    theElement.toggleClass("ui-overlay-shadow", newOptions.shadow);
                }

                if (newOptions.corners !== undefined) {
                    theElement.toggleClass("ui-corner-all", newOptions.corners);
                }

                if (newOptions.transition !== undefined) {
                    if (!this._currentTransition) {
                        this._applyTransition(newOptions.transition);
                    }
                }

                if (newOptions.tolerance !== undefined) {
                    this._setTolerance(newOptions.tolerance);
                }

                if (newOptions.disabled !== undefined) {
                    if (newOptions.disabled) {
                        this.close();
                    }
                }

                return this._super(newOptions);
            },

            _setTolerance: function (value) {
                var tol = { t: 30, r: 15, b: 30, l: 15 },
                    ar;

                if (value !== undefined) {
                    ar = String(value).split(",");

                    $.each(ar, function (idx, val) { ar[idx] = parseInt(val, 10); });

                    switch (ar.length) {
                        // All values are to be the same
                        case 1:
                            if (!isNaN(ar[0])) {
                                tol.t = tol.r = tol.b = tol.l = ar[0];
                            }
                            break;

                            // The first value denotes top/bottom tolerance, and the second value denotes left/right tolerance
                        case 2:
                            if (!isNaN(ar[0])) {
                                tol.t = tol.b = ar[0];
                            }
                            if (!isNaN(ar[1])) {
                                tol.l = tol.r = ar[1];
                            }
                            break;

                            // The array contains values in the order top, right, bottom, left
                        case 4:
                            if (!isNaN(ar[0])) {
                                tol.t = ar[0];
                            }
                            if (!isNaN(ar[1])) {
                                tol.r = ar[1];
                            }
                            if (!isNaN(ar[2])) {
                                tol.b = ar[2];
                            }
                            if (!isNaN(ar[3])) {
                                tol.l = ar[3];
                            }
                            break;

                        default:
                            break;
                    }
                }

                this._tolerance = tol;
                return this;
            },

            _clampPopupWidth: function (infoOnly) {
                var menuSize,
                    windowCoordinates = getWindowCoordinates(this.window),
                    // rectangle within which the popup must fit
                    rectangle = {
                        x: this._tolerance.l,
                        y: windowCoordinates.y + this._tolerance.t,
                        cx: windowCoordinates.cx - this._tolerance.l - this._tolerance.r,
                        cy: windowCoordinates.cy - this._tolerance.t - this._tolerance.b
                    };

                if (!infoOnly) {
                    // Clamp the width of the menu before grabbing its size
                    this._ui.container.css("max-width", rectangle.cx);
                }

                menuSize = {
                    cx: this._ui.container.outerWidth(true),
                    cy: this._ui.container.outerHeight(true)
                };

                return { rc: rectangle, menuSize: menuSize };
            },

            _calculateFinalLocation: function (desired, clampInfo) {
                var returnValue,
                    rectangle = clampInfo.rc,
                    menuSize = clampInfo.menuSize;

                // Center the menu over the desired coordinates, while not going outside
                // the window tolerances. This will center wrt. the window if the popup is
                // too large.
                returnValue = {
                    left: fitSegmentInsideSegment(rectangle.cx, menuSize.cx, rectangle.x, desired.x),
                    top: fitSegmentInsideSegment(rectangle.cy, menuSize.cy, rectangle.y, desired.y)
                };

                // Make sure the top of the menu is visible
                returnValue.top = Math.max(0, returnValue.top);

                // If the height of the menu is smaller than the height of the document
                // align the bottom with the bottom of the document

                returnValue.top -= Math.min(returnValue.top,
                    Math.max(0, returnValue.top + menuSize.cy - this.document.height()));

                return returnValue;
            },

            // Try and center the overlay over the given coordinates
            _placementCoords: function (desired) {
                return this._calculateFinalLocation(desired, this._clampPopupWidth());
            },

            _createPrerequisites: function (screenPrerequisite, containerPrerequisite, whenDone) {
                var prerequisites,
                    self = this;

                // It is important to maintain both the local variable prerequisites and
                // self._prerequisites. The local variable remains in the closure of the
                // functions which call the callbacks passed in. The comparison between the
                // local variable and self._prerequisites is necessary, because once a
                // function has been passed to .animationComplete() it will be called next
                // time an animation completes, even if that's not the animation whose end
                // the function was supposed to catch (for example, if an abort happens
                // during the opening animation, the .animationComplete handler is not
                // called for that animation anymore, but the handler remains attached, so
                // it is called the next time the popup is opened - making it stale.
                // Comparing the local variable prerequisites to the widget-level variable
                // self._prerequisites ensures that callbacks triggered by a stale
                // .animationComplete will be ignored.

                prerequisites = {
                    screen: $.Deferred(),
                    container: $.Deferred()
                };

                prerequisites.screen.then(function () {
                    if (prerequisites === self._prerequisites) {
                        screenPrerequisite();
                    }
                });

                prerequisites.container.then(function () {
                    if (prerequisites === self._prerequisites) {
                        containerPrerequisite();
                    }
                });

                Promise.all([prerequisites.screen, prerequisites.container]).then(function () {
                    if (prerequisites === self._prerequisites) {
                        self._prerequisites = null;
                        whenDone();
                    }
                });

                self._prerequisites = prerequisites;
            },

            _animate: function (args) {
                // NOTE before removing the default animation of the screen
                //      this had an animate callback that would resolve the deferred
                //      now the deferred is resolved immediately
                // TODO remove the dependency on the screen deferred
                this._ui.screen
                    .removeClass(args.classToRemove)
                    .addClass(args.screenClassToAdd);

                args.prerequisites.screen.resolve();

                if (args.transition && args.transition !== "none") {
                    if (args.applyTransition) {
                        this._applyTransition(args.transition);
                    }
                    if (this._fallbackTransition) {
                        this._ui.container
                            .addClass(args.containerClassToAdd)
                            .removeClass(args.classToRemove)
                            .animationComplete($.proxy(args.prerequisites.container, "resolve"));
                        return;
                    }
                }
                this._ui.container.removeClass(args.classToRemove);
                args.prerequisites.container.resolve();
            },

            // The desired coordinates passed in will be returned untouched if no reference element can be identified via
            // desiredPosition.positionTo. Nevertheless, this function ensures that its return value always contains valid
            // x and y coordinates by specifying the center middle of the window if the coordinates are absent.
            // options: { x: coordinate, y: coordinate, positionTo: string: "origin", "window", or jQuery selector
            _desiredCoords: function (openOptions) {
                var offset,
                    dst = null,
                    windowCoordinates = getWindowCoordinates(this.window),
                    x = openOptions.x,
                    y = openOptions.y,
                    pTo = openOptions.positionTo;

                // Establish which element will serve as the reference
                if (pTo && pTo !== "origin") {
                    if (pTo === "window") {
                        x = windowCoordinates.cx / 2 + windowCoordinates.x;
                        y = windowCoordinates.cy / 2 + windowCoordinates.y;
                    } else {
                        try {
                            dst = $(pTo);
                        } catch (err) {
                            dst = null;
                        }
                        if (dst) {
                            dst.filter(":visible");
                            if (dst.length === 0) {
                                dst = null;
                            }
                        }
                    }
                }

                // If an element was found, center over it
                if (dst) {
                    offset = dst.offset();
                    x = offset.left + dst.outerWidth() / 2;
                    y = offset.top + dst.outerHeight() / 2;
                }

                // Make sure x and y are valid numbers - center over the window
                if ($.type(x) !== "number" || isNaN(x)) {
                    x = windowCoordinates.cx / 2 + windowCoordinates.x;
                }
                if ($.type(y) !== "number" || isNaN(y)) {
                    y = windowCoordinates.cy / 2 + windowCoordinates.y;
                }

                return { x: x, y: y };
            },

            _reposition: function (openOptions) {
                // We only care about position-related parameters for repositioning
                openOptions = {
                    x: openOptions.x,
                    y: openOptions.y,
                    positionTo: openOptions.positionTo
                };
                this._trigger("beforeposition", undefined, openOptions);
                this._ui.container.offset(this._placementCoords(this._desiredCoords(openOptions)));
            },

            reposition: function (openOptions) {
                if (this._isOpen) {
                    this._reposition(openOptions);
                }
            },

            _safelyBlur: function (currentElement) {
                if (currentElement !== this.window[0] &&
                    currentElement.nodeName.toLowerCase() !== "body") {
                    $(currentElement).blur();
                }
            },

            _openPrerequisitesComplete: function () {
                var id = this.element.attr("id");

                this._ui.container.addClass("ui-popup-active");
                this._isOpen = true;
                this._resizeScreen();

                // Check to see if currElement is not a child of the container.  If it's not, blur
                if (!$.contains(this._ui.container[0], this.document[0].activeElement)) {
                    this._safelyBlur(this.document[0].activeElement);
                }
                this._ignoreResizeEvents();
                if (id) {
                    this.document.find("[aria-haspopup='true'][aria-owns='" + id + "']").attr("aria-expanded", true);
                }
                this._trigger("afteropen");
            },

            _open: function (options) {
                var openOptions = $.extend({}, this.options, options),
                    // TODO move blacklist to private method
                    androidBlacklist = (function () {
                        var ua = navigator.userAgent,
                            // Rendering engine is Webkit, and capture major version
                            wkmatch = ua.match(/AppleWebKit\/([0-9\.]+)/),
                            wkversion = !!wkmatch && wkmatch[1],
                            androidmatch = ua.match(/Android (\d+(?:\.\d+))/),
                            andversion = !!androidmatch && androidmatch[1],
                            chromematch = ua.indexOf("Chrome") > -1;

                        // Platform is Android, WebKit version is greater than 534.13 ( Android 3.2.1 ) and not Chrome.
                        if (androidmatch !== null && andversion === "4.0" && wkversion && wkversion > 534.13 && !chromematch) {
                            return true;
                        }
                        return false;
                    }());

                // Count down to triggering "popupafteropen" - we have two prerequisites:
                // 1. The popup window animation completes (container())
                // 2. The screen opacity animation completes (screen())
                this._createPrerequisites(
                    $.noop,
                    $.noop,
                    $.proxy(this, "_openPrerequisitesComplete"));

                this._currentTransition = openOptions.transition;
                this._applyTransition(openOptions.transition);

                this._ui.screen.removeClass("ui-screen-hidden");
                this._ui.container.removeClass("ui-popup-truncate");

                // Give applications a chance to modify the contents of the container before it appears
                this._reposition(openOptions);

                this._ui.container.removeClass("ui-popup-hidden");

                if (this.options.overlayTheme && androidBlacklist) {
                    /* TODO: The native browser on Android 4.0.X ("Ice Cream Sandwich") suffers from an issue where the popup overlay appears to be z-indexed above the popup itself when certain other styles exist on the same page -- namely, any element set to `position: fixed` and certain types of input. These issues are reminiscent of previously uncovered bugs in older versions of Android's native browser: https://github.com/scottjehl/Device-Bugs/issues/3
                    This fix closes the following bugs ( I use "closes" with reluctance, and stress that this issue should be revisited as soon as possible ):
                    https://github.com/jquery/jquery-mobile/issues/4816
                    https://github.com/jquery/jquery-mobile/issues/4844
                    https://github.com/jquery/jquery-mobile/issues/4874
                    */

                    // TODO sort out why this._page isn't working
                    this.element.closest(".ui-page").addClass("ui-popup-open");
                }
                this._animate({
                    additionalCondition: true,
                    transition: openOptions.transition,
                    classToRemove: "",
                    screenClassToAdd: "in",
                    containerClassToAdd: "in",
                    applyTransition: false,
                    prerequisites: this._prerequisites
                });
            },

            _closePrerequisiteScreen: function () {
                this._ui.screen
                    .removeClass("out")
                    .addClass("ui-screen-hidden");
            },

            _closePrerequisiteContainer: function () {
                this._ui.container
                    .removeClass("reverse out")
                    .addClass("ui-popup-hidden ui-popup-truncate")
                    .removeAttr("style");
            },

            _closePrerequisitesDone: function () {
                var container = this._ui.container,
                    id = this.element.attr("id");

                // remove the global mutex for popups
                $.mobile.popup.active = undefined;

                // Blur elements inside the container, including the container
                $(":focus", container[0]).add(container[0]).blur();

                if (id) {
                    this.document.find("[aria-haspopup='true'][aria-owns='" + id + "']").attr("aria-expanded", false);
                }

                // alert users that the popup is closed
                this._trigger("afterclose");
            },

            _close: function (immediate) {
                this._ui.container.removeClass("ui-popup-active");
                this._page.removeClass("ui-popup-open");

                this._isOpen = false;

                // Count down to triggering "popupafterclose" - we have two prerequisites:
                // 1. The popup window reverse animation completes (container())
                // 2. The screen opacity animation completes (screen())
                this._createPrerequisites(
                    $.proxy(this, "_closePrerequisiteScreen"),
                    $.proxy(this, "_closePrerequisiteContainer"),
                    $.proxy(this, "_closePrerequisitesDone"));

                this._animate({
                    additionalCondition: this._ui.screen.hasClass("in"),
                    transition: (immediate ? "none" : (this._currentTransition)),
                    classToRemove: "in",
                    screenClassToAdd: "out",
                    containerClassToAdd: "reverse out",
                    applyTransition: true,
                    prerequisites: this._prerequisites
                });
            },

            _unenhance: function () {
                if (this.options.enhanced) {
                    return;
                }

                // Put the element back to where the placeholder was and remove the "ui-popup" class
                this._setOptions({ theme: $.mobile.popup.prototype.options.theme });
                this.element
                    // Cannot directly insertAfter() - we need to detach() first, because
                    // insertAfter() will do nothing if the payload div was not attached
                    // to the DOM at the time the widget was created, and so the payload
                    // will remain inside the container even after we call insertAfter().
                    // If that happens and we remove the container a few lines below, we
                    // will cause an infinite recursion - #5244
                    .detach()
                    .insertAfter(this._ui.placeholder)
                    .removeClass("ui-popup ui-overlay-shadow ui-corner-all ui-body-inherit");
                this._ui.screen.remove();
                this._ui.container.remove();
                this._ui.placeholder.remove();
            },

            _destroy: function () {
                if ($.mobile.popup.active === this) {
                    this.element.one("popupafterclose", $.proxy(this, "_unenhance"));
                    this.close();
                } else {
                    this._unenhance();
                }

                return this;
            },

            _closePopup: function (theEvent, data) {
                var parsedDst, toUrl,
                    currentOptions = this.options,
                    immediate = false;

                if ((theEvent && theEvent.isDefaultPrevented()) || $.mobile.popup.active !== this) {
                    return;
                }

                // restore location on screen
                window.scrollTo(0, this._scrollTop);

                if (theEvent && theEvent.type === "pagebeforechange" && data) {
                    // Determine whether we need to rapid-close the popup, or whether we can
                    // take the time to run the closing transition
                    if (typeof data.toPage === "string") {
                        parsedDst = data.toPage;
                    } else {
                        parsedDst = data.toPage.data("url");
                    }
                    parsedDst = $.mobile.path.parseUrl(parsedDst);
                    toUrl = parsedDst.pathname + parsedDst.search + parsedDst.hash;

                    if (this._myUrl !== $.mobile.path.makeUrlAbsolute(toUrl)) {
                        // Going to a different page - close immediately
                        immediate = true;
                    } else {
                        theEvent.preventDefault();
                    }
                }

                // remove nav bindings
                this.window.off(currentOptions.closeEvents);
                // unbind click handlers added when history is disabled
                this.element.off(currentOptions.closeLinkEvents, currentOptions.closeLinkSelector);

                this._close(immediate);
            },

            // any navigation event after a popup is opened should close the popup
            // NOTE the pagebeforechange is bound to catch navigation events that don't
            //      alter the url (eg, dialogs from popups)
            _bindContainerClose: function () {
                this.window
                    .on(this.options.closeEvents, $.proxy(this, "_closePopup"));
            },

            widget: function () {
                return this._ui.container;
            },

            // TODO no clear deliniation of what should be here and
            // what should be in _open. Seems to be "visual" vs "history" for now
            open: function (options) {
                var url, hashkey, activePage, currentIsDialog, hasHash, urlHistory,
                    self = this,
                    currentOptions = this.options;

                // make sure open is idempotent
                if ($.mobile.popup.active || currentOptions.disabled) {
                    return this;
                }

                // set the global popup mutex
                $.mobile.popup.active = this;
                this._scrollTop = this.window.scrollTop();

                // if history alteration is disabled close on navigate events
                // and leave the url as is
                if (!(currentOptions.history)) {
                    self._open(options);
                    self._bindContainerClose();

                    // When histoy is disabled we have to grab the data-rel
                    // back link clicks so we can close the popup instead of
                    // relying on history to do it for us
                    self.element
                        .on(currentOptions.closeLinkEvents, currentOptions.closeLinkSelector, function (theEvent) {
                            self.close();
                            theEvent.preventDefault();
                        });

                    return this;
                }

                // cache some values for min/readability
                urlHistory = $.mobile.navigate.history;
                hashkey = $.mobile.dialogHashKey;
                activePage = $.mobile.activePage;
                currentIsDialog = (activePage ? activePage.hasClass("ui-dialog") : false);
                this._myUrl = url = urlHistory.getActive().url;
                hasHash = (url.indexOf(hashkey) > -1) && !currentIsDialog && (urlHistory.activeIndex > 0);

                if (hasHash) {
                    self._open(options);
                    self._bindContainerClose();
                    return this;
                }

                // if the current url has no dialog hash key proceed as normal
                // otherwise, if the page is a dialog simply tack on the hash key
                if (url.indexOf(hashkey) === -1 && !currentIsDialog) {
                    url = url + (url.indexOf("#") > -1 ? hashkey : "#" + hashkey);
                } else {
                    url = $.mobile.path.parseLocation().hash + hashkey;
                }

                // swallow the the initial navigation event, and bind for the next
                this.window.one("beforenavigate", function (theEvent) {
                    theEvent.preventDefault();
                    self._open(options);
                    self._bindContainerClose();
                });

                this.urlAltered = true;
                $.mobile.navigate(url, { role: "dialog" });

                return this;
            },

            close: function () {
                // make sure close is idempotent
                if ($.mobile.popup.active !== this) {
                    return this;
                }

                this._scrollTop = this.window.scrollTop();

                if (this.options.history && this.urlAltered) {
                    $.mobile.pageContainer.pagecontainer("back");
                    this.urlAltered = false;
                } else {
                    // simulate the nav bindings having fired
                    this._closePopup();
                }

                return this;
            }
        });

        // TODO this can be moved inside the widget
        $.mobile.popup.handleLink = function ($link) {
            var offset,
                path = $.mobile.path,

                // NOTE make sure to get only the hash from the href because ie7 (wp7)
                //      returns the absolute href in this case ruining the element selection
                popup = $(path.hashToSelector(path.parseUrl($link.attr("href")).hash)).first();

            if (popup.length > 0 && popup.data("mobile-popup")) {
                offset = $link.offset();
                popup.popup("open", {
                    x: offset.left + $link.outerWidth() / 2,
                    y: offset.top + $link.outerHeight() / 2,
                    transition: $link.data("transition"),
                    positionTo: $link.data("position-to")
                });
            }

            //remove after delay
            setTimeout(function () {
                $link.removeClass($.mobile.activeBtnClass);
            }, 300);
        };

        // TODO move inside _create
        $(document).on("pagebeforechange", function (theEvent, data) {
            if (data.options.role === "popup") {
                $.mobile.popup.handleLink(data.options.link);
                theEvent.preventDefault();
            }
        });

    })(jQuery);


    (function ($, undefined) {

        var ieHack = ($.mobile.browser.oldIE && $.mobile.browser.oldIE <= 8),
            uiTemplate = $(
                "<div class='ui-popup-arrow-guide'></div>" +
                "<div class='ui-popup-arrow-container" + (ieHack ? " ie" : "") + "'>" +
                    "<div class='ui-popup-arrow'></div>" +
                "</div>"
            );

        function getArrow() {
            var clone = uiTemplate.clone(),
                gd = clone.eq(0),
                ct = clone.eq(1),
                ar = ct.children();

            return { arEls: ct.add(gd), gd: gd, ct: ct, ar: ar };
        }

        $.widget("mobile.popup", $.mobile.popup, {
            options: {

                arrow: ""
            },

            _create: function () {
                var ar,
                    ret = this._super();

                if (this.options.arrow) {
                    this._ui.arrow = ar = this._addArrow();
                }

                return ret;
            },

            _addArrow: function () {
                var theme,
                    opts = this.options,
                    ar = getArrow();

                theme = this._themeClassFromOption("ui-body-", opts.theme);
                ar.ar.addClass(theme + (opts.shadow ? " ui-overlay-shadow" : ""));
                ar.arEls.hide().appendTo(this.element);

                return ar;
            },

            _unenhance: function () {
                var ar = this._ui.arrow;

                if (ar) {
                    ar.arEls.remove();
                }

                return this._super();
            },

            // Pretend to show an arrow described by @p and @dir and calculate the
            // distance from the desired point. If a best-distance is passed in, return
            // the minimum of the one passed in and the one calculated.
            _tryAnArrow: function (p, dir, desired, s, best) {
                var result, r, diff, desiredForArrow = {}, tip = {};

                // If the arrow has no wiggle room along the edge of the popup, it cannot
                // be displayed along the requested edge without it sticking out.
                if (s.arFull[p.dimKey] > s.guideDims[p.dimKey]) {
                    return best;
                }

                desiredForArrow[p.fst] = desired[p.fst] +
                    (s.arHalf[p.oDimKey] + s.menuHalf[p.oDimKey]) * p.offsetFactor -
                    s.contentBox[p.fst] + (s.clampInfo.menuSize[p.oDimKey] - s.contentBox[p.oDimKey]) * p.arrowOffsetFactor;
                desiredForArrow[p.snd] = desired[p.snd];

                result = s.result || this._calculateFinalLocation(desiredForArrow, s.clampInfo);
                r = { x: result.left, y: result.top };

                tip[p.fst] = r[p.fst] + s.contentBox[p.fst] + p.tipOffset;
                tip[p.snd] = Math.max(result[p.prop] + s.guideOffset[p.prop] + s.arHalf[p.dimKey],
                    Math.min(result[p.prop] + s.guideOffset[p.prop] + s.guideDims[p.dimKey] - s.arHalf[p.dimKey],
                        desired[p.snd]));

                diff = Math.abs(desired.x - tip.x) + Math.abs(desired.y - tip.y);
                if (!best || diff < best.diff) {
                    // Convert tip offset to coordinates inside the popup
                    tip[p.snd] -= s.arHalf[p.dimKey] + result[p.prop] + s.contentBox[p.snd];
                    best = { dir: dir, diff: diff, result: result, posProp: p.prop, posVal: tip[p.snd] };
                }

                return best;
            },

            _getPlacementState: function (clamp) {
                var offset, gdOffset,
                    ar = this._ui.arrow,
                    state = {
                        clampInfo: this._clampPopupWidth(!clamp),
                        arFull: { cx: ar.ct.width(), cy: ar.ct.height() },
                        guideDims: { cx: ar.gd.width(), cy: ar.gd.height() },
                        guideOffset: ar.gd.offset()
                    };

                offset = this.element.offset();

                ar.gd.css({ left: 0, top: 0, right: 0, bottom: 0 });
                gdOffset = ar.gd.offset();
                state.contentBox = {
                    x: gdOffset.left - offset.left,
                    y: gdOffset.top - offset.top,
                    cx: ar.gd.width(),
                    cy: ar.gd.height()
                };
                ar.gd.removeAttr("style");

                // The arrow box moves between guideOffset and guideOffset + guideDims - arFull
                state.guideOffset = { left: state.guideOffset.left - offset.left, top: state.guideOffset.top - offset.top };
                state.arHalf = { cx: state.arFull.cx / 2, cy: state.arFull.cy / 2 };
                state.menuHalf = { cx: state.clampInfo.menuSize.cx / 2, cy: state.clampInfo.menuSize.cy / 2 };

                return state;
            },

            _placementCoords: function (desired) {
                var state, best, params, elOffset, bgRef,
                    optionValue = this.options.arrow,
                    ar = this._ui.arrow;

                if (!ar) {
                    return this._super(desired);
                }

                ar.arEls.show();

                bgRef = {};
                state = this._getPlacementState(true);
                params = {
                    "l": { fst: "x", snd: "y", prop: "top", dimKey: "cy", oDimKey: "cx", offsetFactor: 1, tipOffset: -state.arHalf.cx, arrowOffsetFactor: 0 },
                    "r": { fst: "x", snd: "y", prop: "top", dimKey: "cy", oDimKey: "cx", offsetFactor: -1, tipOffset: state.arHalf.cx + state.contentBox.cx, arrowOffsetFactor: 1 },
                    "b": { fst: "y", snd: "x", prop: "left", dimKey: "cx", oDimKey: "cy", offsetFactor: -1, tipOffset: state.arHalf.cy + state.contentBox.cy, arrowOffsetFactor: 1 },
                    "t": { fst: "y", snd: "x", prop: "left", dimKey: "cx", oDimKey: "cy", offsetFactor: 1, tipOffset: -state.arHalf.cy, arrowOffsetFactor: 0 }
                };

                // Try each side specified in the options to see on which one the arrow
                // should be placed such that the distance between the tip of the arrow and
                // the desired coordinates is the shortest.
                $.each((optionValue === true ? "l,t,r,b" : optionValue).split(","),
                    $.proxy(function (key, value) {
                        best = this._tryAnArrow(params[value], value, desired, state, best);
                    }, this));

                // Could not place the arrow along any of the edges - behave as if showing
                // the arrow was turned off.
                if (!best) {
                    ar.arEls.hide();
                    return this._super(desired);
                }

                // Move the arrow into place
                ar.ct
                    .removeClass("ui-popup-arrow-l ui-popup-arrow-t ui-popup-arrow-r ui-popup-arrow-b")
                    .addClass("ui-popup-arrow-" + best.dir)
                    .removeAttr("style").css(best.posProp, best.posVal)
                    .show();

                // Do not move/size the background div on IE, because we use the arrow div for background as well.
                if (!ieHack) {
                    elOffset = this.element.offset();
                    bgRef[params[best.dir].fst] = ar.ct.offset();
                    bgRef[params[best.dir].snd] = {
                        left: elOffset.left + state.contentBox.x,
                        top: elOffset.top + state.contentBox.y
                    };
                }

                return best.result;
            },

            _setOptions: function (opts) {
                var newTheme,
                    oldTheme = this.options.theme,
                    ar = this._ui.arrow,
                    ret = this._super(opts);

                if (opts.arrow !== undefined) {
                    if (!ar && opts.arrow) {
                        this._ui.arrow = this._addArrow();

                        // Important to return here so we don't set the same options all over
                        // again below.
                        return;
                    } else if (ar && !opts.arrow) {
                        ar.arEls.remove();
                        this._ui.arrow = null;
                    }
                }

                // Reassign with potentially new arrow
                ar = this._ui.arrow;

                if (ar) {
                    if (opts.theme !== undefined) {
                        oldTheme = this._themeClassFromOption("ui-body-", oldTheme);
                        newTheme = this._themeClassFromOption("ui-body-", opts.theme);
                        ar.ar.removeClass(oldTheme).addClass(newTheme);
                    }

                    if (opts.shadow !== undefined) {
                        ar.ar.toggleClass("ui-overlay-shadow", opts.shadow);
                    }
                }

                return ret;
            },

            _destroy: function () {
                var ar = this._ui.arrow;

                if (ar) {
                    ar.arEls.remove();
                }

                return this._super();
            }
        });

    })(jQuery);
});
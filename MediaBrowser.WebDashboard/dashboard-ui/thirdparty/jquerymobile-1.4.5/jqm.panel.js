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

        $.widget("mobile.panel", {
            options: {
                animate: true,
                theme: null,
                position: "left",
                dismissible: true,
                display: "overlay", //accepts reveal, push, overlay
                swipeClose: true,
                positionFixed: true
            },

            _parentPage: null,
            _page: null,
            _modal: null,
            _panelInner: null,
            _wrapper: null,

            _create: function () {
                var el = this.element,
                    parentPage = el.closest(".ui-page, [data-role='page']");

                // expose some private props to other methods
                $.extend(this, {
                    _parentPage: (parentPage.length > 0) ? parentPage : false,
                    _openedPage: null,
                    _page: this._getPage,
                    _panelInner: this._getPanelInner()
                });
                if (this.options.display !== "overlay") {
                    this._getWrapper();
                }
                this._addPanelClasses();

                // if animating, add the class to do so
                if (!!this.options.animate) {
                    this.element.addClass("ui-panel-animate");
                }

                this._bindUpdateLayout();
                this._bindCloseEvents();
                this._bindLinkListeners();
                this._bindPageEvents();

                if (!!this.options.dismissible) {
                    this._createModal();
                }

                this._bindSwipeEvents();
            },

            _getPanelInner: function () {
                var panelInner = this.element[0].querySelector("." + "ui-panel-inner");
                if (!panelInner) {
                    panelInner = this.element.children().wrapAll("<div class='" + "ui-panel-inner" + "' />").parent();
                } else {
                    panelInner = $(panelInner);
                }

                return panelInner;
            },

            _createModal: function () {
                var self = this,
                    target = self._parentPage ? self._parentPage.parent() : self.element.parent();

                self._modal = $("<div class='" + "ui-panel-dismiss" + "'></div>")
                    .on("mousedown", function () {
                        self.close();
                    })
                    .appendTo(target);
            },

            _getPage: function () {
                var page = this._openedPage || this._parentPage || $(".ui-page-active");

                return page;
            },

            _getWrapper: function () {
                var wrapper = this._page().find("." + "ui-panel-wrapper");
                if (wrapper.length === 0) {
                    wrapper = this._page().children(".ui-header:not(.ui-header-fixed), .ui-content:not(.ui-popup), .ui-footer:not(.ui-footer-fixed)")
                        .wrapAll("<div class='" + "ui-panel-wrapper" + "'></div>")
                        .parent();
                }

                this._wrapper = wrapper;
            },

            _getPosDisplayClasses: function (prefix) {
                return prefix + "-position-right " + prefix + "-display-" + this.options.display;
            },

            _getPanelClasses: function () {
                var panelClasses = "ui-panel" +
                    " " + this._getPosDisplayClasses("ui-panel") +
                    " " + "ui-panel-closed" +
                    " " + "ui-body-" + (this.options.theme ? this.options.theme : "inherit");

                if (!!this.options.positionFixed) {
                    panelClasses += " " + "ui-panel-fixed";
                }

                return panelClasses;
            },

            _addPanelClasses: function () {
                this.element.addClass(this._getPanelClasses());
            },

            _handleCloseClick: function (event) {
                if (!event.isDefaultPrevented()) {
                    this.close();
                }
            },

            _bindCloseEvents: function () {
            },

            _positionPanel: function (scrollToTop) {
                var self = this,
                    panelInnerHeight = self._panelInner.outerHeight(),
                    expand = panelInnerHeight > (window.innerHeight || $(window).height());

                if (expand || !self.options.positionFixed) {
                    if (expand) {
                        self._unfixPanel();
                    }
                    if (scrollToTop) {
                        this.window[0].scrollTo(0, $.mobile.defaultHomeScroll);
                    }
                } else {
                    self._fixPanel();
                }
            },

            _bindFixListener: function () {
                this._on($(window), { "resize": "_positionPanel" });
            },

            _unbindFixListener: function () {
                this._off($(window), "resize");
            },

            _unfixPanel: function () {
                if (!!this.options.positionFixed) {
                    this.element.removeClass("ui-panel-fixed");
                }
            },

            _fixPanel: function () {
                if (!!this.options.positionFixed) {
                    this.element.addClass("ui-panel-fixed");
                }
            },

            _bindUpdateLayout: function () {
                var self = this;

                self.element.on("updatelayout", function (/* e */) {
                    if (self._open) {
                        self._positionPanel();
                    }
                });
            },

            _bindLinkListeners: function () {
                this._on("body", {
                    "click a": "_handleClick"
                });

            },

            _handleClick: function (e) {
                var link,
                    panelId = this.element.attr("id");

                if (e.currentTarget.href.split("#")[1] === panelId && panelId !== undefined) {

                    e.preventDefault();
                    link = $(e.target);
                    if (link.hasClass("ui-btn")) {
                        link.addClass($.mobile.activeBtnClass);
                        this.element.one("panelopen panelclose", function () {
                            link.removeClass($.mobile.activeBtnClass);
                        });
                    }
                    this.toggle();
                }
            },

            _bindSwipeEvents: function () {
                var self = this,
                    area = self._modal ? self.element.add(self._modal) : self.element;

                // on swipe, close the panel
                if (!!self.options.swipeClose) {
                    if (self.options.position === "left") {
                        area.on("swipeleft.panel", function (/* e */) {
                            self.close();
                        });
                    } else {
                        area.on("swiperight.panel", function (/* e */) {
                            self.close();
                        });
                    }
                }
            },

            _bindPageEvents: function () {
                var self = this;

                this.document
                    // Close the panel if another panel on the page opens
                    .on("panelbeforeopen", function (e) {
                        if (self._open && e.target !== self.element[0]) {
                            self.close();
                        }
                    })
                    // On escape, close? might need to have a target check too...
                    .on("keyup.panel", function (e) {
                        if (e.keyCode === 27 && self._open) {
                            self.close();
                        }
                    });
                if (!this._parentPage && this.options.display !== "overlay") {
                    this._on(this.document, {
                        "pageshow": function () {
                            this._openedPage = null;
                            this._getWrapper();
                        }
                    });
                }
                // Clean up open panels after page hide
                if (self._parentPage) {
                    this.document.on("pagehide", "[data-role='page']", function () {
                        if (self._open) {
                            self.close(true);
                        }
                    });
                } else {
                    this.document.on("pagebeforehide", function () {
                        if (self._open) {
                            self.close(true);
                        }
                    });
                }
            },

            // state storage of open or closed
            _open: false,
            _pageContentOpenClasses: null,
            _modalOpenClasses: null,

            open: function (immediate) {
                if (!this._open) {
                    var self = this,
                        o = self.options,

                        _openPanel = function () {
                            self._off(self.document, "panelclose");
                            self._page().data("panel", "open");

                            if (!!o.animate && o.display !== "overlay") {
                                self._wrapper.addClass("ui-panel-animate");
                            }

                            if (!immediate && !!o.animate) {
                                (self._wrapper || self.element)
                                    .animationComplete(complete, "transition");
                            } else {
                                setTimeout(complete, 0);
                            }

                            if (o.theme && o.display !== "overlay") {
                                self._page().parent()
                                    .addClass("ui-panel-page-container" + "-themed " + "ui-panel-page-container" + "-" + o.theme);
                            }

                            self.element
                                .removeClass("ui-panel-closed")
                                .addClass("ui-panel-open");

                            self._positionPanel(true);

                            self._pageContentOpenClasses = self._getPosDisplayClasses("ui-panel-page-content");

                            if (o.display !== "overlay") {
                                self._page().parent().addClass("ui-panel-page-container");
                                self._wrapper.addClass(self._pageContentOpenClasses);
                            }

                            self._modalOpenClasses = self._getPosDisplayClasses("ui-panel-dismiss") + " " + "ui-panel-dismiss-open";
                            if (self._modal) {
                                self._modal
                                    .addClass(self._modalOpenClasses)
                                    .height(Math.max(self._modal.height(), self.document.height()));
                            }
                        },
                        complete = function () {

                            // Bail if the panel was closed before the opening animation has completed
                            if (!self._open) {
                                return;
                            }

                            if (o.display !== "overlay") {
                                self._wrapper.addClass("ui-panel-page-content" + "-open");
                            }

                            self._bindFixListener();

                            self._trigger("open");

                            self._openedPage = self._page();
                        };

                    self._trigger("beforeopen");

                    if (self._page().data("panel") === "open") {
                        self._on(self.document, {
                            "panelclose": _openPanel
                        });
                    } else {
                        _openPanel();
                    }

                    self._open = true;
                }
            },

            close: function (immediate) {
                if (this._open) {
                    var self = this,
                        o = this.options,

                        _closePanel = function () {

                            self.element.removeClass("ui-panel-open");

                            if (o.display !== "overlay") {
                                self._wrapper.removeClass(self._pageContentOpenClasses);
                            }

                            if (!immediate && !!o.animate) {
                                (self._wrapper || self.element)
                                    .animationComplete(complete, "transition");
                            } else {
                                setTimeout(complete, 0);
                            }

                            if (self._modal) {
                                self._modal
                                    .removeClass(self._modalOpenClasses)
                                    .height("");
                            }
                        },
                        complete = function () {
                            if (o.theme && o.display !== "overlay") {
                                self._page().parent().removeClass("ui-panel-page-container" + "-themed " + "ui-panel-page-container" + "-" + o.theme);
                            }

                            self.element.addClass("ui-panel-closed");

                            if (o.display !== "overlay") {
                                self._page().parent().removeClass("ui-panel-page-container");
                                self._wrapper.removeClass("ui-panel-page-content" + "-open");
                            }

                            if (!!o.animate && o.display !== "overlay") {
                                self._wrapper.removeClass("ui-panel-animate");
                            }

                            self._fixPanel();
                            self._unbindFixListener();

                            self._page().removeData("panel");

                            self._trigger("close");

                            self._openedPage = null;
                        };

                    self._trigger("beforeclose");

                    _closePanel();

                    self._open = false;
                }
            },

            toggle: function () {
                this[this._open ? "close" : "open"]();
            },

            _destroy: function () {
                var otherPanels,
                o = this.options,
                multiplePanels = ($("body > :mobile-panel").length + $.mobile.activePage.find(":mobile-panel").length) > 1;

                if (o.display !== "overlay") {

                    //  remove the wrapper if not in use by another panel
                    otherPanels = $("body > :mobile-panel").add($.mobile.activePage.find(":mobile-panel"));
                    if (otherPanels.not(".ui-panel-display-overlay").not(this.element).length === 0) {
                        this._wrapper.children().unwrap();
                    }

                    if (this._open) {

                        this._page().parent().removeClass("ui-panel-page-container");

                        if (o.theme) {
                            this._page().parent().removeClass("ui-panel-page-container" + "-themed " + "ui-panel-page-container" + "-" + o.theme);
                        }
                    }
                }

                if (!multiplePanels) {

                    this.document.off("panelopen panelclose");

                }

                if (this._open) {
                    this._page().removeData("panel");
                }

                this._panelInner.children().unwrap();

                this.element
                    .removeClass([this._getPanelClasses(), "ui-panel-open", "ui-panel-animate"].join(" "))
                    .off("swipeleft.panel swiperight.panel")
                    .off("panelbeforeopen")
                    .off("panelhide")
                    .off("keyup.panel")
                    .off("updatelayout");

                if (this._modal) {
                    this._modal.remove();
                }
            }
        });

    })(jQuery);

});
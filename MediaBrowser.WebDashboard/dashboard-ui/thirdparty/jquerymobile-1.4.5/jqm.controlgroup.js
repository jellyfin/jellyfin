define(['jqmwidget'], function () {

    (function ($, undefined) {

        var uiScreenHiddenRegex = /\bui-screen-hidden\b/;
        function noHiddenClass(elements) {
            var index,
                length = elements.length,
                result = [];

            for (index = 0; index < length; index++) {
                if (!elements[index].className.match(uiScreenHiddenRegex)) {
                    result.push(elements[index]);
                }
            }

            return $(result);
        }

        $.mobile.behaviors.addFirstLastClasses = {
            _getVisibles: function ($els, create) {
                var visibles;

                if (create) {
                    visibles = noHiddenClass($els);
                } else {
                    visibles = $els.filter(":visible");
                    if (visibles.length === 0) {
                        visibles = noHiddenClass($els);
                    }
                }

                return visibles;
            },

            _addFirstLastClasses: function ($els, $visibles, create) {
                $els.removeClass("ui-first-child ui-last-child");
                $visibles.eq(0).addClass("ui-first-child").end().last().addClass("ui-last-child");
                if (!create) {
                    this.element.trigger("updatelayout");
                }
            },

            _removeFirstLastClasses: function ($els) {
                $els.removeClass("ui-first-child ui-last-child");
            }
        };

    })(jQuery);

    (function ($, undefined) {

        function keepNativeSelector() {
            var keepNative = $.trim("[data-role='none']"),
        globalValue = $.trim($.mobile.keepNative),
        optionValue = $.trim("[data-role='none']"),

        // Check if $.mobile.keepNative has changed from the factory default
        newDefault = "",

        // If $.mobile.keepNative has not changed, use options.keepNativeDefault
        oldDefault = (newDefault === "" ? optionValue : "");

            // Concatenate keepNative selectors from all sources where the value has
            // changed or, if nothing has changed, return the default
            return ((keepNative ? [keepNative] : [])
                .concat(newDefault ? [newDefault] : [])
                .concat(oldDefault ? [oldDefault] : [])
                .join(", "));

        }

        $.widget("mobile.controlgroup", $.extend({
            options: {
                enhanced: false,
                theme: null,
                shadow: false,
                corners: true,
                excludeInvisible: true,
                type: "vertical",
                mini: false
            },

            _create: function () {
                var elem = this.element,
                    opts = this.options,
                    keepNative = keepNativeSelector();

                // Run buttonmarkup
                if ($.fn.buttonMarkup) {
                    this.element
                        .find($.fn.buttonMarkup.initSelector)
                        .not(keepNative)
                        .buttonMarkup();
                }
                // Enhance child widgets
                $.each(this._childWidgets, $.proxy(function (number, widgetName) {
                    if ($.mobile[widgetName]) {
                        this.element
                            .find($.mobile[widgetName].initSelector)
                            .not(keepNative)[widgetName]();
                    }
                }, this));

                $.extend(this, {
                    _ui: null,
                    _initialRefresh: true
                });

                if (opts.enhanced) {
                    this._ui = {
                        groupLegend: elem.children(".ui-controlgroup-label").children(),
                        childWrapper: elem.children(".ui-controlgroup-controls")
                    };
                } else {
                    this._ui = this._enhance();
                }

            },

            _childWidgets: ["checkboxradio", "selectmenu", "button"],

            _themeClassFromOption: function (value) {
                return (value ? (value === "none" ? "" : "ui-group-theme-" + value) : "");
            },

            _enhance: function () {
                var elem = this.element,
                    opts = this.options,
                    ui = {
                        groupLegend: elem.children("legend"),
                        childWrapper: elem
                            .addClass("ui-controlgroup " +
                                "ui-controlgroup-" +
                                    (opts.type === "horizontal" ? "horizontal" : "vertical") + " " +
                                this._themeClassFromOption(opts.theme) + " " +
                                (opts.corners ? "ui-corner-all " : "") +
                                (opts.mini ? "ui-mini " : ""))
                            .wrapInner("<div " +
                                "class='ui-controlgroup-controls " +
                                    (opts.shadow === true ? "ui-shadow" : "") + "'></div>")
                            .children()
                    };

                if (ui.groupLegend.length > 0) {
                    $("<div role='heading' class='ui-controlgroup-label'></div>")
                        .append(ui.groupLegend)
                        .prependTo(elem);
                }

                return ui;
            },

            _init: function () {
                this.refresh();
            },

            _setOptions: function (options) {
                var callRefresh, returnValue,
                    elem = this.element;

                // Must have one of horizontal or vertical
                if (options.type !== undefined) {
                    elem
                        .removeClass("ui-controlgroup-horizontal ui-controlgroup-vertical")
                        .addClass("ui-controlgroup-" + (options.type === "horizontal" ? "horizontal" : "vertical"));
                    callRefresh = true;
                }

                if (options.theme !== undefined) {
                    elem
                        .removeClass(this._themeClassFromOption(this.options.theme))
                        .addClass(this._themeClassFromOption(options.theme));
                }

                if (options.corners !== undefined) {
                    elem.toggleClass("ui-corner-all", options.corners);
                }

                if (options.mini !== undefined) {
                    elem.toggleClass("ui-mini", options.mini);
                }

                if (options.shadow !== undefined) {
                    this._ui.childWrapper.toggleClass("ui-shadow", options.shadow);
                }

                if (options.excludeInvisible !== undefined) {
                    this.options.excludeInvisible = options.excludeInvisible;
                    callRefresh = true;
                }

                returnValue = this._super(options);

                if (callRefresh) {
                    this.refresh();
                }

                return returnValue;
            },

            container: function () {
                return this._ui.childWrapper;
            },

            refresh: function () {
                var $el = this.container(),
                    els = $el.find(".ui-btn").not(".ui-slider-handle"),
                    create = this._initialRefresh;
                if ($.mobile.checkboxradio) {
                    $el.find(":mobile-checkboxradio").checkboxradio("refresh");
                }
                this._addFirstLastClasses(els,
                    this.options.excludeInvisible ? this._getVisibles(els, create) : els,
                    create);
                this._initialRefresh = false;
            },

            // Caveat: If the legend is not the first child of the controlgroup at enhance
            // time, it will be after _destroy().
            _destroy: function () {
                var ui, buttons,
                    opts = this.options;

                if (opts.enhanced) {
                    return this;
                }

                ui = this._ui;
                buttons = this.element
                    .removeClass("ui-controlgroup " +
                        "ui-controlgroup-horizontal ui-controlgroup-vertical ui-corner-all ui-mini " +
                        this._themeClassFromOption(opts.theme))
                    .find(".ui-btn")
                    .not(".ui-slider-handle");

                this._removeFirstLastClasses(buttons);

                ui.groupLegend.unwrap();
                ui.childWrapper.children().unwrap();
            }
        }, $.mobile.behaviors.addFirstLastClasses));

    })(jQuery);

});
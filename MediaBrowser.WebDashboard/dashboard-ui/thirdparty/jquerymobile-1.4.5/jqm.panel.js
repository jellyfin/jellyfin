(function () {

    if (jQuery.widget) {
        return;
    }

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

                this.element[0].dispatchEvent(new CustomEvent(event.type, {
                    bubbles: true,
                    detail: {
                        data: data,
                        originalEvent: event
                    }
                }));

                //this.element.trigger(event, data);
                return !($.isFunction(callback) &&
                    callback.apply(this.element[0], [event].concat(data)) === false ||
                    event.isDefaultPrevented());
            }
        };

    })(jQuery);

    (function ($, undefined) {

        $.extend($.Widget.prototype, {
            _getCreateOptions: function () {

                var option, value,
                    elem = this.element[0],
                    options = {};

                //
                if (!this.element.data("defaults")) {
                    for (option in this.options) {

                        value = this.element.data(option);

                        if (value != null) {
                            options[option] = value;
                        }
                    }
                }

                return options;
            }
        });

    })(jQuery);

    (function ($, undefined) {


        var originalWidget = $.widget

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

    })(jQuery);


})();

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
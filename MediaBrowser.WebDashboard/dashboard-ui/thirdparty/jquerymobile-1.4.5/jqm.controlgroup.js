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
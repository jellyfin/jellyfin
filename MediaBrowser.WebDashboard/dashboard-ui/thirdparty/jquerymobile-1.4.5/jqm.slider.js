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

(function ($, window, undefined) {
    var rbrace = /(?:\{[\s\S]*\}|\[[\s\S]*\])$/;

    $.extend($.mobile, {

        // Namespace used framework-wide for data-attrs. Default is no namespace

        // Retrieve an attribute from an element and perform some massaging of the value

        getAttribute: function (element, key) {
            var data;

            element = element.jquery ? element[0] : element;

            if (element && element.getAttribute) {
                data = element.getAttribute("data-" + key);
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
        }

    });

})(jQuery, this);

(function ($, undefined) {

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

    $.widget("mobile.slider", $.extend({
        initSelector: "input[type='range']:not([data-role='none'])",

        widgetEventPrefix: "slide",

        options: {
            theme: null,
            trackTheme: null,
            corners: true,
            mini: false,
            highlight: false
        },

        _create: function () {

            // TODO: Each of these should have comments explain what they're for
            var self = this,
                control = this.element,
                trackTheme = this.options.trackTheme || $.mobile.getAttribute(control[0], "theme"),
                trackThemeClass = trackTheme ? " ui-bar-" + trackTheme : " ui-bar-inherit",
                cornerClass = (this.options.corners || control.data("corners")) ? " ui-corner-all" : "",
                miniClass = (this.options.mini || control.data("mini")) ? " ui-mini" : "",
                cType = control[0].nodeName.toLowerCase(),
                isToggleSwitch = (cType === "select"),
                isRangeslider = control.parent().is("[data-role='rangeslider']"),
                selectClass = (isToggleSwitch) ? "ui-slider-switch" : "",
                controlID = control.attr("id"),
                $label = $("[for='" + controlID + "']"),
                labelID = $label.attr("id") || controlID + "-label",
                min = !isToggleSwitch ? parseFloat(control.attr("min")) : 0,
                max = !isToggleSwitch ? parseFloat(control.attr("max")) : control.find("option").length - 1,
                step = window.parseFloat(control.attr("step") || 1),
                domHandle = document.createElement("a"),
                handle = $(domHandle),
                domSlider = document.createElement("div"),
                slider = $(domSlider),
                valuebg = this.options.highlight && !isToggleSwitch ? (function () {
                    var bg = document.createElement("div");
                    bg.className = "ui-slider-bg " + $.mobile.activeBtnClass;
                    return $(bg).prependTo(slider);
                })() : false,
                options,
                wrapper,
                j, length,
                i, optionsCount, origTabIndex,
                side, activeClass, sliderImg;

            $label.attr("id", labelID);
            this.isToggleSwitch = isToggleSwitch;

            domHandle.setAttribute("href", "#");
            domSlider.setAttribute("role", "application");
            domSlider.className = [this.isToggleSwitch ? "ui-slider ui-slider-track ui-shadow-inset " : "ui-slider-track ui-shadow-inset ", selectClass, trackThemeClass, cornerClass, miniClass].join("");
            domHandle.className = "ui-slider-handle";
            domSlider.appendChild(domHandle);

            handle.attr({
                "role": "slider",
                "aria-valuemin": min,
                "aria-valuemax": max,
                "aria-valuenow": this._value(),
                "aria-valuetext": this._value(),
                "title": this._value(),
                "aria-labelledby": labelID
            });

            $.extend(this, {
                slider: slider,
                handle: handle,
                control: control,
                type: cType,
                step: step,
                max: max,
                min: min,
                valuebg: valuebg,
                isRangeslider: isRangeslider,
                dragging: false,
                beforeStart: null,
                userModified: false,
                mouseMoved: false
            });

            if (isToggleSwitch) {
                // TODO: restore original tabindex (if any) in a destroy method
                origTabIndex = control.attr("tabindex");
                if (origTabIndex) {
                    handle.attr("tabindex", origTabIndex);
                }
                control.attr("tabindex", "-1").focus(function () {
                    $(this).blur();
                    handle.focus();
                });

                wrapper = document.createElement("div");
                wrapper.className = "ui-slider-inneroffset";

                for (j = 0, length = domSlider.childNodes.length; j < length; j++) {
                    wrapper.appendChild(domSlider.childNodes[j]);
                }

                domSlider.appendChild(wrapper);

                // slider.wrapInner( "<div class='ui-slider-inneroffset'></div>" );

                // make the handle move with a smooth transition
                handle.addClass("ui-slider-handle-snapping");

                options = control.find("option");

                for (i = 0, optionsCount = options.length; i < optionsCount; i++) {
                    side = !i ? "b" : "a";
                    activeClass = !i ? "" : " " + $.mobile.activeBtnClass;
                    sliderImg = document.createElement("span");

                    sliderImg.className = ["ui-slider-label ui-slider-label-", side, activeClass].join("");
                    sliderImg.setAttribute("role", "img");
                    sliderImg.appendChild(document.createTextNode(options[i].innerHTML));
                    $(sliderImg).prependTo(slider);
                }

                self._labels = $(".ui-slider-label", slider);

            }

            // monitor the input for updated values
            control.addClass(isToggleSwitch ? "ui-slider-switch" : "ui-slider-input");

            this._on(control, {
                "change": "_controlChange",
                "keyup": "_controlKeyup",
                "blur": "_controlBlur",
                "mouseup": "_controlVMouseUp"
            });

            slider.bind("mousedown", $.proxy(this._sliderVMouseDown, this))
                .bind("click", false);

            // We have to instantiate a new function object for the unbind to work properly
            // since the method itself is defined in the prototype (causing it to unbind everything)
            this._on(document, { "mousemove": "_preventDocumentDrag" });
            this._on(slider.add(document), { "mouseup": "_sliderVMouseUp" });

            slider.insertAfter(control);

            // wrap in a div for styling purposes
            if (!isToggleSwitch && !isRangeslider) {
                wrapper = this.options.mini ? "<div class='ui-slider ui-mini'>" : "<div class='ui-slider'>";

                control.add(slider).wrapAll(wrapper);
            }

            // bind the handle event callbacks and set the context to the widget instance
            this._on(this.handle, {
                "mousedown": "_handleVMouseDown",
                "keydown": "_handleKeydown",
                "keyup": "_handleKeyup"
            });

            this.handle.bind("click", false);

            //this._handleFormReset();

            this.refresh(undefined, undefined, true);
        },

        _setOptions: function (options) {
            if (options.theme !== undefined) {
                this._setTheme(options.theme);
            }

            if (options.trackTheme !== undefined) {
                this._setTrackTheme(options.trackTheme);
            }

            if (options.corners !== undefined) {
                this._setCorners(options.corners);
            }

            if (options.mini !== undefined) {
                this._setMini(options.mini);
            }

            if (options.highlight !== undefined) {
                this._setHighlight(options.highlight);
            }

            if (options.disabled !== undefined) {
                this._setDisabled(options.disabled);
            }
            this._super(options);
        },

        _controlChange: function (event) {
            // if the user dragged the handle, the "change" event was triggered from inside refresh(); don't call refresh() again
            if (this._trigger("controlchange", event) === false) {
                return false;
            }
            if (!this.mouseMoved) {
                this.refresh(this._value(), true);
            }
        },

        _controlKeyup: function (/* event */) { // necessary?
            this.refresh(this._value(), true, true);
        },

        _controlBlur: function (/* event */) {
            this.refresh(this._value(), true);
        },

        // it appears the clicking the up and down buttons in chrome on
        // range/number inputs doesn't trigger a change until the field is
        // blurred. Here we check thif the value has changed and refresh
        _controlVMouseUp: function (/* event */) {
            this._checkedRefresh();
        },

        // NOTE force focus on handle
        _handleVMouseDown: function (/* event */) {
            this.handle.focus();
        },

        _handleKeydown: function (event) {
            var index = this._value();
            if (this.options.disabled) {
                return;
            }

            // In all cases prevent the default and mark the handle as active
            switch (event.keyCode) {
                case $.ui.keyCode.HOME:
                case $.ui.keyCode.END:
                case $.ui.keyCode.PAGE_UP:
                case $.ui.keyCode.PAGE_DOWN:
                case $.ui.keyCode.UP:
                case $.ui.keyCode.RIGHT:
                case $.ui.keyCode.DOWN:
                case $.ui.keyCode.LEFT:
                    event.preventDefault();

                    if (!this._keySliding) {
                        this._keySliding = true;
                        this.handle.addClass("ui-state-active"); /* TODO: We don't use this class for styling. Do we need to add it? */
                    }

                    break;
            }

            // move the slider according to the keypress
            switch (event.keyCode) {
                case $.ui.keyCode.HOME:
                    this.refresh(this.min);
                    break;
                case $.ui.keyCode.END:
                    this.refresh(this.max);
                    break;
                case $.ui.keyCode.PAGE_UP:
                case $.ui.keyCode.UP:
                case $.ui.keyCode.RIGHT:
                    this.refresh(index + this.step);
                    break;
                case $.ui.keyCode.PAGE_DOWN:
                case $.ui.keyCode.DOWN:
                case $.ui.keyCode.LEFT:
                    this.refresh(index - this.step);
                    break;
            }
        }, // remove active mark

        _handleKeyup: function (/* event */) {
            if (this._keySliding) {
                this._keySliding = false;
                this.handle.removeClass("ui-state-active"); /* See comment above. */
            }
        },

        _sliderVMouseDown: function (event) {
            // NOTE: we don't do this in refresh because we still want to
            //       support programmatic alteration of disabled inputs
            if (this.options.disabled || !(event.which === 1 || event.which === 0 || event.which === undefined)) {
                return false;
            }
            if (this._trigger("beforestart", event) === false) {
                return false;
            }
            this.dragging = true;
            this.userModified = false;
            this.mouseMoved = false;

            if (this.isToggleSwitch) {
                this.beforeStart = this.element[0].selectedIndex;
            }

            this.refresh(event);
            this._trigger("start");
            return false;
        },

        _sliderVMouseUp: function () {
            if (this.dragging) {
                this.dragging = false;

                if (this.isToggleSwitch) {
                    // make the handle move with a smooth transition
                    this.handle.addClass("ui-slider-handle-snapping");

                    if (this.mouseMoved) {
                        // this is a drag, change the value only if user dragged enough
                        if (this.userModified) {
                            this.refresh(this.beforeStart === 0 ? 1 : 0);
                        } else {
                            this.refresh(this.beforeStart);
                        }
                    } else {
                        // this is just a click, change the value
                        this.refresh(this.beforeStart === 0 ? 1 : 0);
                    }
                }

                this.mouseMoved = false;
                this._trigger("stop");
                return false;
            }
        },

        _preventDocumentDrag: function (event) {
            // NOTE: we don't do this in refresh because we still want to
            //       support programmatic alteration of disabled inputs
            if (this._trigger("drag", event) === false) {
                return false;
            }
            if (this.dragging && !this.options.disabled) {

                // this.mouseMoved must be updated before refresh() because it will be used in the control "change" event
                this.mouseMoved = true;

                if (this.isToggleSwitch) {
                    // make the handle move in sync with the mouse
                    this.handle.removeClass("ui-slider-handle-snapping");
                }

                this.refresh(event);

                // only after refresh() you can calculate this.userModified
                this.userModified = this.beforeStart !== this.element[0].selectedIndex;
                return false;
            }
        },

        _checkedRefresh: function () {
            if (this.value !== this._value()) {
                this.refresh(this._value());
            }
        },

        _value: function () {
            return this.isToggleSwitch ? this.element[0].selectedIndex : parseFloat(this.element.val());
        },

        _reset: function () {
            this.refresh(undefined, false, true);
        },

        refresh: function (val, isfromControl, preventInputUpdate) {
            // NOTE: we don't return here because we want to support programmatic
            //       alteration of the input value, which should still update the slider

            var self = this,
                parentTheme = $.mobile.getAttribute(this.element[0], "theme"),
                theme = this.options.theme || parentTheme,
                themeClass = theme ? " ui-btn-" + theme : "",
                trackTheme = this.options.trackTheme || parentTheme,
                trackThemeClass = trackTheme ? " ui-bar-" + trackTheme : " ui-bar-inherit",
                cornerClass = this.options.corners ? " ui-corner-all" : "",
                miniClass = this.options.mini ? " ui-mini" : "",
                left, width, data, tol,
                pxStep, percent,
                control, isInput, optionElements, min, max, step,
                newval, valModStep, alignValue, percentPerStep,
                handlePercent, aPercent, bPercent,
                valueChanged;

            self.slider[0].className = [this.isToggleSwitch ? "ui-slider ui-slider-switch ui-slider-track ui-shadow-inset" : "ui-slider-track ui-shadow-inset", trackThemeClass, cornerClass, miniClass].join("");
            if (this.options.disabled || this.element.prop("disabled")) {
                this.disable();
            }

            // set the stored value for comparison later
            this.value = this._value();
            if (this.options.highlight && !this.isToggleSwitch && this.slider.find(".ui-slider-bg").length === 0) {
                this.valuebg = (function () {
                    var bg = document.createElement("div");
                    bg.className = "ui-slider-bg " + $.mobile.activeBtnClass;
                    return $(bg).prependTo(self.slider);
                })();
            }
            this.handle.addClass("ui-btn" + themeClass + " ui-shadow");

            control = this.element;
            isInput = !this.isToggleSwitch;
            optionElements = isInput ? [] : control.find("option");
            min = isInput ? parseFloat(control.attr("min")) : 0;
            max = isInput ? parseFloat(control.attr("max")) : optionElements.length - 1;
            step = (isInput && parseFloat(control.attr("step")) > 0) ? parseFloat(control.attr("step")) : 1;

            if (typeof val === "object") {
                data = val;
                // a slight tolerance helped get to the ends of the slider
                tol = 8;

                left = this.slider.offset().left;
                width = this.slider.width();
                pxStep = width / ((max - min) / step);
                if (!this.dragging ||
                        data.pageX < left - tol ||
                        data.pageX > left + width + tol) {
                    return;
                }
                if (pxStep > 1) {
                    percent = ((data.pageX - left) / width) * 100;
                } else {
                    percent = Math.round(((data.pageX - left) / width) * 100);
                }
            } else {
                if (val == null) {
                    val = isInput ? parseFloat(control.val() || 0) : control[0].selectedIndex;
                }
                percent = (parseFloat(val) - min) / (max - min) * 100;
            }

            if (isNaN(percent)) {
                return;
            }

            newval = (percent / 100) * (max - min) + min;

            //from jQuery UI slider, the following source will round to the nearest step
            valModStep = (newval - min) % step;
            alignValue = newval - valModStep;

            if (Math.abs(valModStep) * 2 >= step) {
                alignValue += (valModStep > 0) ? step : (-step);
            }

            percentPerStep = 100 / ((max - min) / step);
            // Since JavaScript has problems with large floats, round
            // the final value to 5 digits after the decimal point (see jQueryUI: #4124)
            newval = parseFloat(alignValue.toFixed(5));

            if (typeof pxStep === "undefined") {
                pxStep = width / ((max - min) / step);
            }
            if (pxStep > 1 && isInput) {
                percent = (newval - min) * percentPerStep * (1 / step);
            }
            if (percent < 0) {
                percent = 0;
            }

            if (percent > 100) {
                percent = 100;
            }

            if (newval < min) {
                newval = min;
            }

            if (newval > max) {
                newval = max;
            }

            this.handle.css("left", percent + "%");

            this.handle[0].setAttribute("aria-valuenow", isInput ? newval : optionElements.eq(newval).attr("value"));

            this.handle[0].setAttribute("aria-valuetext", isInput ? newval : optionElements.eq(newval).text());

            this.handle[0].setAttribute("title", isInput ? newval : optionElements.eq(newval).text());

            if (this.valuebg) {
                this.valuebg.css("width", percent + "%");
            }

            // drag the label widths
            if (this._labels) {
                handlePercent = this.handle.width() / this.slider.width() * 100;
                aPercent = percent && handlePercent + (100 - handlePercent) * percent / 100;
                bPercent = percent === 100 ? 0 : Math.min(handlePercent + 100 - aPercent, 100);

                this._labels.each(function () {
                    var ab = $(this).hasClass("ui-slider-label-a");
                    $(this).width((ab ? aPercent : bPercent) + "%");
                });
            }

            if (!preventInputUpdate) {
                valueChanged = false;

                // update control"s value
                if (isInput) {
                    valueChanged = parseFloat(control.val()) !== newval;
                    control.val(newval);
                } else {
                    valueChanged = control[0].selectedIndex !== newval;
                    control[0].selectedIndex = newval;
                }
                if (this._trigger("beforechange", val) === false) {
                    return false;
                }
                if (!isfromControl && valueChanged) {
                    control.trigger("change");
                }
            }
        },

        _setHighlight: function (value) {
            value = !!value;
            if (value) {
                this.options.highlight = !!value;
                this.refresh();
            } else if (this.valuebg) {
                this.valuebg.remove();
                this.valuebg = false;
            }
        },

        _setTheme: function (value) {
            this.handle
                .removeClass("ui-btn-" + this.options.theme)
                .addClass("ui-btn-" + value);

            var currentTheme = this.options.theme ? this.options.theme : "inherit",
                newTheme = value ? value : "inherit";

            this.control
                .removeClass("ui-body-" + currentTheme)
                .addClass("ui-body-" + newTheme);
        },

        _setTrackTheme: function (value) {
            var currentTrackTheme = this.options.trackTheme ? this.options.trackTheme : "inherit",
                newTrackTheme = value ? value : "inherit";

            this.slider
                .removeClass("ui-body-" + currentTrackTheme)
                .addClass("ui-body-" + newTrackTheme);
        },

        _setMini: function (value) {
            value = !!value;
            if (!this.isToggleSwitch && !this.isRangeslider) {
                this.slider.parent().toggleClass("ui-mini", value);
                this.element.toggleClass("ui-mini", value);
            }
            this.slider.toggleClass("ui-mini", value);
        },

        _setCorners: function (value) {
            this.slider.toggleClass("ui-corner-all", value);

            if (!this.isToggleSwitch) {
                this.control.toggleClass("ui-corner-all", value);
            }
        },

        _setDisabled: function (value) {
            value = !!value;
            this.element.prop("disabled", value);
            this.slider
                .toggleClass("ui-state-disabled", value)
                .attr("aria-disabled", value);

            this.element.toggleClass("ui-state-disabled", value);
        }

    }, $.mobile.behaviors.formReset));

})(jQuery);

(function ($, undefined) {
    $.widget("mobile.rangeslider", $.extend({

        options: {
            theme: null,
            trackTheme: null,
            corners: true,
            mini: false,
            highlight: true
        },

        _create: function () {
            var $el = this.element,
			elClass = this.options.mini ? "ui-rangeslider ui-mini" : "ui-rangeslider",
			_inputFirst = $el.find("input").first(),
			_inputLast = $el.find("input").last(),
			_label = $el.find("label").first(),
			_sliderWidgetFirst = $.data(_inputFirst.get(0), "mobile-slider") ||
				$.data(_inputFirst.slider().get(0), "mobile-slider"),
			_sliderWidgetLast = $.data(_inputLast.get(0), "mobile-slider") ||
				$.data(_inputLast.slider().get(0), "mobile-slider"),
			_sliderFirst = _sliderWidgetFirst.slider,
			_sliderLast = _sliderWidgetLast.slider,
			firstHandle = _sliderWidgetFirst.handle,
			_sliders = $("<div class='ui-rangeslider-sliders' />").appendTo($el);

            _inputFirst.addClass("ui-rangeslider-first");
            _inputLast.addClass("ui-rangeslider-last");
            $el.addClass(elClass);

            _sliderFirst.appendTo(_sliders);
            _sliderLast.appendTo(_sliders);
            _label.insertBefore($el);
            firstHandle.prependTo(_sliderLast);

            $.extend(this, {
                _inputFirst: _inputFirst,
                _inputLast: _inputLast,
                _sliderFirst: _sliderFirst,
                _sliderLast: _sliderLast,
                _label: _label,
                _targetVal: null,
                _sliderTarget: false,
                _sliders: _sliders,
                _proxy: false
            });

            this.refresh();
            this._on(this.element.find("input.ui-slider-input"), {
                "slidebeforestart": "_slidebeforestart",
                "slidestop": "_slidestop",
                "slidedrag": "_slidedrag",
                "slidebeforechange": "_change",
                "blur": "_change",
                "keyup": "_change"
            });
            this._on({
                "mousedown": "_change"
            });
            this._on(this.element.closest("form"), {
                "reset": "_handleReset"
            });
            this._on(firstHandle, {
                "mousedown": "_dragFirstHandle"
            });
        },
        _handleReset: function () {
            var self = this;
            //we must wait for the stack to unwind before updateing other wise sliders will not have updated yet
            setTimeout(function () {
                self._updateHighlight();
            }, 0);
        },

        _dragFirstHandle: function (event) {
            //if the first handle is dragged send the event to the first slider
            $.data(this._inputFirst.get(0), "mobile-slider").dragging = true;
            $.data(this._inputFirst.get(0), "mobile-slider").refresh(event);
            $.data(this._inputFirst.get(0), "mobile-slider")._trigger("start");
            return false;
        },

        _slidedrag: function (event) {
            var first = $(event.target).is(this._inputFirst),
				otherSlider = (first) ? this._inputLast : this._inputFirst;

            this._sliderTarget = false;
            //if the drag was initiated on an extreme and the other handle is focused send the events to
            //the closest handle
            if ((this._proxy === "first" && first) || (this._proxy === "last" && !first)) {
                $.data(otherSlider.get(0), "mobile-slider").dragging = true;
                $.data(otherSlider.get(0), "mobile-slider").refresh(event);
                return false;
            }
        },

        _slidestop: function (event) {
            var first = $(event.target).is(this._inputFirst);

            this._proxy = false;
            //this stops dragging of the handle and brings the active track to the front
            //this makes clicks on the track go the the last handle used
            this.element.find("input").trigger("mouseup");
            this._sliderFirst.css("z-index", first ? 1 : "");
        },

        _slidebeforestart: function (event) {
            this._sliderTarget = false;
            //if the track is the target remember this and the original value
            if ($(event.originalEvent.target).hasClass("ui-slider-track")) {
                this._sliderTarget = true;
                this._targetVal = $(event.target).val();
            }
        },

        _setOptions: function (options) {
            if (options.theme !== undefined) {
                this._setTheme(options.theme);
            }

            if (options.trackTheme !== undefined) {
                this._setTrackTheme(options.trackTheme);
            }

            if (options.mini !== undefined) {
                this._setMini(options.mini);
            }

            if (options.highlight !== undefined) {
                this._setHighlight(options.highlight);
            }

            if (options.disabled !== undefined) {
                this._setDisabled(options.disabled);
            }

            this._super(options);
            this.refresh();
        },

        refresh: function () {
            var $el = this.element,
				o = this.options;

            if (this._inputFirst.is(":disabled") || this._inputLast.is(":disabled")) {
                this.options.disabled = true;
            }

            $el.find("input").slider({
                theme: o.theme,
                trackTheme: o.trackTheme,
                disabled: o.disabled,
                corners: o.corners,
                mini: o.mini,
                highlight: o.highlight
            }).slider("refresh");
            this._updateHighlight();
        },

        _change: function (event) {
            if (event.type === "keyup") {
                this._updateHighlight();
                return false;
            }

            var self = this,
				min = parseFloat(this._inputFirst.val(), 10),
				max = parseFloat(this._inputLast.val(), 10),
				first = $(event.target).hasClass("ui-rangeslider-first"),
				thisSlider = first ? this._inputFirst : this._inputLast,
				otherSlider = first ? this._inputLast : this._inputFirst;

            if ((this._inputFirst.val() > this._inputLast.val() && event.type === "mousedown" && !$(event.target).hasClass("ui-slider-handle"))) {
                thisSlider.blur();
            } else if (event.type === "mousedown") {
                return;
            }
            if (min > max && !this._sliderTarget) {
                //this prevents min from being greater then max
                thisSlider.val(first ? max : min).slider("refresh");
                this._trigger("normalize");
            } else if (min > max) {
                //this makes it so clicks on the target on either extreme go to the closest handle
                thisSlider.val(this._targetVal).slider("refresh");

                //You must wait for the stack to unwind so first slider is updated before updating second
                setTimeout(function () {
                    otherSlider.val(first ? min : max).slider("refresh");
                    $.data(otherSlider.get(0), "mobile-slider").handle.focus();
                    self._sliderFirst.css("z-index", first ? "" : 1);
                    self._trigger("normalize");
                }, 0);
                this._proxy = (first) ? "first" : "last";
            }
            //fixes issue where when both _sliders are at min they cannot be adjusted
            if (min === max) {
                $.data(thisSlider.get(0), "mobile-slider").handle.css("z-index", 1);
                $.data(otherSlider.get(0), "mobile-slider").handle.css("z-index", 0);
            } else {
                $.data(otherSlider.get(0), "mobile-slider").handle.css("z-index", "");
                $.data(thisSlider.get(0), "mobile-slider").handle.css("z-index", "");
            }

            this._updateHighlight();

            if (min >= max) {
                return false;
            }
        },

        _updateHighlight: function () {
            var min = parseInt($.data(this._inputFirst.get(0), "mobile-slider").handle.get(0).style.left, 10),
				max = parseInt($.data(this._inputLast.get(0), "mobile-slider").handle.get(0).style.left, 10),
				width = (max - min);

            this.element.find(".ui-slider-bg").css({
                "margin-left": min + "%",
                "width": width + "%"
            });
        },

        _setTheme: function (value) {
            this._inputFirst.slider("option", "theme", value);
            this._inputLast.slider("option", "theme", value);
        },

        _setTrackTheme: function (value) {
            this._inputFirst.slider("option", "trackTheme", value);
            this._inputLast.slider("option", "trackTheme", value);
        },

        _setMini: function (value) {
            this._inputFirst.slider("option", "mini", value);
            this._inputLast.slider("option", "mini", value);
            this.element.toggleClass("ui-mini", !!value);
        },

        _setHighlight: function (value) {
            this._inputFirst.slider("option", "highlight", value);
            this._inputLast.slider("option", "highlight", value);
        },

        _setDisabled: function (value) {
            this._inputFirst.prop("disabled", value);
            this._inputLast.prop("disabled", value);
        },

        _destroy: function () {
            this._label.prependTo(this.element);
            this.element.removeClass("ui-rangeslider ui-mini");
            this._inputFirst.after(this._sliderFirst);
            this._inputLast.after(this._sliderLast);
            this._sliders.remove();
            this.element.find("input").removeClass("ui-rangeslider-first ui-rangeslider-last").slider("destroy");
        }

    }, $.mobile.behaviors.formReset));

})(jQuery);


(function ($, undefined) {

    var popup;

    function getPopup() {
        if (!popup) {
            popup = $("<div></div>", {
                "class": "ui-slider-popup ui-shadow ui-corner-all"
            });
        }
        return popup.clone();
    }

    $.widget("mobile.slider", $.mobile.slider, {
        options: {
            popupEnabled: false,
            showValue: false
        },

        _create: function () {
            this._super();

            $.extend(this, {
                _currentValue: null,
                _popup: null,
                _popupVisible: false
            });

            this._setOption("popupEnabled", this.options.popupEnabled);

            this._on(this.handle, { "mousedown": "_showPopup" });
            this._on(this.slider.add(this.document), { "mouseup": "_hidePopup" });
            this._refresh();
        },

        // position the popup centered 5px above the handle
        _positionPopup: function () {
            var dstOffset = this.handle.offset();

            this._popup.offset({
                left: dstOffset.left + (this.handle.width() - this._popup.width()) / 2,
                top: dstOffset.top - this._popup.outerHeight() - 5
            });
        },

        _setOption: function (key, value) {
            this._super(key, value);

            if (key === "showValue") {
                this.handle.html(value && !this.options.mini ? this._value() : "");
            } else if (key === "popupEnabled") {
                if (value && !this._popup) {
                    this._popup = getPopup()
                        .addClass("ui-body-" + (this.options.theme || "a"))
                        .hide()
                        .insertBefore(this.element);
                }
            }
        },

        // show value on the handle and in popup
        refresh: function () {
            this._super.apply(this, arguments);
            this._refresh();
        },

        _refresh: function () {
            var o = this.options, newValue;

            if (o.popupEnabled) {
                // remove the title attribute from the handle (which is
                // responsible for the annoying tooltip); NB we have
                // to do it here as the jqm slider sets it every time
                // the slider's value changes :(
                this.handle.removeAttr("title");
            }

            newValue = this._value();
            if (newValue === this._currentValue) {
                return;
            }
            this._currentValue = newValue;

            if (o.popupEnabled && this._popup) {
                this._positionPopup();
                this._popup.html(newValue);
            }

            if (o.showValue && !this.options.mini) {
                this.handle.html(newValue);
            }
        },

        _showPopup: function () {
            if (this.options.popupEnabled && !this._popupVisible) {
                this.handle.html("");
                this._popup.show();
                this._positionPopup();
                this._popupVisible = true;
            }
        },

        _hidePopup: function () {
            var o = this.options;

            if (o.popupEnabled && this._popupVisible) {
                if (o.showValue && !o.mini) {
                    this.handle.html(this._value());
                }
                this._popup.hide();
                this._popupVisible = false;
            }
        }
    });

})(jQuery);

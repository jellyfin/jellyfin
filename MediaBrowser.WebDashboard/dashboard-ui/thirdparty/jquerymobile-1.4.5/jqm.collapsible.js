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

    var rInitialLetter = /([A-Z])/g,

        // Construct iconpos class from iconpos value
        iconposClass = function (iconpos) {
            return ("ui-btn-icon-" + (iconpos === null ? "left" : iconpos));
        };

    $.widget("mobile.collapsible", {
        options: {
            enhanced: false,
            expandCueText: null,
            collapseCueText: null,
            collapsed: true,
            heading: "h1,h2,h3,h4,h5,h6,legend",
            collapsedIcon: null,
            expandedIcon: null,
            iconpos: null,
            theme: null,
            contentTheme: null,
            inset: null,
            corners: null,
            mini: null
        },

        _create: function () {
            var elem = this.element,
                ui = {
                    accordion: elem
                        .closest("[data-role='collapsible-set']," +
                            "[data-role='collapsibleset']" +
                            ($.mobile.collapsibleset ? ", :mobile-collapsibleset" :
                                ""))
                        .addClass("ui-collapsible-set")
                };

            this._ui = ui;
            this._renderedOptions = this._getOptions(this.options);

            if (this.options.enhanced) {
                ui.heading = this.element.children(".ui-collapsible-heading");
                ui.content = ui.heading.next();
                ui.anchor = ui.heading.children();
                ui.status = ui.anchor.children(".ui-collapsible-heading-status");
            } else {
                this._enhance(elem, ui);
            }

            this._on(ui.heading, {
                "tap": function () {
                    ui.heading.find("a").first().addClass($.mobile.activeBtnClass);
                },

                "click": function (event) {
                    this._handleExpandCollapse(!ui.heading.hasClass("ui-collapsible-heading-collapsed"));
                    event.preventDefault();
                    event.stopPropagation();
                }
            });
        },

        // Adjust the keys inside options for inherited values
        _getOptions: function (options) {
            var key,
                accordion = this._ui.accordion,
                accordionWidget = this._ui.accordionWidget;

            // Copy options
            options = $.extend({}, options);

            if (accordion.length && !accordionWidget) {
                this._ui.accordionWidget =
                accordionWidget = accordion.data("mobile-collapsibleset");
            }

            for (key in options) {

                // Retrieve the option value first from the options object passed in and, if
                // null, from the parent accordion or, if that's null too, or if there's no
                // parent accordion, then from the defaults.
                options[key] =
                    (options[key] != null) ? options[key] :
                    (accordionWidget) ? accordionWidget.options[key] :
                    accordion.length ? $.mobile.getAttribute(accordion[0],
                        key.replace(rInitialLetter, "-$1").toLowerCase()) :
                    null;

                if (null == options[key]) {
                    options[key] = $.mobile.collapsible.defaults[key];
                }
            }

            return options;
        },

        _themeClassFromOption: function (prefix, value) {
            return (value ? (value === "none" ? "" : prefix + value) : "");
        },

        _enhance: function (elem, ui) {
            var iconclass,
                opts = this._renderedOptions,
                contentThemeClass = this._themeClassFromOption("ui-body-", opts.contentTheme);

            elem.addClass("ui-collapsible " +
                (opts.inset ? "ui-collapsible-inset " : "") +
                (opts.inset && opts.corners ? "ui-corner-all " : "") +
                (contentThemeClass ? "ui-collapsible-themed-content " : ""));
            ui.originalHeading = elem.children(this.options.heading).first(),
            ui.content = elem
                .wrapInner("<div " +
                    "class='ui-collapsible-content " +
                    contentThemeClass + "'></div>")
                .children(".ui-collapsible-content"),
            ui.heading = ui.originalHeading;

            // Replace collapsibleHeading if it's a legend
            if (ui.heading.is("legend")) {
                ui.heading = $("<div role='heading'>" + ui.heading.html() + "</div>");
                ui.placeholder = $("<div><!-- placeholder for legend --></div>").insertBefore(ui.originalHeading);
                ui.originalHeading.remove();
            }

            iconclass = (opts.collapsed ? (opts.collapsedIcon ? "ui-icon-" + opts.collapsedIcon : "") :
                (opts.expandedIcon ? "ui-icon-" + opts.expandedIcon : ""));

            ui.status = $("<span class='ui-collapsible-heading-status'></span>");
            ui.anchor = ui.heading
                .detach()
                //modify markup & attributes
                .addClass("ui-collapsible-heading")
                .append(ui.status)
                .wrapInner("<a href='#' class='ui-collapsible-heading-toggle'></a>")
                .find("a")
                    .first()
                    .addClass("ui-btn " +
                        (iconclass ? iconclass + " " : "") +
                        (iconclass ? iconposClass(opts.iconpos) +
                            " " : "") +
                        this._themeClassFromOption("ui-btn-", opts.theme) + " " +
                        (opts.mini ? "ui-mini " : ""));

            //drop heading in before content
            ui.heading.insertBefore(ui.content);

            this._handleExpandCollapse(this.options.collapsed);

            return ui;
        },

        refresh: function () {
            this._applyOptions(this.options);
            this._renderedOptions = this._getOptions(this.options);
        },

        _applyOptions: function (options) {
            var isCollapsed, newTheme, oldTheme, hasCorners, hasIcon,
                elem = this.element,
                currentOpts = this._renderedOptions,
                ui = this._ui,
                anchor = ui.anchor,
                status = ui.status,
                opts = this._getOptions(options);

            // First and foremost we need to make sure the collapsible is in the proper
            // state, in case somebody decided to change the collapsed option at the
            // same time as another option
            if (options.collapsed !== undefined) {
                this._handleExpandCollapse(options.collapsed);
            }

            isCollapsed = elem.hasClass("ui-collapsible-collapsed");

            // We only need to apply the cue text for the current state right away.
            // The cue text for the alternate state will be stored in the options
            // and applied the next time the collapsible's state is toggled
            if (isCollapsed) {
                if (opts.expandCueText !== undefined) {
                    status.text(opts.expandCueText);
                }
            } else {
                if (opts.collapseCueText !== undefined) {
                    status.text(opts.collapseCueText);
                }
            }

            // Update icon

            // Is it supposed to have an icon?
            hasIcon =

                // If the collapsedIcon is being set, consult that
                (opts.collapsedIcon !== undefined ? opts.collapsedIcon !== false :

                    // Otherwise consult the existing option value
                    currentOpts.collapsedIcon !== false);


            // If any icon-related options have changed, make sure the new icon
            // state is reflected by first removing all icon-related classes
            // reflecting the current state and then adding all icon-related
            // classes for the new state
            if (!(opts.iconpos === undefined &&
                opts.collapsedIcon === undefined &&
                opts.expandedIcon === undefined)) {

                // Remove all current icon-related classes
                anchor.removeClass([iconposClass(currentOpts.iconpos)]
                    .concat((currentOpts.expandedIcon ?
                        ["ui-icon-" + currentOpts.expandedIcon] : []))
                    .concat((currentOpts.collapsedIcon ?
                        ["ui-icon-" + currentOpts.collapsedIcon] : []))
                    .join(" "));

                // Add new classes if an icon is supposed to be present
                if (hasIcon) {
                    anchor.addClass(
                        [iconposClass(opts.iconpos !== undefined ?
                            opts.iconpos : currentOpts.iconpos)]
                            .concat(isCollapsed ?
                                ["ui-icon-" + (opts.collapsedIcon !== undefined ?
                                    opts.collapsedIcon :
                                    currentOpts.collapsedIcon)] :
                                ["ui-icon-" + (opts.expandedIcon !== undefined ?
                                    opts.expandedIcon :
                                    currentOpts.expandedIcon)])
                            .join(" "));
                }
            }

            if (opts.theme !== undefined) {
                oldTheme = this._themeClassFromOption("ui-btn-", currentOpts.theme);
                newTheme = this._themeClassFromOption("ui-btn-", opts.theme);
                anchor.removeClass(oldTheme).addClass(newTheme);
            }

            if (opts.contentTheme !== undefined) {
                oldTheme = this._themeClassFromOption("ui-body-",
                    currentOpts.contentTheme);
                newTheme = this._themeClassFromOption("ui-body-",
                    opts.contentTheme);
                ui.content.removeClass(oldTheme).addClass(newTheme);
            }

            if (opts.inset !== undefined) {
                elem.toggleClass("ui-collapsible-inset", opts.inset);
                hasCorners = !!(opts.inset && (opts.corners || currentOpts.corners));
            }

            if (opts.corners !== undefined) {
                hasCorners = !!(opts.corners && (opts.inset || currentOpts.inset));
            }

            if (hasCorners !== undefined) {
                elem.toggleClass("ui-corner-all", hasCorners);
            }

            if (opts.mini !== undefined) {
                anchor.toggleClass("ui-mini", opts.mini);
            }
        },

        _setOptions: function (options) {
            this._applyOptions(options);
            this._super(options);
            this._renderedOptions = this._getOptions(this.options);
        },

        _handleExpandCollapse: function (isCollapse) {
            var opts = this._renderedOptions,
                ui = this._ui;

            ui.status.text(isCollapse ? opts.expandCueText : opts.collapseCueText);
            ui.heading
                .toggleClass("ui-collapsible-heading-collapsed", isCollapse)
                .find("a").first()
                .toggleClass("ui-icon-" + opts.expandedIcon, !isCollapse)

                // logic or cause same icon for expanded/collapsed state would remove the ui-icon-class
                .toggleClass("ui-icon-" + opts.collapsedIcon, (isCollapse || opts.expandedIcon === opts.collapsedIcon))
                .removeClass($.mobile.activeBtnClass);

            this.element.toggleClass("ui-collapsible-collapsed", isCollapse);
            ui.content
                .toggleClass("ui-collapsible-content-collapsed", isCollapse)
                .attr("aria-hidden", isCollapse)
                .trigger("updatelayout");
            this.options.collapsed = isCollapse;
            this._trigger(isCollapse ? "collapse" : "expand");
        },

        expand: function () {
            this._handleExpandCollapse(false);
        },

        collapse: function () {
            this._handleExpandCollapse(true);
        },

        _destroy: function () {
            var ui = this._ui,
                opts = this.options;

            if (opts.enhanced) {
                return;
            }

            if (ui.placeholder) {
                ui.originalHeading.insertBefore(ui.placeholder);
                ui.placeholder.remove();
                ui.heading.remove();
            } else {
                ui.status.remove();
                ui.heading
                    .removeClass("ui-collapsible-heading ui-collapsible-heading-collapsed")
                    .children()
                        .contents()
                            .unwrap();
            }

            ui.anchor.contents().unwrap();
            ui.content.contents().unwrap();
            this.element
                .removeClass("ui-collapsible ui-collapsible-collapsed " +
                    "ui-collapsible-themed-content ui-collapsible-inset ui-corner-all");
        }
    });

    // Defaults to be used by all instances of collapsible if per-instance values
    // are unset or if nothing is specified by way of inheritance from an accordion.
    // Note that this hash does not contain options "collapsed" or "heading",
    // because those are not inheritable.
    $.mobile.collapsible.defaults = {
        expandCueText: " click to expand contents",
        collapseCueText: " click to collapse contents",
        collapsedIcon: "plus",
        contentTheme: "inherit",
        expandedIcon: "minus",
        iconpos: "left",
        inset: true,
        corners: true,
        theme: "inherit",
        mini: false
    };

})(jQuery);


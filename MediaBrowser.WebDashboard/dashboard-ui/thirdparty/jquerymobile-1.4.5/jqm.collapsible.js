define(['jqmwidget'], function () {

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

});
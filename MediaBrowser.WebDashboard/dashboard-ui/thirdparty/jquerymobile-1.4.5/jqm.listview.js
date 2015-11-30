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

        var getAttr = $.mobile.getAttribute;

        $.widget("mobile.listview", $.extend({

            options: {
                theme: null,
                countTheme: null, /* Deprecated in 1.4 */
                dividerTheme: null,
                icon: "carat-r",
                splitIcon: "carat-r",
                splitTheme: null,
                corners: true,
                shadow: true,
                inset: false
            },

            _create: function () {
                var t = this,
                    listviewClasses = "";

                listviewClasses += t.options.inset ? " ui-listview-inset" : "";

                if (!!t.options.inset) {
                    listviewClasses += t.options.corners ? " ui-corner-all" : "";
                    listviewClasses += t.options.shadow ? " ui-shadow" : "";
                }

                // create listview markup
                t.element.addClass(" ui-listview" + listviewClasses);

                t.refresh(true);
            },

            // TODO: Remove in 1.5
            _findFirstElementByTagName: function (ele, nextProp, lcName, ucName) {
                var dict = {};
                dict[lcName] = dict[ucName] = true;
                while (ele) {
                    if (dict[ele.nodeName]) {
                        return ele;
                    }
                    ele = ele[nextProp];
                }
                return null;
            },
            // TODO: Remove in 1.5
            _addThumbClasses: function (containers) {
                var i, img, len = containers.length;
                for (i = 0; i < len; i++) {
                    img = $(this._findFirstElementByTagName(containers[i].firstChild, "nextSibling", "img", "IMG"));
                    if (img.length) {
                        $(this._findFirstElementByTagName(img[0].parentNode, "parentNode", "li", "LI")).addClass(img.hasClass("ui-li-icon") ? "ui-li-has-icon" : "ui-li-has-thumb");
                    }
                }
            },

            _getChildrenByTagName: function (ele, lcName, ucName) {
                var results = [],
                    dict = {};
                dict[lcName] = dict[ucName] = true;
                ele = ele.firstChild;
                while (ele) {
                    if (dict[ele.nodeName]) {
                        results.push(ele);
                    }
                    ele = ele.nextSibling;
                }
                return $(results);
            },

            _beforeListviewRefresh: $.noop,
            _afterListviewRefresh: $.noop,

            refresh: function (create) {
                var buttonClass, pos, numli, item, itemClass, itemTheme, itemIcon, icon, a,
                    isDivider, startCount, newStartCount, value, last, splittheme, splitThemeClass, spliticon,
                    altButtonClass, dividerTheme, li,
                    o = this.options,
                    $list = this.element,
                    ol = !!$.nodeName($list[0], "ol"),
                    start = $list.attr("start"),
                    itemClassDict = {},
                    countBubbles = $list.find(".ui-li-count"),
                    countTheme = getAttr($list[0], "counttheme") || this.options.countTheme,
                    countThemeClass = countTheme ? "ui-body-" + countTheme : "ui-body-inherit";

                if (o.theme) {
                    $list.addClass("ui-group-theme-" + o.theme);
                }

                // Check if a start attribute has been set while taking a value of 0 into account
                if (ol && (start || start === 0)) {
                    startCount = parseInt(start, 10) - 1;
                    $list.css("counter-reset", "listnumbering " + startCount);
                }

                this._beforeListviewRefresh();

                li = this._getChildrenByTagName($list[0], "li", "LI");

                for (pos = 0, numli = li.length; pos < numli; pos++) {
                    item = li.eq(pos);
                    itemClass = "";

                    if (create || item[0].className.search(/\bui-li-static\b|\bui-li-divider\b/) < 0) {
                        a = this._getChildrenByTagName(item[0], "a", "A");
                        isDivider = (getAttr(item[0], "role") === "list-divider");
                        value = item.attr("value");
                        itemTheme = getAttr(item[0], "theme");

                        if (a.length && a[0].className.search(/\bui-btn\b/) < 0 && !isDivider) {
                            itemIcon = getAttr(item[0], "icon");
                            icon = (itemIcon === false) ? false : (itemIcon || o.icon);

                            // TODO: Remove in 1.5 together with links.js (links.js / .ui-link deprecated in 1.4)
                            a.removeClass("ui-link");

                            buttonClass = "ui-btn";

                            if (itemTheme) {
                                buttonClass += " ui-btn-" + itemTheme;
                            }

                            if (a.length > 1) {
                                itemClass = "ui-li-has-alt";

                                last = a.last();
                                splittheme = getAttr(last[0], "theme") || o.splitTheme || getAttr(item[0], "theme", true);
                                splitThemeClass = splittheme ? " ui-btn-" + splittheme : "";
                                spliticon = getAttr(last[0], "icon") || getAttr(item[0], "icon") || o.splitIcon;
                                altButtonClass = "ui-btn ui-btn-icon-notext ui-icon-" + spliticon + splitThemeClass;

                                last
                                    .attr("title", $.trim(last.text()))
                                    .addClass(altButtonClass)
                                    .empty();

                                // Reduce to the first anchor, because only the first gets the buttonClass
                                a = a.first();
                            } else if (icon) {
                                buttonClass += " ui-btn-icon-right ui-icon-" + icon;
                            }

                            // Apply buttonClass to the (first) anchor
                            a.addClass(buttonClass);
                        } else if (isDivider) {
                            dividerTheme = (getAttr(item[0], "theme") || o.dividerTheme || o.theme);

                            itemClass = "ui-li-divider ui-bar-" + (dividerTheme ? dividerTheme : "inherit");

                            item.attr("role", "heading");
                        } else if (a.length <= 0) {
                            itemClass = "ui-li-static ui-body-" + (itemTheme ? itemTheme : "inherit");
                        }
                        if (ol && value) {
                            newStartCount = parseInt(value, 10) - 1;

                            item.css("counter-reset", "listnumbering " + newStartCount);
                        }
                    }

                    // Instead of setting item class directly on the list item
                    // at this point in time, push the item into a dictionary
                    // that tells us what class to set on it so we can do this after this
                    // processing loop is finished.

                    if (!itemClassDict[itemClass]) {
                        itemClassDict[itemClass] = [];
                    }

                    itemClassDict[itemClass].push(item[0]);
                }

                // Set the appropriate listview item classes on each list item.
                // The main reason we didn't do this
                // in the for-loop above is because we can eliminate per-item function overhead
                // by calling addClass() and children() once or twice afterwards. This
                // can give us a significant boost on platforms like WP7.5.

                for (itemClass in itemClassDict) {
                    $(itemClassDict[itemClass]).addClass(itemClass);
                }

                countBubbles.each(function () {
                    $(this).closest("li").addClass("ui-li-has-count");
                });
                if (countThemeClass) {
                    countBubbles.not("[class*='ui-body-']").addClass(countThemeClass);
                }

                // Deprecated in 1.4. From 1.5 you have to add class ui-li-has-thumb or ui-li-has-icon to the LI.
                this._addThumbClasses(li);
                this._addThumbClasses(li.find(".ui-btn"));

                this._afterListviewRefresh();

                this._addFirstLastClasses(li, this._getVisibles(li, create), create);
            }
        }, $.mobile.behaviors.addFirstLastClasses));

    })(jQuery);

});
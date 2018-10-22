define(["userSettings"], function(userSettings) {
    "use strict";
    var libraryBrowser = {
        getSavedQueryKey: function(modifier) {
            return window.location.href.split("#")[0] + (modifier || "")
        },
        loadSavedQueryValues: function(key, query) {
            var values = userSettings.get(key);
            return values ? (values = JSON.parse(values), Object.assign(query, values)) : query
        },
        saveQueryValues: function(key, query) {
            var values = {};
            query.SortBy && (values.SortBy = query.SortBy), query.SortOrder && (values.SortOrder = query.SortOrder), userSettings.set(key, JSON.stringify(values))
        },
        saveViewSetting: function(key, value) {
            userSettings.set(key + "-_view", value)
        },
        getSavedView: function(key) {
            return userSettings.get(key + "-_view")
        },
        showLayoutMenu: function(button, currentLayout, views) {
            var dispatchEvent = !0;
            views || (dispatchEvent = !1, views = button.getAttribute("data-layouts"), views = views ? views.split(",") : ["List", "Poster", "PosterCard", "Thumb", "ThumbCard"]);
            var menuItems = views.map(function(v) {
                return {
                    name: Globalize.translate("Option" + v),
                    id: v,
                    selected: currentLayout == v
                }
            });
            require(["actionsheet"], function(actionsheet) {
                actionsheet.show({
                    items: menuItems,
                    positionTo: button,
                    callback: function(id) {
                        button.dispatchEvent(new CustomEvent("layoutchange", {
                            detail: {
                                viewStyle: id
                            },
                            bubbles: !0,
                            cancelable: !1
                        })), dispatchEvent || window.$ && $(button).trigger("layoutchange", [id])
                    }
                })
            })
        },
        getQueryPagingHtml: function(options) {
            var startIndex = options.startIndex,
                limit = options.limit,
                totalRecordCount = options.totalRecordCount,
                html = "",
                recordsEnd = Math.min(startIndex + limit, totalRecordCount),
                showControls = limit < totalRecordCount;
            if (html += '<div class="listPaging">', showControls) {
                html += '<span style="vertical-align:middle;">';
                html += (totalRecordCount ? startIndex + 1 : 0) + "-" + recordsEnd + " of " + totalRecordCount, html += "</span>"
            }
            return (showControls || options.viewButton || options.filterButton || options.sortButton || options.addLayoutButton) && (html += '<div style="display:inline-block;">', showControls && (html += '<button is="paper-icon-button-light" class="btnPreviousPage autoSize" ' + (startIndex ? "" : "disabled") + '><i class="md-icon">&#xE5C4;</i></button>', html += '<button is="paper-icon-button-light" class="btnNextPage autoSize" ' + (startIndex + limit >= totalRecordCount ? "disabled" : "") + '><i class="md-icon">&#xE5C8;</i></button>'), options.addLayoutButton && (html += '<button is="paper-icon-button-light" title="' + Globalize.translate("ButtonSelectView") + '" class="btnChangeLayout autoSize" data-layouts="' + (options.layouts || "") + '" onclick="LibraryBrowser.showLayoutMenu(this, \'' + (options.currentLayout || "") + '\');"><i class="md-icon">&#xE42A;</i></button>'), options.sortButton && (html += '<button is="paper-icon-button-light" class="btnSort autoSize" title="' + Globalize.translate("ButtonSort") + '"><i class="md-icon">&#xE053;</i></button>'), options.filterButton && (html += '<button is="paper-icon-button-light" class="btnFilter autoSize" title="' + Globalize.translate("ButtonFilter") + '"><i class="md-icon">&#xE152;</i></button>'), html += "</div>"), html += "</div>"
        },
        showSortMenu: function(options) {
            require(["dialogHelper", "emby-radio"], function(dialogHelper) {
                function onSortByChange() {
                    var newValue = this.value;
                    if (this.checked) {
                        var changed = options.query.SortBy != newValue;
                        options.query.SortBy = newValue.replace("_", ","), options.query.StartIndex = 0, options.callback && changed && options.callback()
                    }
                }

                function onSortOrderChange() {
                    var newValue = this.value;
                    if (this.checked) {
                        var changed = options.query.SortOrder != newValue;
                        options.query.SortOrder = newValue, options.query.StartIndex = 0, options.callback && changed && options.callback()
                    }
                }
                var dlg = dialogHelper.createDialog({
                    removeOnClose: !0,
                    modal: !1,
                    entryAnimationDuration: 160,
                    exitAnimationDuration: 200
                });
                dlg.classList.add("ui-body-a"), dlg.classList.add("background-theme-a"), dlg.classList.add("formDialog");
                var html = "";
                html += '<div style="margin:0;padding:1.25em 1.5em 1.5em;">', html += '<h2 style="margin:0 0 .5em;">', html += Globalize.translate("HeaderSortBy"), html += "</h2>";
                var i, length, isChecked;
                for (html += "<div>", i = 0, length = options.items.length; i < length; i++) {
                    var option = options.items[i],
                        radioValue = option.id.replace(",", "_");
                    isChecked = (options.query.SortBy || "").replace(",", "_") == radioValue ? " checked" : "", html += '<label class="radio-label-block"><input type="radio" is="emby-radio" name="SortBy" data-id="' + option.id + '" value="' + radioValue + '" class="menuSortBy" ' + isChecked + " /><span>" + option.name + "</span></label>"
                }
                html += "</div>", html += '<h2 style="margin: 1em 0 .5em;">', html += Globalize.translate("HeaderSortOrder"), html += "</h2>", html += "<div>", isChecked = "Ascending" == options.query.SortOrder ? " checked" : "", html += '<label class="radio-label-block"><input type="radio" is="emby-radio" name="SortOrder" value="Ascending" class="menuSortOrder" ' + isChecked + " /><span>" + Globalize.translate("OptionAscending") + "</span></label>", isChecked = "Descending" == options.query.SortOrder ? " checked" : "", html += '<label class="radio-label-block"><input type="radio" is="emby-radio" name="SortOrder" value="Descending" class="menuSortOrder" ' + isChecked + " /><span>" + Globalize.translate("OptionDescending") + "</span></label>", html += "</div>", html += "</div>", dlg.innerHTML = html, dialogHelper.open(dlg);
                var sortBys = dlg.querySelectorAll(".menuSortBy");
                for (i = 0, length = sortBys.length; i < length; i++) sortBys[i].addEventListener("change", onSortByChange);
                var sortOrders = dlg.querySelectorAll(".menuSortOrder");
                for (i = 0, length = sortOrders.length; i < length; i++) sortOrders[i].addEventListener("change", onSortOrderChange)
            })
        }
    };
    return window.LibraryBrowser = libraryBrowser, libraryBrowser
});
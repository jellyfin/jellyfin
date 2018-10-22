define(["require", "dom", "focusManager", "dialogHelper", "loading", "apphost", "inputManager", "layoutManager", "connectionManager", "appRouter", "globalize", "userSettings", "emby-checkbox", "emby-input", "paper-icon-button-light", "emby-select", "material-icons", "css!./../formdialog", "emby-button", "emby-linkbutton", "flexStyles"], function(require, dom, focusManager, dialogHelper, loading, appHost, inputManager, layoutManager, connectionManager, appRouter, globalize, userSettings) {
    "use strict";

    function onSubmit(e) {
        return e.preventDefault(), !1
    }

    function renderOptions(context, selector, cssClass, items, isCheckedFn) {
        var elem = context.querySelector(selector);
        items.length ? elem.classList.remove("hide") : elem.classList.add("hide");
        var html = "";
        html += items.map(function(filter) {
            var itemHtml = "",
                checkedHtml = isCheckedFn(filter) ? " checked" : "";
            return itemHtml += "<label>", itemHtml += '<input is="emby-checkbox" type="checkbox"' + checkedHtml + ' data-filter="' + filter.Id + '" class="' + cssClass + '"/>', itemHtml += "<span>" + filter.Name + "</span>", itemHtml += "</label>"
        }).join(""), elem.querySelector(".filterOptions").innerHTML = html
    }

    function renderDynamicFilters(context, result, options) {
        renderOptions(context, ".genreFilters", "chkGenreFilter", result.Genres, function(i) {
            var delimeter = -1 === (options.settings.GenreIds || "").indexOf("|") ? "," : "|";
            return -1 !== (delimeter + (options.settings.GenreIds || "") + delimeter).indexOf(delimeter + i.Id + delimeter)
        })
    }

    function loadDynamicFilters(context, options) {
        var apiClient = connectionManager.getApiClient(options.serverId),
            filterMenuOptions = Object.assign(options.filterMenuOptions, {
                UserId: apiClient.getCurrentUserId(),
                ParentId: options.parentId,
                IncludeItemTypes: options.itemTypes.join(",")
            });
        apiClient.getFilters(filterMenuOptions).then(function(result) {
            renderDynamicFilters(context, result, options)
        }, function() {})
    }

    function initEditor(context, settings) {
        context.querySelector("form").addEventListener("submit", onSubmit);
        var i, length, elems = context.querySelectorAll(".simpleFilter");
        for (i = 0, length = elems.length; i < length; i++) "INPUT" === elems[i].tagName ? elems[i].checked = settings[elems[i].getAttribute("data-settingname")] || !1 : elems[i].querySelector("input").checked = settings[elems[i].getAttribute("data-settingname")] || !1;
        var videoTypes = settings.VideoTypes ? settings.VideoTypes.split(",") : [];
        for (elems = context.querySelectorAll(".chkVideoTypeFilter"), i = 0, length = elems.length; i < length; i++) elems[i].checked = -1 !== videoTypes.indexOf(elems[i].getAttribute("data-filter"));
        var seriesStatuses = settings.SeriesStatus ? settings.SeriesStatus.split(",") : [];
        for (elems = context.querySelectorAll(".chkSeriesStatus"), i = 0, length = elems.length; i < length; i++) elems[i].checked = -1 !== seriesStatuses.indexOf(elems[i].getAttribute("data-filter"));
        context.querySelector(".basicFilterSection .viewSetting:not(.hide)") ? context.querySelector(".basicFilterSection").classList.remove("hide") : context.querySelector(".basicFilterSection").classList.add("hide"), context.querySelector(".featureSection .viewSetting:not(.hide)") ? context.querySelector(".featureSection").classList.remove("hide") : context.querySelector(".featureSection").classList.add("hide")
    }

    function saveValues(context, settings, settingsKey) {
        var i, length, elems = context.querySelectorAll(".simpleFilter");
        for (i = 0, length = elems.length; i < length; i++) "INPUT" === elems[i].tagName ? setBasicFilter(context, settingsKey + "-filter-" + elems[i].getAttribute("data-settingname"), elems[i]) : setBasicFilter(context, settingsKey + "-filter-" + elems[i].getAttribute("data-settingname"), elems[i].querySelector("input"));
        var videoTypes = [];
        for (elems = context.querySelectorAll(".chkVideoTypeFilter"), i = 0, length = elems.length; i < length; i++) elems[i].checked && videoTypes.push(elems[i].getAttribute("data-filter"));
        userSettings.setFilter(settingsKey + "-filter-VideoTypes", videoTypes.join(","));
        var seriesStatuses = [];
        for (elems = context.querySelectorAll(".chkSeriesStatus"), i = 0, length = elems.length; i < length; i++) elems[i].checked && seriesStatuses.push(elems[i].getAttribute("data-filter"));
        var genres = [];
        for (elems = context.querySelectorAll(".chkGenreFilter"), i = 0, length = elems.length; i < length; i++) elems[i].checked && genres.push(elems[i].getAttribute("data-filter"));
        userSettings.setFilter(settingsKey + "-filter-GenreIds", genres.join(","))
    }

    function setBasicFilter(context, key, elem) {
        var value = elem.checked;
        value = value || null, userSettings.setFilter(key, value)
    }

    function centerFocus(elem, horiz, on) {
        require(["scrollHelper"], function(scrollHelper) {
            var fn = on ? "on" : "off";
            scrollHelper.centerFocus[fn](elem, horiz)
        })
    }

    function moveCheckboxFocus(elem, offset) {
        for (var parent = dom.parentWithClass(elem, "checkboxList-verticalwrap"), elems = focusManager.getFocusableElements(parent), index = -1, i = 0, length = elems.length; i < length; i++)
            if (elems[i] === elem) {
                index = i;
                break
            } index += offset, index = Math.min(elems.length - 1, index), index = Math.max(0, index);
        var newElem = elems[index];
        newElem && focusManager.focus(newElem)
    }

    function onInputCommand(e) {
        switch (e.detail.command) {
            case "left":
                moveCheckboxFocus(e.target, -1), e.preventDefault();
                break;
            case "right":
                moveCheckboxFocus(e.target, 1), e.preventDefault()
        }
    }

    function FilterMenu() {}

    function bindCheckboxInput(context, on) {
        for (var elems = context.querySelectorAll(".checkboxList-verticalwrap"), i = 0, length = elems.length; i < length; i++) on ? inputManager.on(elems[i], onInputCommand) : inputManager.off(elems[i], onInputCommand)
    }
    return FilterMenu.prototype.show = function(options) {
        return new Promise(function(resolve, reject) {
            require(["text!./filtermenu.template.html"], function(template) {
                var dialogOptions = {
                    removeOnClose: !0,
                    scrollY: !1
                };
                layoutManager.tv ? dialogOptions.size = "fullscreen" : dialogOptions.size = "small";
                var dlg = dialogHelper.createDialog(dialogOptions);
                dlg.classList.add("formDialog");
                var html = "";
                html += '<div class="formDialogHeader">', html += '<button is="paper-icon-button-light" class="btnCancel hide-mouse-idle-tv" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>', html += '<h3 class="formDialogHeaderTitle">${Filters}</h3>', html += "</div>", html += template, dlg.innerHTML = globalize.translateDocument(html, "sharedcomponents");
                for (var settingElements = dlg.querySelectorAll(".viewSetting"), i = 0, length = settingElements.length; i < length; i++) - 1 === options.visibleSettings.indexOf(settingElements[i].getAttribute("data-settingname")) ? settingElements[i].classList.add("hide") : settingElements[i].classList.remove("hide");
                initEditor(dlg, options.settings), loadDynamicFilters(dlg, options), bindCheckboxInput(dlg, !0), dlg.querySelector(".btnCancel").addEventListener("click", function() {
                    dialogHelper.close(dlg)
                }), layoutManager.tv && centerFocus(dlg.querySelector(".formDialogContent"), !1, !0);
                var submitted;
                dlg.querySelector("form").addEventListener("change", function() {
                    submitted = !0
                }, !0), dialogHelper.open(dlg).then(function() {
                    if (bindCheckboxInput(dlg, !1), layoutManager.tv && centerFocus(dlg.querySelector(".formDialogContent"), !1, !1), submitted) return saveValues(dlg, options.settings, options.settingsKey), void resolve();
                    reject()
                })
            })
        })
    }, FilterMenu
});
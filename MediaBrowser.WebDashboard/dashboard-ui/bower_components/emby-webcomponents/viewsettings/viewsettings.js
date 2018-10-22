define(["require", "dialogHelper", "loading", "apphost", "layoutManager", "connectionManager", "appRouter", "globalize", "userSettings", "emby-checkbox", "emby-input", "paper-icon-button-light", "emby-select", "material-icons", "css!./../formdialog", "emby-button", "emby-linkbutton", "flexStyles"], function(require, dialogHelper, loading, appHost, layoutManager, connectionManager, appRouter, globalize, userSettings) {
    "use strict";

    function onSubmit(e) {
        return e.preventDefault(), !1
    }

    function initEditor(context, settings) {
        context.querySelector("form").addEventListener("submit", onSubmit);
        for (var elems = context.querySelectorAll(".viewSetting-checkboxContainer"), i = 0, length = elems.length; i < length; i++) elems[i].querySelector("input").checked = settings[elems[i].getAttribute("data-settingname")] || !1;
        context.querySelector(".selectImageType").value = settings.imageType || "primary"
    }

    function saveValues(context, settings, settingsKey) {
        for (var elems = context.querySelectorAll(".viewSetting-checkboxContainer"), i = 0, length = elems.length; i < length; i++) userSettings.set(settingsKey + "-" + elems[i].getAttribute("data-settingname"), elems[i].querySelector("input").checked);
        userSettings.set(settingsKey + "-imageType", context.querySelector(".selectImageType").value)
    }

    function centerFocus(elem, horiz, on) {
        require(["scrollHelper"], function(scrollHelper) {
            var fn = on ? "on" : "off";
            scrollHelper.centerFocus[fn](elem, horiz)
        })
    }

    function showIfAllowed(context, selector, visible) {
        var elem = context.querySelector(selector);
        visible && !elem.classList.contains("hiddenFromViewSettings") ? elem.classList.remove("hide") : elem.classList.add("hide")
    }

    function ViewSettings() {}
    return ViewSettings.prototype.show = function(options) {
        return new Promise(function(resolve, reject) {
            require(["text!./viewsettings.template.html"], function(template) {
                var dialogOptions = {
                    removeOnClose: !0,
                    scrollY: !1
                };
                layoutManager.tv ? dialogOptions.size = "fullscreen" : dialogOptions.size = "small";
                var dlg = dialogHelper.createDialog(dialogOptions);
                dlg.classList.add("formDialog");
                var html = "";
                html += '<div class="formDialogHeader">', html += '<button is="paper-icon-button-light" class="btnCancel hide-mouse-idle-tv" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>', html += '<h3 class="formDialogHeaderTitle">${Settings}</h3>', html += "</div>", html += template, dlg.innerHTML = globalize.translateDocument(html, "sharedcomponents");
                for (var settingElements = dlg.querySelectorAll(".viewSetting"), i = 0, length = settingElements.length; i < length; i++) - 1 === options.visibleSettings.indexOf(settingElements[i].getAttribute("data-settingname")) ? (settingElements[i].classList.add("hide"), settingElements[i].classList.add("hiddenFromViewSettings")) : (settingElements[i].classList.remove("hide"), settingElements[i].classList.remove("hiddenFromViewSettings"));
                initEditor(dlg, options.settings), dlg.querySelector(".selectImageType").addEventListener("change", function() {
                    showIfAllowed(dlg, ".chkTitleContainer", "list" !== this.value), showIfAllowed(dlg, ".chkYearContainer", "list" !== this.value)
                }), dlg.querySelector(".btnCancel").addEventListener("click", function() {
                    dialogHelper.close(dlg)
                }), layoutManager.tv && centerFocus(dlg.querySelector(".formDialogContent"), !1, !0);
                var submitted;
                dlg.querySelector(".selectImageType").dispatchEvent(new CustomEvent("change", {})), dlg.querySelector("form").addEventListener("change", function() {
                    submitted = !0
                }, !0), dialogHelper.open(dlg).then(function() {
                    if (layoutManager.tv && centerFocus(dlg.querySelector(".formDialogContent"), !1, !1), submitted) return saveValues(dlg, options.settings, options.settingsKey), void resolve();
                    reject()
                })
            })
        })
    }, ViewSettings
});
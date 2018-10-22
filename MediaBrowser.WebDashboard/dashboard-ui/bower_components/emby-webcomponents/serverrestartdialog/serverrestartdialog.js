define(["loading", "events", "dialogHelper", "dom", "layoutManager", "scrollHelper", "globalize", "require", "material-icons", "emby-button", "paper-icon-button-light", "emby-input", "formDialogStyle", "flexStyles"], function(loading, events, dialogHelper, dom, layoutManager, scrollHelper, globalize, require) {
    "use strict";

    function reloadPageWhenServerAvailable(retryCount) {
        var apiClient = currentApiClient;
        apiClient && apiClient.getJSON(apiClient.getUrl("System/Info")).then(function(info) {
            info.IsShuttingDown ? retryReload(retryCount) : (currentInstance.restarted = !0, dialogHelper.close(currentDlg))
        }, function() {
            retryReload(retryCount)
        })
    }

    function retryReload(retryCount) {
        setTimeout(function() {
            retryCount = retryCount || 0, ++retryCount < 150 && reloadPageWhenServerAvailable(retryCount)
        }, 500)
    }

    function startRestart(instance, apiClient, dlg) {
        currentApiClient = apiClient, currentDlg = dlg, currentInstance = instance, apiClient.restartServer().then(function() {
            setTimeout(reloadPageWhenServerAvailable, 250)
        })
    }

    function showDialog(instance, options, template) {
        function onButtonClick() {
            dialogHelper.close(dlg)
        }
        var dialogOptions = {
                removeOnClose: !0,
                scrollY: !1
            },
            enableTvLayout = layoutManager.tv;
        enableTvLayout && (dialogOptions.size = "fullscreen");
        var dlg = dialogHelper.createDialog(dialogOptions),
            configuredButtons = [];
        dlg.classList.add("formDialog"), dlg.innerHTML = globalize.translateHtml(template, "sharedcomponents"), dlg.classList.add("align-items-center"), dlg.classList.add("justify-items-center");
        var formDialogContent = dlg.querySelector(".formDialogContent");
        formDialogContent.style["flex-grow"] = "initial", enableTvLayout ? (formDialogContent.style["max-width"] = "50%", formDialogContent.style["max-height"] = "60%", scrollHelper.centerFocus.on(formDialogContent, !1)) : (formDialogContent.style.maxWidth = Math.min(150 * configuredButtons.length + 200, dom.getWindowSize().innerWidth - 50) + "px", dlg.classList.add("dialog-fullscreen-lowres")), dlg.querySelector(".formDialogHeaderTitle").innerHTML = globalize.translate("sharedcomponents#HeaderRestartingEmbyServer"), dlg.querySelector(".text").innerHTML = globalize.translate("sharedcomponents#RestartPleaseWaitMessage");
        var i, length, html = "";
        for (i = 0, length = configuredButtons.length; i < length; i++) {
            var item = configuredButtons[i],
                autoFocus = 0 === i ? " autofocus" : "",
                buttonClass = "btnOption raised formDialogFooterItem formDialogFooterItem-autosize";
            item.type && (buttonClass += " button-" + item.type), html += '<button is="emby-button" type="button" class="' + buttonClass + '" data-id="' + item.id + '"' + autoFocus + ">" + item.name + "</button>"
        }
        dlg.querySelector(".formDialogFooter").innerHTML = html;
        var buttons = dlg.querySelectorAll(".btnOption");
        for (i = 0, length = buttons.length; i < length; i++) buttons[i].addEventListener("click", onButtonClick);
        var dlgPromise = dialogHelper.open(dlg);
        return startRestart(instance, options.apiClient, dlg), dlgPromise.then(function() {
            enableTvLayout && scrollHelper.centerFocus.off(dlg.querySelector(".formDialogContent"), !1), instance.destroy(), loading.hide(), instance.restarted && events.trigger(instance, "restarted")
        })
    }

    function ServerRestartDialog(options) {
        this.options = options
    }
    var currentApiClient, currentDlg, currentInstance;
    return ServerRestartDialog.prototype.show = function() {
        var instance = this;
        return loading.show(), new Promise(function(resolve, reject) {
            require(["text!./../dialog/dialog.template.html"], function(template) {
                showDialog(instance, instance.options, template).then(resolve, reject)
            })
        })
    }, ServerRestartDialog.prototype.destroy = function() {
        currentApiClient = null, currentDlg = null, currentInstance = null, this.options = null
    }, ServerRestartDialog
});
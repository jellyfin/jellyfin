define(["loading", "events", "dialogHelper", "dom", "layoutManager", "scrollHelper", "globalize", "require", "material-icons", "emby-button", "paper-icon-button-light", "emby-input", "formDialogStyle", "flexStyles"], function(loading, events, dialogHelper, dom, layoutManager, scrollHelper, globalize, require) {
    "use strict";

    function showDialog(instance, options, template) {
        var dialogOptions = {
                removeOnClose: !0,
                scrollY: !1
            },
            enableTvLayout = layoutManager.tv;
        enableTvLayout && (dialogOptions.size = "fullscreen");
        var dlg = dialogHelper.createDialog(dialogOptions);
        dlg.classList.add("formDialog"), dlg.innerHTML = globalize.translateHtml(template, "sharedcomponents"), dlg.classList.add("align-items-center"), dlg.classList.add("justify-items-center");
        var formDialogContent = dlg.querySelector(".formDialogContent");
        return formDialogContent.style["flex-grow"] = "initial", formDialogContent.style["max-width"] = "50%", formDialogContent.style["max-height"] = "60%", enableTvLayout ? (scrollHelper.centerFocus.on(formDialogContent, !1), dlg.querySelector(".formDialogHeader").style.marginTop = "15%") : dlg.classList.add("dialog-fullscreen-lowres"), dlg.querySelector(".formDialogHeaderTitle").innerHTML = options.title, dlg.querySelector(".text").innerHTML = options.text, instance.dlg = dlg, dialogHelper.open(dlg).then(function() {
            enableTvLayout && scrollHelper.centerFocus.off(dlg.querySelector(".formDialogContent"), !1), loading.hide()
        })
    }

    function LoadingDialog(options) {
        this.options = options
    }
    return LoadingDialog.prototype.show = function() {
        var instance = this;
        return loading.show(), new Promise(function(resolve, reject) {
            require(["text!./../dialog/dialog.template.html"], function(template) {
                showDialog(instance, instance.options, template), resolve()
            })
        })
    }, LoadingDialog.prototype.setTitle = function(title) {}, LoadingDialog.prototype.setText = function(text) {}, LoadingDialog.prototype.hide = function() {
        this.dlg && (dialogHelper.close(this.dlg), this.dlg = null)
    }, LoadingDialog.prototype.destroy = function() {
        this.dlg = null, this.options = null
    }, LoadingDialog
});
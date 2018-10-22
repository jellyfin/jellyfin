define(["dialogHelper", "connectionManager", "dom", "loading", "scrollHelper", "layoutManager", "globalize", "require", "emby-button", "emby-select", "formDialogStyle", "css!./style"], function(dialogHelper, connectionManager, dom, loading, scrollHelper, layoutManager, globalize, require) {
    "use strict";

    function onFileReaderError(evt) {
        switch (loading.hide(), evt.target.error.code) {
            case evt.target.error.NOT_FOUND_ERR:
                require(["toast"], function(toast) {
                    toast(globalize.translate("sharedcomponents#MessageFileReadError"))
                });
                break;
            case evt.target.error.ABORT_ERR:
                break;
            default:
                require(["toast"], function(toast) {
                    toast(globalize.translate("sharedcomponents#MessageFileReadError"))
                })
        }
    }

    function setFiles(page, files) {
        var file = files[0];
        if (!file || !file.type.match("image.*")) return page.querySelector("#imageOutput").innerHTML = "", page.querySelector("#fldUpload").classList.add("hide"), void(currentFile = null);
        currentFile = file;
        var reader = new FileReader;
        reader.onerror = onFileReaderError, reader.onloadstart = function() {
            page.querySelector("#fldUpload").classList.add("hide")
        }, reader.onabort = function() {
            loading.hide(), console.log("File read cancelled")
        }, reader.onload = function(theFile) {
            return function(e) {
                var html = ['<img style="max-width:100%;max-height:100%;" src="', e.target.result, '" title="', escape(theFile.name), '"/>'].join("");
                page.querySelector("#imageOutput").innerHTML = html, page.querySelector("#fldUpload").classList.remove("hide")
            }
        }(file), reader.readAsDataURL(file)
    }

    function onSubmit(e) {
        var file = currentFile;
        if (!file) return !1;
        if ("image/png" !== file.type && "image/jpeg" !== file.type && "image/jpeg" !== file.type) return !1;
        loading.show();
        var dlg = dom.parentWithClass(this, "dialog"),
            imageType = dlg.querySelector("#selectImageType").value;
        return connectionManager.getApiClient(currentServerId).uploadItemImage(currentItemId, imageType, file).then(function() {
            dlg.querySelector("#uploadImage").value = "", loading.hide(), hasChanges = !0, dialogHelper.close(dlg)
        }), e.preventDefault(), !1
    }

    function initEditor(page) {
        page.querySelector("form").addEventListener("submit", onSubmit), page.querySelector("#uploadImage").addEventListener("change", function() {
            setFiles(page, this.files)
        }), page.querySelector(".btnBrowse").addEventListener("click", function() {
            page.querySelector("#uploadImage").click()
        })
    }

    function showEditor(options, resolve, reject) {
        options = options || {}, require(["text!./imageuploader.template.html"], function(template) {
            currentItemId = options.itemId, currentServerId = options.serverId;
            var dialogOptions = {
                removeOnClose: !0
            };
            layoutManager.tv ? dialogOptions.size = "fullscreen" : dialogOptions.size = "fullscreen-border";
            var dlg = dialogHelper.createDialog(dialogOptions);
            dlg.classList.add("formDialog"), dlg.innerHTML = globalize.translateDocument(template, "sharedcomponents"), layoutManager.tv && scrollHelper.centerFocus.on(dlg, !1), dlg.addEventListener("close", function() {
                layoutManager.tv && scrollHelper.centerFocus.off(dlg, !1), loading.hide(), resolve(hasChanges)
            }), dialogHelper.open(dlg), initEditor(dlg), dlg.querySelector("#selectImageType").value = options.imageType || "Primary", dlg.querySelector(".btnCancel").addEventListener("click", function() {
                dialogHelper.close(dlg)
            })
        })
    }
    var currentItemId, currentServerId, currentFile, hasChanges = !1;
    return {
        show: function(options) {
            return new Promise(function(resolve, reject) {
                hasChanges = !1, showEditor(options, resolve, reject)
            })
        }
    }
});
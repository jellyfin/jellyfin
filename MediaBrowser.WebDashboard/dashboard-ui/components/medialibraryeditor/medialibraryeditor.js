define(["loading", "dialogHelper", "dom", "components/libraryoptionseditor/libraryoptionseditor", "emby-button", "listViewStyle", "paper-icon-button-light", "formDialogStyle", "emby-toggle", "flexStyles"], function(loading, dialogHelper, dom, libraryoptionseditor) {
    "use strict";

    function addMediaLocation(page, path, networkSharePath) {
        var virtualFolder = currentOptions.library,
            refreshAfterChange = currentOptions.refresh;
        ApiClient.addMediaPath(virtualFolder.Name, path, networkSharePath, refreshAfterChange).then(function() {
            hasChanges = !0, refreshLibraryFromServer(page)
        }, function() {
            require(["toast"], function(toast) {
                toast(Globalize.translate("ErrorAddingMediaPathToVirtualFolder"))
            })
        })
    }

    function updateMediaLocation(page, path, networkSharePath) {
        var virtualFolder = currentOptions.library;
        ApiClient.updateMediaPath(virtualFolder.Name, {
            Path: path,
            NetworkPath: networkSharePath
        }).then(function() {
            hasChanges = !0, refreshLibraryFromServer(page)
        }, function() {
            require(["toast"], function(toast) {
                toast(Globalize.translate("ErrorAddingMediaPathToVirtualFolder"))
            })
        })
    }

    function onRemoveClick(btnRemovePath, location) {
        var button = btnRemovePath,
            virtualFolder = currentOptions.library;
        require(["confirm"], function(confirm) {
            confirm({
                title: Globalize.translate("HeaderRemoveMediaLocation"),
                text: Globalize.translate("MessageConfirmRemoveMediaLocation"),
                confirmText: Globalize.translate("ButtonDelete"),
                primary: "cancel"
            }).then(function() {
                var refreshAfterChange = currentOptions.refresh;
                ApiClient.removeMediaPath(virtualFolder.Name, location, refreshAfterChange).then(function() {
                    hasChanges = !0, refreshLibraryFromServer(dom.parentWithClass(button, "dlg-libraryeditor"))
                }, function() {
                    require(["toast"], function(toast) {
                        toast(Globalize.translate("DefaultErrorMessage"))
                    })
                })
            })
        })
    }

    function onListItemClick(e) {
        var listItem = dom.parentWithClass(e.target, "listItem");
        if (listItem) {
            var index = parseInt(listItem.getAttribute("data-index")),
                pathInfos = (currentOptions.library.LibraryOptions || {}).PathInfos || [],
                pathInfo = null == index ? {} : pathInfos[index] || {},
                originalPath = pathInfo.Path || (null == index ? null : currentOptions.library.Locations[index]),
                btnRemovePath = dom.parentWithClass(e.target, "btnRemovePath");
            if (btnRemovePath) return void onRemoveClick(btnRemovePath, originalPath);
            showDirectoryBrowser(dom.parentWithClass(listItem, "dlg-libraryeditor"), originalPath, pathInfo.NetworkPath)
        }
    }

    function getFolderHtml(pathInfo, index) {
        var html = "";
        return html += '<div class="listItem listItem-border lnkPath" data-index="' + index + '" style="padding-left:.5em;">', html += '<div class="' + (pathInfo.NetworkPath ? "listItemBody two-line" : "listItemBody") + '">', html += '<h3 class="listItemBodyText">', html += pathInfo.Path, html += "</h3>", pathInfo.NetworkPath && (html += '<div class="listItemBodyText secondary">' + pathInfo.NetworkPath + "</div>"), html += "</div>", html += '<button type="button" is="paper-icon-button-light" class="listItemButton btnRemovePath" data-index="' + index + '"><i class="md-icon">remove_circle</i></button>', html += "</div>"
    }

    function refreshLibraryFromServer(page) {
        ApiClient.getVirtualFolders().then(function(result) {
            var library = result.filter(function(f) {
                return f.Name === currentOptions.library.Name
            })[0];
            library && (currentOptions.library = library, renderLibrary(page, currentOptions))
        })
    }

    function renderLibrary(page, options) {
        var pathInfos = (options.library.LibraryOptions || {}).PathInfos || [];
        pathInfos.length || (pathInfos = options.library.Locations.map(function(p) {
            return {
                Path: p
            }
        })), "boxsets" === options.library.CollectionType ? page.querySelector(".folders").classList.add("hide") : page.querySelector(".folders").classList.remove("hide"), page.querySelector(".folderList").innerHTML = pathInfos.map(getFolderHtml).join("")
    }

    function onAddButtonClick() {
        showDirectoryBrowser(dom.parentWithClass(this, "dlg-libraryeditor"))
    }

    function showDirectoryBrowser(context, originalPath, networkPath) {
        require(["directorybrowser"], function(directoryBrowser) {
            var picker = new directoryBrowser;
            picker.show({
                enableNetworkSharePath: !0,
                pathReadOnly: null != originalPath,
                path: originalPath,
                networkSharePath: networkPath,
                callback: function(path, networkSharePath) {
                    path && (originalPath ? updateMediaLocation(context, originalPath, networkSharePath) : addMediaLocation(context, path, networkSharePath)), picker.close()
                }
            })
        })
    }

    function onToggleAdvancedChange() {
        var dlg = dom.parentWithClass(this, "dlg-libraryeditor");
        libraryoptionseditor.setAdvancedVisible(dlg.querySelector(".libraryOptions"), this.checked)
    }

    function initEditor(dlg, options) {
        renderLibrary(dlg, options), dlg.querySelector(".btnAddFolder").addEventListener("click", onAddButtonClick), dlg.querySelector(".folderList").addEventListener("click", onListItemClick), dlg.querySelector(".chkAdvanced").addEventListener("change", onToggleAdvancedChange), libraryoptionseditor.embed(dlg.querySelector(".libraryOptions"), options.library.CollectionType, options.library.LibraryOptions).then(function() {
            onToggleAdvancedChange.call(dlg.querySelector(".chkAdvanced"))
        })
    }

    function onDialogClosing() {
        var dlg = this,
            libraryOptions = libraryoptionseditor.getLibraryOptions(dlg.querySelector(".libraryOptions"));
        libraryOptions = Object.assign(currentOptions.library.LibraryOptions || {}, libraryOptions), ApiClient.updateVirtualFolderOptions(currentOptions.library.ItemId, libraryOptions)
    }

    function onDialogClosed() {
        loading.hide(), hasChanges = !0, currentDeferred.resolveWith(null, [hasChanges])
    }

    function editor() {
        this.show = function(options) {
            var deferred = jQuery.Deferred();
            currentOptions = options, currentDeferred = deferred, hasChanges = !1;
            var xhr = new XMLHttpRequest;
            return xhr.open("GET", "components/medialibraryeditor/medialibraryeditor.template.html", !0), xhr.onload = function(e) {
                var template = this.response,
                    dlg = dialogHelper.createDialog({
                        size: "medium-tall",
                        modal: !1,
                        removeOnClose: !0,
                        scrollY: !1
                    });
                dlg.classList.add("dlg-libraryeditor"), dlg.classList.add("ui-body-a"), dlg.classList.add("background-theme-a"), dlg.classList.add("formDialog"), dlg.innerHTML = Globalize.translateDocument(template), dlg.querySelector(".formDialogHeaderTitle").innerHTML = options.library.Name, initEditor(dlg, options), dlg.addEventListener("closing", onDialogClosing), dlg.addEventListener("close", onDialogClosed), dialogHelper.open(dlg), dlg.querySelector(".btnCancel").addEventListener("click", function() {
                    dialogHelper.close(dlg)
                }), refreshLibraryFromServer(dlg)
            }, xhr.send(), deferred.promise()
        }
    }
    var currentDeferred, hasChanges, currentOptions;
    return editor
});
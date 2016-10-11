define(['dialogHelper', 'dom', 'components/libraryoptionseditor/libraryoptionseditor', 'emby-button', 'listViewStyle', 'paper-icon-button-light', 'formDialogStyle'], function (dialogHelper, dom, libraryoptionseditor) {

    var currentDeferred;
    var hasChanges;
    var currentOptions;

    function addMediaLocation(page, path, networkSharePath) {

        var virtualFolder = currentOptions.library;

        var refreshAfterChange = currentOptions.refresh;

        ApiClient.addMediaPath(virtualFolder.Name, path, networkSharePath, refreshAfterChange).then(function () {

            hasChanges = true;
            refreshLibraryFromServer(page);

        }, function () {

            require(['toast'], function (toast) {
                toast(Globalize.translate('ErrorAddingMediaPathToVirtualFolder'));
            });
        });
    }

    function updateMediaLocation(page, path, networkSharePath) {
        var virtualFolder = currentOptions.library;
        ApiClient.updateMediaPath(virtualFolder.Name, {

            Path: path,
            NetworkPath: networkSharePath

        }).then(function () {

            hasChanges = true;
            refreshLibraryFromServer(page);

        }, function () {

            require(['toast'], function (toast) {
                toast(Globalize.translate('ErrorAddingMediaPathToVirtualFolder'));
            });
        });
    }

    function onRemoveClick(btnRemovePath) {

        var button = btnRemovePath;
        var index = parseInt(button.getAttribute('data-index'));

        var virtualFolder = currentOptions.library;

        var location = virtualFolder.Locations[index];

        require(['confirm'], function (confirm) {

            confirm({

                title: Globalize.translate('HeaderRemoveMediaLocation'),
                text: Globalize.translate('MessageConfirmRemoveMediaLocation'),
                confirmText: Globalize.translate('ButtonDelete'),
                primary: 'cancel'

            }).then(function () {

                var refreshAfterChange = currentOptions.refresh;

                ApiClient.removeMediaPath(virtualFolder.Name, location, refreshAfterChange).then(function () {

                    hasChanges = true;
                    refreshLibraryFromServer(dom.parentWithClass(button, 'dlg-libraryeditor'));

                }, function () {

                    require(['toast'], function (toast) {
                        toast(Globalize.translate('DefaultErrorMessage'));
                    });
                });
            });
        });
    }

    function onListItemClick(e) {

        var btnRemovePath = dom.parentWithClass(e.target, 'btnRemovePath');
        if (btnRemovePath) {
            onRemoveClick(btnRemovePath);
            return;
        }

        var listItem = dom.parentWithClass(e.target, 'listItem');
        if (!listItem) {
            return;
        }

        var index = parseInt(listItem.getAttribute('data-index'));
        var page = dom.parentWithClass(listItem, 'dlg-libraryeditor');
        showDirectoryBrowser(page, index);
    }

    function getFolderHtml(pathInfo, index) {

        var html = '';

        html += '<div class="listItem lnkPath" data-index="' + index + '">';

        html += '<i class="listItemIcon md-icon">folder</i>';

        var cssClass = pathInfo.NetworkPath ? 'listItemBody two-line' : 'listItemBody';

        html += '<div class="' + cssClass + '">';

        html += '<h3 class="listItemBodyText">';
        html += pathInfo.Path;
        html += '</h3>';
        if (pathInfo.NetworkPath) {
            html += '<div class="listItemBodyText secondary">' + pathInfo.NetworkPath + '</div>';
        }
        html += '</div>';

        html += '<button is="paper-icon-button-light" class="listItemButton btnRemovePath" data-index="' + index + '"><i class="md-icon">remove_circle</i></button>';

        html += '</div>';

        return html;
    }

    function refreshLibraryFromServer(page) {

        ApiClient.getVirtualFolders().then(function (result) {

            var library = result.filter(function (f) {

                return f.Name == currentOptions.library.Name;

            })[0];

            if (library) {
                currentOptions.library = library;
                renderLibrary(page, currentOptions);
            }
        });
    }

    function renderLibrary(page, options) {

        var pathInfos = (options.library.LibraryOptions || {}).PathInfos || [];

        if (!pathInfos.length) {
            pathInfos = options.library.Locations.map(function (p) {
                return {
                    Path: p
                };
            });
        }

        var foldersHtml = pathInfos.map(getFolderHtml).join('');

        page.querySelector('.folderList').innerHTML = foldersHtml;

        var listItems = page.querySelectorAll('.listItem');
        for (var i = 0, length = listItems.length; i < length; i++) {
            listItems[i].addEventListener('click', onListItemClick);
        }
    }

    function onAddButtonClick() {

        var page = dom.parentWithClass(this, 'dlg-libraryeditor');

        showDirectoryBrowser(page);
    }

    function showDirectoryBrowser(context, listIndex) {

        require(['directorybrowser'], function (directoryBrowser) {

            var picker = new directoryBrowser();

            var pathInfos = (currentOptions.library.LibraryOptions || {}).PathInfos || [];
            var pathInfo = listIndex == null ? {} : (pathInfos[listIndex] || {});
            // legacy
            var location = listIndex == null ? null : (currentOptions.library.Locations[listIndex]);
            var originalPath = pathInfo.Path || location;

            picker.show({

                enableNetworkSharePath: true,
                pathReadOnly: listIndex != null,
                path: originalPath,
                networkSharePath: pathInfo.NetworkPath,
                callback: function (path, networkSharePath) {

                    if (path) {
                        if (originalPath) {
                            updateMediaLocation(context, originalPath, networkSharePath);
                        } else {
                            addMediaLocation(context, path, networkSharePath);
                        }
                    }
                    picker.close();
                }

            });
        });
    }

    function initEditor(dlg, options) {
        renderLibrary(dlg, options);

        dlg.querySelector('.btnAddFolder').addEventListener('click', onAddButtonClick);

        libraryoptionseditor.embed(dlg.querySelector('.libraryOptions'), options.library.CollectionType, options.library.LibraryOptions);
    }

    function onDialogClosing() {

        var dlg = this;

        var libraryOptions = libraryoptionseditor.getLibraryOptions(dlg.querySelector('.libraryOptions'));

        libraryOptions = Object.assign(currentOptions.library.LibraryOptions || {}, libraryOptions);

        ApiClient.updateVirtualFolderOptions(currentOptions.library.ItemId, libraryOptions);
    }

    function onDialogClosed() {

        Dashboard.hideLoadingMsg();

        // hardcoding this to true for now until libraryOptions are taken into account
        hasChanges = true;

        currentDeferred.resolveWith(null, [hasChanges]);
    }

    function editor() {

        var self = this;

        self.show = function (options) {

            var deferred = jQuery.Deferred();

            currentOptions = options;
            currentDeferred = deferred;
            hasChanges = false;

            var xhr = new XMLHttpRequest();
            xhr.open('GET', 'components/medialibraryeditor/medialibraryeditor.template.html', true);

            xhr.onload = function (e) {

                var template = this.response;
                var dlg = dialogHelper.createDialog({
                    size: 'medium',

                    // In (at least) chrome this is causing the text field to not be editable
                    modal: false,
                    removeOnClose: true,
                    scrollY: false
                });

                dlg.classList.add('dlg-libraryeditor');
                dlg.classList.add('ui-body-a');
                dlg.classList.add('background-theme-a');
                dlg.classList.add('formDialog');

                dlg.innerHTML = Globalize.translateDocument(template);

                dlg.querySelector('.formDialogHeaderTitle').innerHTML = options.library.Name;

                initEditor(dlg, options);

                dlg.addEventListener('closing', onDialogClosing);
                dlg.addEventListener('close', onDialogClosed);

                dialogHelper.open(dlg);

                dlg.querySelector('.btnCancel').addEventListener('click', function () {

                    dialogHelper.close(dlg);
                });

                refreshLibraryFromServer(dlg);
            }

            xhr.send();

            return deferred.promise();
        };
    }

    return editor;
});
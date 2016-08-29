define(['dialogHelper', 'dom', 'components/libraryoptionseditor/libraryoptionseditor', 'emby-button', 'listViewStyle', 'paper-icon-button-light', 'formDialogStyle'], function (dialogHelper, dom, libraryoptionseditor) {

    var currentDeferred;
    var hasChanges;
    var currentOptions;

    function addMediaLocation(page, path) {

        var virtualFolder = currentOptions.library;

        var refreshAfterChange = currentOptions.refresh;

        ApiClient.addMediaPath(virtualFolder.Name, path, refreshAfterChange).then(function () {

            hasChanges = true;
            refreshLibraryFromServer(page);

        }, function () {

            require(['toast'], function (toast) {
                toast(Globalize.translate('ErrorAddingMediaPathToVirtualFolder'));
            });
        });
    }

    function onRemoveClick() {

        var button = this;
        var index = parseInt(button.getAttribute('data-index'));

        var virtualFolder = currentOptions.library;

        var location = virtualFolder.Locations[index];

        require(['confirm'], function (confirm) {

            confirm(Globalize.translate('MessageConfirmRemoveMediaLocation'), Globalize.translate('HeaderRemoveMediaLocation')).then(function () {

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

    function getFolderHtml(path, index) {

        var html = '';

        html += '<div class="listItem lnkPath">';

        html += '<i class="listItemIcon md-icon">folder</i>';

        html += '<div class="listItemBody">';
        html += '<h3 class="listItemBodyText">';
        html += path;
        html += '</h3>';
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
        var foldersHtml = options.library.Locations.map(getFolderHtml).join('');

        page.querySelector('.folderList').innerHTML = foldersHtml;

        var btnRemovePath = page.querySelectorAll('.btnRemovePath');
        for (var i = 0, length = btnRemovePath.length; i < length; i++) {
            btnRemovePath[i].addEventListener('click', onRemoveClick);
        }
    }

    function onAddButtonClick() {

        var page = dom.parentWithClass(this, 'dlg-libraryeditor');

        require(['directorybrowser'], function (directoryBrowser) {

            var picker = new directoryBrowser();

            picker.show({

                callback: function (path) {

                    if (path) {
                        addMediaLocation(page, path);
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
                    size: 'small',

                    // In (at least) chrome this is causing the text field to not be editable
                    modal: false,
                    removeOnClose: true
                });

                dlg.classList.add('dlg-libraryeditor');
                dlg.classList.add('ui-body-a');
                dlg.classList.add('background-theme-a');

                dlg.innerHTML = Globalize.translateDocument(template);

                dlg.querySelector('.formDialogHeaderTitle').innerHTML = options.library.Name;

                document.body.appendChild(dlg);

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
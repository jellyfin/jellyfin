define([], function () {

    var currentDeferred;
    var hasChanges;
    var currentOptions;

    function addMediaLocation(page, path) {

        var virtualFolder = currentOptions.library;

        var refreshAfterChange = currentOptions.refresh;

        ApiClient.addMediaPath(virtualFolder.Name, path, refreshAfterChange).done(function () {

            hasChanges = true;
            refreshLibraryFromServer(page);

        }).fail(function () {

            Dashboard.showError(Globalize.translate('ErrorAddingMediaPathToVirtualFolder'));
        });
    }

    function onRemoveClick() {

        var button = this;
        var index = parseInt(button.getAttribute('data-index'));

        var virtualFolder = currentOptions.library;

        var location = virtualFolder.Locations[index];

        Dashboard.confirm(Globalize.translate('MessageConfirmRemoveMediaLocation'), Globalize.translate('HeaderRemoveMediaLocation'), function (confirmResult) {

            if (confirmResult) {

                var refreshAfterChange = currentOptions.refresh;

                ApiClient.removeMediaPath(virtualFolder.Name, location, refreshAfterChange).done(function () {

                    hasChanges = true;
                    refreshLibraryFromServer($(button).parents('.editorContent')[0]);

                }).fail(function () {

                    Dashboard.showError(Globalize.translate('DefaultErrorMessage'));
                });
            }
        });
    }

    function getFolderHtml(path, index) {

        var html = '';

        html += '<paper-icon-item role="menuitem" class="lnkPath">';

        html += '<paper-fab mini style="background:#52B54B;" icon="folder" item-icon></paper-fab>';

        html += '<paper-item-body>';
        html += path;
        html += '</paper-item-body>';

        html += '<paper-icon-button icon="remove-circle" class="btnRemovePath" data-index="' + index + '"></paper-icon-button>';

        html += '</paper-icon-item>';

        return html;
    }

    function refreshLibraryFromServer(page) {

        ApiClient.getVirtualFolders().done(function (result) {

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

        $(page.querySelectorAll('.btnRemovePath')).on('click', onRemoveClick);
    }

    function onAddButtonClick() {

        var page = $(this).parents('.editorContent')[0];

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

    function initEditor(page, options) {
        renderLibrary(page, options);

        $('.btnAddFolder', page).on('click', onAddButtonClick);
    }

    function onDialogClosed() {

        $(this).remove();
        Dashboard.hideLoadingMsg();
        currentDeferred.resolveWith(null, [hasChanges]);
    }

    function editor() {

        var self = this;

        self.show = function (options) {

            var deferred = DeferredBuilder.Deferred();

            currentOptions = options;
            currentDeferred = deferred;
            hasChanges = false;

            require(['components/paperdialoghelper'], function () {

                HttpClient.send({

                    type: 'GET',
                    url: 'components/medialibraryeditor/medialibraryeditor.template.html'

                }).done(function (template) {

                    var dlg = PaperDialogHelper.createDialog({
                        size: 'small',
                        theme: 'a',

                        // In (at least) chrome this is causing the text field to not be editable
                        modal: false
                    });

                    var html = '';
                    html += '<h2 class="dialogHeader">';
                    html += '<paper-fab icon="arrow-back" mini class="btnCloseDialog"></paper-fab>';

                    html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + options.library.Name + '</div>';
                    html += '</h2>';

                    html += '<div class="editorContent" style="max-width:800px;margin:auto;">';
                    html += Globalize.translateDocument(template);
                    html += '</div>';

                    dlg.innerHTML = html;
                    document.body.appendChild(dlg);

                    var editorContent = dlg.querySelector('.editorContent');
                    initEditor(editorContent, options);

                    $(dlg).on('iron-overlay-closed', onDialogClosed);

                    PaperDialogHelper.openWithHash(dlg, 'medialibraryeditor');

                    $('.btnCloseDialog', dlg).on('click', function () {

                        PaperDialogHelper.close(dlg);
                    });

                    refreshLibraryFromServer(editorContent);
                });

            });

            return deferred.promise();
        };
    }

    return editor;
});
define(['dialogHelper', 'jQuery', 'paper-fab', 'paper-item-body', 'paper-icon-item'], function (dialogHelper, $) {

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
                    refreshLibraryFromServer($(button).parents('.editorContent')[0]);

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
                    modal: false
                });

                dlg.classList.add('ui-body-a');
                dlg.classList.add('background-theme-a');
                dlg.classList.add('popupEditor');

                var html = '';
                html += '<h2 class="dialogHeader">';
                html += '<paper-fab icon="arrow-back" mini class="btnCloseDialog" tabindex="-1"></paper-fab>';

                html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + options.library.Name + '</div>';
                html += '</h2>';

                html += '<div class="editorContent" style="max-width:800px;margin:auto;">';
                html += Globalize.translateDocument(template);
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                var editorContent = dlg.querySelector('.editorContent');
                initEditor(editorContent, options);

                $(dlg).on('close', onDialogClosed);

                dialogHelper.open(dlg);

                $('.btnCloseDialog', dlg).on('click', function () {

                    dialogHelper.close(dlg);
                });

                refreshLibraryFromServer(editorContent);
            }

            xhr.send();

            return deferred.promise();
        };
    }

    return editor;
});
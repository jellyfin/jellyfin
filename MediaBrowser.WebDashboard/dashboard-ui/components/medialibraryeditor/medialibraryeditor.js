define(['dialogHelper', 'jQuery', 'emby-button', 'listViewStyle', 'paper-icon-button-light'], function (dialogHelper, $) {

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

        html += '<div class="listItem lnkPath">';

        html += '<i class="listItemIcon md-icon">folder</i>';

        html += '<div class="listItemBody">';
        html += '<h3 class="listItemBodyText">';
        html += path;
        html += '</h3>';
        html += '</div>';

        html += '<button is="paper-icon-button-light" class="btnRemovePath" data-index="' + index + '"><i class="md-icon">remove_circle</i></button>';

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
                html += '<button type="button" is="emby-button" icon="arrow-back" class="fab mini btnCloseDialog" tabindex="-1"><i class="md-icon">arrow_back</i></button>';

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
define(['components/paperdialoghelper', 'paper-dialog', 'paper-input', 'paper-fab', 'paper-item-body', 'paper-icon-item'], function (paperDialogHelper) {

    var currentDeferred;
    var hasChanges;
    var currentOptions;
    var paths = [];

    function onSubmit() {

        if (paths.length == 0) {
            Dashboard.alert({
                message: Globalize.translate('PleaseAddAtLeastOneFolder')
            });
            return false;
        }

        var form = this;
        var dlg = $(form).parents('paper-dialog')[0];

        var name = $('#txtValue', form).val();
        var type = $('#selectCollectionType', form).val();

        if (type == 'mixed') {
            type = null;
        }

        ApiClient.addVirtualFolder(name, type, currentOptions.refresh, paths).then(function () {

            hasChanges = true;
            paperDialogHelper.close(dlg);

        }, function () {

            Dashboard.alert(Globalize.translate('ErrorAddingMediaPathToVirtualFolder'));
        });

        return false;
    }

    function getCollectionTypeOptionsHtml(collectionTypeOptions) {

        return collectionTypeOptions.filter(function (i) {

            return i.isSelectable !== false;

        }).map(function (i) {

            return '<option value="' + i.value + '">' + i.name + '</option>';

        }).join("");
    }

    function initEditor(page, collectionTypeOptions) {

        $('#selectCollectionType', page).html(getCollectionTypeOptionsHtml(collectionTypeOptions)).val('').on('change', function () {

            if (this.value == 'mixed') {
                return;
            }

            var dlg = $(this).parents('paper-dialog')[0];

            var index = this.selectedIndex;
            if (index != -1) {

                var name = this.options[index].innerHTML
                    .replace('*', '')
                    .replace('&amp;', '&');

                var value = this.value;

                $('#txtValue', dlg).val(name);

                var folderOption = collectionTypeOptions.filter(function (i) {

                    return i.value == value;

                })[0];

                $('.collectionTypeFieldDescription', dlg).html(folderOption.message || '');
            }
        });

        $('.btnAddFolder', page).on('click', onAddButtonClick);
        $('form', page).off('submit', onSubmit).on('submit', onSubmit);
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

    function renderPaths(page) {
        var foldersHtml = paths.map(getFolderHtml).join('');

        var folderList = page.querySelector('.folderList');
        folderList.innerHTML = foldersHtml;

        if (foldersHtml) {
            folderList.classList.remove('hide');
        } else {
            folderList.classList.add('hide');
        }

        $(page.querySelectorAll('.btnRemovePath')).on('click', onRemoveClick);
    }

    function addMediaLocation(page, path) {

        if (paths.filter(function (p) {

            return p.toLowerCase() == path.toLowerCase();

        }).length == 0) {
            paths.push(path);
            renderPaths(page);
        }
    }

    function onRemoveClick() {

        var button = this;
        var index = parseInt(button.getAttribute('data-index'));

        var location = paths[index];
        paths = paths.filter(function (p) {

            return p.toLowerCase() != location.toLowerCase();
        });
        var page = $(this).parents('.editorContent')[0];
        renderPaths(page);
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

            var xhr = new XMLHttpRequest();
            xhr.open('GET', 'components/medialibrarycreator/medialibrarycreator.template.html', true);

            xhr.onload = function (e) {

                var template = this.response;
                var dlg = paperDialogHelper.createDialog({
                    size: 'small',
                    theme: 'a',

                    // In (at least) chrome this is causing the text field to not be editable
                    modal: false
                });

                var html = '';
                html += '<h2 class="dialogHeader">';
                html += '<paper-fab icon="arrow-back" mini class="btnCloseDialog"></paper-fab>';

                var title = Globalize.translate('ButtonAddMediaLibrary');

                html += '<div style="display:inline-block;margin-left:.6em;vertical-align:middle;">' + title + '</div>';
                html += '</h2>';

                html += '<div class="editorContent" style="max-width:800px;margin:auto;">';
                html += Globalize.translateDocument(template);
                html += '</div>';

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                var editorContent = dlg.querySelector('.editorContent');
                initEditor(editorContent, options.collectionTypeOptions);

                $(dlg).on('iron-overlay-closed', onDialogClosed);

                paperDialogHelper.open(dlg);

                $('.btnCloseDialog', dlg).on('click', function () {

                    paperDialogHelper.close(dlg);
                });

                paths = [];
                renderPaths(editorContent);
            }

            xhr.send();

            return deferred.promise();
        };
    }

    return editor;
});
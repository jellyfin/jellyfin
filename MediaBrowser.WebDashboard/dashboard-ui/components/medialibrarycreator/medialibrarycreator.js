define([], function () {

    var currentDeferred;
    var hasChanges;
    var currentOptions;

    function onSubmit() {

        var form = this;
        var dlg = $(form).parents('paper-dialog')[0];

        var name = $('#txtValue', form).val();
        var type = $('#selectCollectionType', form).val();
        var path = $('#txtPath', form).val();

        if (type == 'mixed') {
            type = null;
        }

        ApiClient.addVirtualFolder(name, type, currentOptions.refresh, path).done(function () {

            hasChanges = true;
            PaperDialogHelper.close(dlg);

        }).fail(function () {

            Dashboard.showError(Globalize.translate('DefaultErrorMessage'));
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

        $('#btnSelectPath').on('click', function () {

            require(['directorybrowser'], function (directoryBrowser) {

                var picker = new directoryBrowser();

                picker.show({

                    callback: function (path) {
                        if (path) {
                            $('#txtPath', page).val(path);
                        }
                        picker.close();
                    }

                });
            });
        });

        $('form', page).off('submit', onSubmit).on('submit', onSubmit);
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
                    url: 'components/medialibrarycreator/medialibrarycreator.template.html'

                }).done(function (template) {

                    var dlg = PaperDialogHelper.createDialog({
                        size: 'small',
                        theme: 'a'
                    });

                    var html = '';
                    html += '<h2 class="dialogHeader">';
                    html += '<paper-fab icon="arrow-back" class="mini btnCloseDialog"></paper-fab>';

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

                    PaperDialogHelper.openWithHash(dlg, 'medialibrarycreator');

                    $('.btnCloseDialog', dlg).on('click', function () {

                        PaperDialogHelper.close(dlg);
                    });
                });

            });

            return deferred.promise();
        };
    }

    return editor;
});
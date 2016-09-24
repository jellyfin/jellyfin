define(['dialogHelper', 'jQuery', 'components/libraryoptionseditor/libraryoptionseditor', 'emby-input', 'emby-select', 'paper-icon-button-light', 'listViewStyle', 'formDialogStyle'], function (dialogHelper, $, libraryoptionseditor) {

    var currentDeferred;
    var hasChanges;
    var currentOptions;
    var pathInfos = [];

    function onSubmit() {

        if (pathInfos.length == 0) {
            require(['alert'], function (alert) {
                alert({
                    text: Globalize.translate('PleaseAddAtLeastOneFolder'),
                    type: 'error'
                });
            });
            return false;
        }

        var form = this;
        var dlg = $(form).parents('.dialog')[0];

        var name = $('#txtValue', form).val();
        var type = $('#selectCollectionType', form).val();

        if (type == 'mixed') {
            type = null;
        }

        var libraryOptions = libraryoptionseditor.getLibraryOptions(dlg.querySelector('.libraryOptions'));

        libraryOptions.PathInfos = pathInfos;

        ApiClient.addVirtualFolder(name, type, currentOptions.refresh, libraryOptions).then(function () {

            hasChanges = true;
            dialogHelper.close(dlg);

        }, function () {

            require(['toast'], function (toast) {
                toast(Globalize.translate('ErrorAddingMediaPathToVirtualFolder'));
            });
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

            var value = this.value;

            var dlg = $(this).parents('.dialog')[0];

            libraryoptionseditor.setContentType(dlg.querySelector('.libraryOptions'), (value == 'mixed' ? '' : value));

            if (value) {
                dlg.querySelector('.libraryOptions').classList.remove('hide');
            } else {
                dlg.querySelector('.libraryOptions').classList.add('hide');
            }

            if (value == 'mixed') {
                return;
            }

            var index = this.selectedIndex;
            if (index != -1) {

                var name = this.options[index].innerHTML
                    .replace('*', '')
                    .replace('&amp;', '&');

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

        var page = $(this).parents('.dlg-librarycreator')[0];

        require(['directorybrowser'], function (directoryBrowser) {

            var picker = new directoryBrowser();

            picker.show({

                enableNetworkSharePath: true,
                callback: function (path, networkSharePath) {

                    if (path) {
                        addMediaLocation(page, path, networkSharePath);
                    }
                    picker.close();
                }

            });
        });
    }

    function getFolderHtml(pathInfo, index) {

        var html = '';

        html += '<div class="listItem lnkPath">';

        html += '<i class="listItemIcon md-icon">folder</i>';

        var cssClass = pathInfo.NetworkPath ? 'listItemBody two-line' : 'listItemBody';

        html += '<div class="' + cssClass + '">';
        html += '<div class="listItemBodyText">' + pathInfo.Path + '</div>';

        if (pathInfo.NetworkPath) {
            html += '<div class="listItemBodyText secondary">' + pathInfo.NetworkPath + '</div>';
        }
        html += '</div>';

        html += '<button is="paper-icon-button-light"" class="listItemButton btnRemovePath" data-index="' + index + '"><i class="md-icon">remove_circle</i></button>';

        html += '</div>';

        return html;
    }

    function renderPaths(page) {
        var foldersHtml = pathInfos.map(getFolderHtml).join('');

        var folderList = page.querySelector('.folderList');
        folderList.innerHTML = foldersHtml;

        if (foldersHtml) {
            folderList.classList.remove('hide');
        } else {
            folderList.classList.add('hide');
        }

        $(page.querySelectorAll('.btnRemovePath')).on('click', onRemoveClick);
    }

    function addMediaLocation(page, path, networkSharePath) {

        if (pathInfos.filter(function (p) {

            return p.Path.toLowerCase() == path.toLowerCase();

        }).length == 0) {

            var pathInfo = {
                Path: path
            };
            if (networkSharePath) {
                pathInfo.NetworkPath = networkSharePath;
            }
            pathInfos.push(pathInfo);
            renderPaths(page);
        }
    }

    function onRemoveClick() {

        var button = this;
        var index = parseInt(button.getAttribute('data-index'));

        var location = pathInfos[index];
        pathInfos = pathInfos.filter(function (p) {

            return p.Path.toLowerCase() != location.toLowerCase();
        });
        var page = $(this).parents('.dlg-librarycreator')[0];
        renderPaths(page);
    }

    function onDialogClosed() {

        Dashboard.hideLoadingMsg();
        currentDeferred.resolveWith(null, [hasChanges]);
    }

    function initLibraryOptions(dlg) {
        libraryoptionseditor.embed(dlg.querySelector('.libraryOptions')).then(function () {
            $('#selectCollectionType', dlg).trigger('change');
        });
    }

    function editor() {

        var self = this;

        self.show = function (options) {

            var deferred = jQuery.Deferred();

            currentOptions = options;
            currentDeferred = deferred;
            hasChanges = false;

            var xhr = new XMLHttpRequest();
            xhr.open('GET', 'components/medialibrarycreator/medialibrarycreator.template.html', true);

            xhr.onload = function (e) {

                var template = this.response;
                var dlg = dialogHelper.createDialog({
                    size: 'medium',

                    // In (at least) chrome this is causing the text field to not be editable
                    modal: false,

                    removeOnClose: true,
                    scrollY: false
                });

                dlg.classList.add('ui-body-a');
                dlg.classList.add('background-theme-a');
                dlg.classList.add('dlg-librarycreator');
                dlg.classList.add('formDialog');

                dlg.innerHTML = Globalize.translateDocument(template);

                initEditor(dlg, options.collectionTypeOptions);

                dlg.addEventListener('close', onDialogClosed);

                dialogHelper.open(dlg);

                dlg.querySelector('.btnCancel').addEventListener('click', function () {

                    dialogHelper.close(dlg);
                });

                pathInfos = [];
                renderPaths(dlg);
                initLibraryOptions(dlg);
            }

            xhr.send();

            return deferred.promise();
        };
    }

    return editor;
});
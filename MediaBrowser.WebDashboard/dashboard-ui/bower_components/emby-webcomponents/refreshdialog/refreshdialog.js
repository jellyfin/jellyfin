define(['shell', 'dialogHelper', 'loading', 'layoutManager', 'connectionManager', 'embyRouter', 'globalize', 'emby-input', 'emby-checkbox', 'paper-icon-button-light', 'emby-select', 'material-icons', 'css!./../formdialog', 'emby-button'], function (shell, dialogHelper, loading, layoutManager, connectionManager, embyRouter, globalize) {
    'use strict';

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function getEditorHtml() {

        var html = '';

        html += '<div class="formDialogContent smoothScrollY" style="padding-top:2em;">';
        html += '<div class="dialogContentInner dialog-content-centered">';
        html += '<form style="margin:auto;">';

        html += '<div class="fldSelectPlaylist selectContainer">';
        html += '<select is="emby-select" id="selectMetadataRefreshMode" label="' + globalize.translate('sharedcomponents#LabelRefreshMode') + '">';
        html += '<option value="missing">' + globalize.translate('sharedcomponents#SearchForMissingMetadata') + '</option>';
        html += '<option value="all" selected>' + globalize.translate('sharedcomponents#ReplaceAllMetadata') + '</option>';
        html += '</select>';
        html += '</div>';

        html += '<label class="checkboxContainer">';
        html += '<input type="checkbox" is="emby-checkbox" class="chkReplaceImages" />';
        html += '<span>' + globalize.translate('sharedcomponents#ReplaceExistingImages') + '</span>';
        html += '</label>';

        html += '<div class="fieldDescription">';
        html += globalize.translate('sharedcomponents#RefreshDialogHelp');
        html += '</div>';

        html += '<input type="hidden" class="fldSelectedItemIds" />';

        html += '<br />';
        html += '<div class="formDialogFooter">';
        html += '<button is="emby-button" type="submit" class="raised btnSubmit block formDialogFooterItem button-submit">' + globalize.translate('sharedcomponents#ButtonOk') + '</button>';
        html += '</div>';

        html += '</form>';
        html += '</div>';
        html += '</div>';

        return html;
    }

    function centerFocus(elem, horiz, on) {
        require(['scrollHelper'], function (scrollHelper) {
            var fn = on ? 'on' : 'off';
            scrollHelper.centerFocus[fn](elem, horiz);
        });
    }

    return function (options) {

        var self = this;

        function onSubmit(e) {

            loading.show();

            var dlg = parentWithClass(this, 'dialog');

            var apiClient = connectionManager.getApiClient(options.serverId);

            var replaceAllImages = dlg.querySelector('.chkReplaceImages').checked;
            var replaceAllMetadata = dlg.querySelector('#selectMetadataRefreshMode').value === 'all';

            options.itemIds.forEach(function (itemId) {
                apiClient.refreshItem(itemId, {

                    Recursive: true,
                    ImageRefreshMode: 'FullRefresh',
                    MetadataRefreshMode: 'FullRefresh',
                    ReplaceAllImages: replaceAllImages,
                    ReplaceAllMetadata: replaceAllMetadata
                });
            });

            dialogHelper.close(dlg);

            require(['toast'], function (toast) {
                toast(globalize.translate('sharedcomponents#RefreshQueued'));
            });

            loading.hide();

            e.preventDefault();
            return false;
        }

        function initEditor(content, items) {

            content.querySelector('form').addEventListener('submit', onSubmit);
        }

        self.show = function () {

            var dialogOptions = {
                removeOnClose: true,
                scrollY: false
            };

            if (layoutManager.tv) {
                dialogOptions.size = 'fullscreen';
            } else {
                dialogOptions.size = 'small';
            }

            var dlg = dialogHelper.createDialog(dialogOptions);

            dlg.classList.add('formDialog');

            var html = '';
            var title = globalize.translate('sharedcomponents#RefreshMetadata');

            html += '<div class="formDialogHeader">';
            html += '<button is="paper-icon-button-light" class="btnCancel autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
            html += '<h3 class="formDialogHeaderTitle">';
            html += title;
            html += '</h3>';

            html += '</div>';

            html += getEditorHtml();

            dlg.innerHTML = html;

            initEditor(dlg);

            dlg.querySelector('.btnCancel').addEventListener('click', function () {

                dialogHelper.close(dlg);
            });

            if (layoutManager.tv) {
                centerFocus(dlg.querySelector('.formDialogContent'), false, true);
            }

            return new Promise(function (resolve, reject) {

                if (layoutManager.tv) {
                    centerFocus(dlg.querySelector('.formDialogContent'), false, false);
                }

                dlg.addEventListener('close', resolve);
                dialogHelper.open(dlg);
            });
        };
    };
});
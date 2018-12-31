define(['shell', 'dialogHelper', 'loading', 'layoutManager', 'connectionManager', 'appRouter', 'globalize', 'emby-input', 'emby-checkbox', 'paper-icon-button-light', 'emby-select', 'material-icons', 'css!./../formdialog', 'emby-button'], function (shell, dialogHelper, loading, layoutManager, connectionManager, appRouter, globalize) {
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
        html += '<option value="scan">' + globalize.translate('sharedcomponents#ScanForNewAndUpdatedFiles') + '</option>';
        html += '<option value="missing">' + globalize.translate('sharedcomponents#SearchForMissingMetadata') + '</option>';
        html += '<option value="all" selected>' + globalize.translate('sharedcomponents#ReplaceAllMetadata') + '</option>';
        html += '</select>';
        html += '</div>';

        html += '<label class="checkboxContainer hide fldReplaceExistingImages">';
        html += '<input type="checkbox" is="emby-checkbox" class="chkReplaceImages" />';
        html += '<span>' + globalize.translate('sharedcomponents#ReplaceExistingImages') + '</span>';
        html += '</label>';

        html += '<div class="fieldDescription">';
        html += globalize.translate('sharedcomponents#RefreshDialogHelp');
        html += '</div>';

        html += '<input type="hidden" class="fldSelectedItemIds" />';

        html += '<br />';
        html += '<div class="formDialogFooter">';
        html += '<button is="emby-button" type="submit" class="raised btnSubmit block formDialogFooterItem button-submit">' + globalize.translate('sharedcomponents#Refresh') + '</button>';
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

    function onSubmit(e) {

        loading.show();

        var instance = this;
        var dlg = parentWithClass(e.target, 'dialog');
        var options = instance.options;

        var apiClient = connectionManager.getApiClient(options.serverId);

        var replaceAllMetadata = dlg.querySelector('#selectMetadataRefreshMode').value === 'all';

        var mode = dlg.querySelector('#selectMetadataRefreshMode').value === 'scan' ? 'Default' : 'FullRefresh';
        var replaceAllImages = mode === 'FullRefresh' && dlg.querySelector('.chkReplaceImages').checked;

        options.itemIds.forEach(function (itemId) {
            apiClient.refreshItem(itemId, {

                Recursive: true,
                ImageRefreshMode: mode,
                MetadataRefreshMode: mode,
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

    function RefreshDialog(options) {
        this.options = options;
    }

    RefreshDialog.prototype.show = function () {

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

        dlg.querySelector('form').addEventListener('submit', onSubmit.bind(this));

        dlg.querySelector('#selectMetadataRefreshMode').addEventListener('change', function () {

            if (this.value === 'scan') {
                dlg.querySelector('.fldReplaceExistingImages').classList.add('hide');
            } else {
                dlg.querySelector('.fldReplaceExistingImages').classList.remove('hide');
            }
        });

        if (this.options.mode) {
            dlg.querySelector('#selectMetadataRefreshMode').value = this.options.mode;
        }

        dlg.querySelector('#selectMetadataRefreshMode').dispatchEvent(new CustomEvent('change'));

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

    return RefreshDialog;
});
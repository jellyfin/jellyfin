define(['dialogHelper', 'globalize', 'layoutManager', 'mediaInfo', 'apphost', 'connectionManager', 'require', 'loading', 'scrollHelper', 'imageLoader', 'scrollStyles', 'emby-button', 'emby-collapse', 'emby-input', 'paper-icon-button-light', 'css!./../formdialog', 'css!./recordingcreator', 'material-icons'], function (dialogHelper, globalize, layoutManager, mediaInfo, appHost, connectionManager, require, loading, scrollHelper, imageLoader) {
    'use strict';

    var currentDialog;
    var recordingDeleted = false;
    var currentItemId;
    var currentServerId;
    var currentResolve;

    function deleteTimer(apiClient, timerId) {

        return new Promise(function (resolve, reject) {

            require(['recordingHelper'], function (recordingHelper) {

                recordingHelper.cancelTimerWithConfirmation(timerId, apiClient.serverId()).then(resolve, reject);
            });
        });
    }

    function renderTimer(context, item, apiClient) {

        var program = item.ProgramInfo || {};

        context.querySelector('#txtPrePaddingMinutes').value = item.PrePaddingSeconds / 60;
        context.querySelector('#txtPostPaddingMinutes').value = item.PostPaddingSeconds / 60;

        loading.hide();
    }

    function closeDialog(isDeleted) {

        recordingDeleted = isDeleted;

        dialogHelper.close(currentDialog);
    }

    function onSubmit(e) {

        var form = this;

        var apiClient = connectionManager.getApiClient(currentServerId);

        apiClient.getLiveTvTimer(currentItemId).then(function (item) {
            item.PrePaddingSeconds = form.querySelector('#txtPrePaddingMinutes').value * 60;
            item.PostPaddingSeconds = form.querySelector('#txtPostPaddingMinutes').value * 60;
            apiClient.updateLiveTvTimer(item).then(currentResolve);
        });

        e.preventDefault();

        // Disable default form submission
        return false;
    }

    function init(context) {

        context.querySelector('.btnCancel').addEventListener('click', function () {

            closeDialog(false);
        });

        context.querySelector('.btnCancelRecording').addEventListener('click', function () {

            var apiClient = connectionManager.getApiClient(currentServerId);
            deleteTimer(apiClient, currentItemId).then(function () {
                closeDialog(true);
            });
        });

        context.querySelector('form').addEventListener('submit', onSubmit);
    }

    function reload(context, id) {

        loading.show();
        currentItemId = id;

        var apiClient = connectionManager.getApiClient(currentServerId);
        apiClient.getLiveTvTimer(id).then(function (result) {

            renderTimer(context, result, apiClient);
            loading.hide();
        });
    }

    function showEditor(itemId, serverId, options) {

        return new Promise(function (resolve, reject) {

            recordingDeleted = false;
            currentServerId = serverId;
            loading.show();
            options = options || {};
            currentResolve = resolve;

            require(['text!./recordingeditor.template.html'], function (template) {

                var dialogOptions = {
                    removeOnClose: true,
                    scrollY: false
                };

                if (layoutManager.tv) {
                    dialogOptions.size = 'fullscreen';
                } else {
                }

                var dlg = dialogHelper.createDialog(dialogOptions);

                dlg.classList.add('formDialog');
                dlg.classList.add('recordingDialog');

                if (!layoutManager.tv) {
                    dlg.style['min-width'] = '20%';
                    dlg.classList.add('dialog-fullscreen-lowres');
                }

                var html = '';

                html += globalize.translateDocument(template, 'sharedcomponents');

                dlg.innerHTML = html;

                if (options.enableCancel === false) {
                    dlg.querySelector('.formDialogFooter').classList.add('hide');
                }

                currentDialog = dlg;

                dlg.addEventListener('closing', function () {

                    if (!recordingDeleted) {
                        dlg.querySelector('.btnSubmit').click();
                    }
                });

                dlg.addEventListener('close', function () {

                    if (recordingDeleted) {
                        resolve({
                            updated: true,
                            deleted: true
                        });
                    }
                });

                if (layoutManager.tv) {
                    scrollHelper.centerFocus.on(dlg.querySelector('.formDialogContent'), false);
                }

                init(dlg);

                reload(dlg, itemId);

                dialogHelper.open(dlg);
            });
        });
    }

    return {
        show: showEditor
    };
});
define(['dialogHelper', 'globalize', 'layoutManager', 'mediaInfo', 'apphost', 'connectionManager', 'require', 'loading', 'scrollHelper', 'imageLoader', 'scrollStyles', 'emby-button', 'emby-collapse', 'emby-input', 'paper-icon-button-light', 'css!./../formdialog', 'css!./recordingcreator', 'material-icons'], function (dialogHelper, globalize, layoutManager, mediaInfo, appHost, connectionManager, require, loading, scrollHelper, imageLoader) {

    var currentDialog;
    var recordingUpdated = false;
    var recordingDeleted = false;
    var currentItemId;
    var currentServerId;

    function deleteTimer(apiClient, timerId) {

        return new Promise(function (resolve, reject) {

            require(['confirm'], function (confirm) {

                confirm(globalize.translate('sharedcomponents#MessageConfirmRecordingCancellation'), globalize.translate('sharedcomponents#HeaderConfirmRecordingCancellation')).then(function () {

                    loading.show();

                    apiClient.cancelLiveTvTimer(timerId).then(function () {

                        require(['toast'], function (toast) {
                            toast(globalize.translate('sharedcomponents#RecordingCancelled'));
                        });

                        loading.hide();
                        resolve();
                    });
                });
            });
        });
    }

    function getImageUrl(item, apiClient, imageHeight) {

        var imageTags = item.ImageTags || {};

        if (item.PrimaryImageTag) {
            imageTags.Primary = item.PrimaryImageTag;
        }

        if (imageTags.Primary) {

            return apiClient.getScaledImageUrl(item.Id, {
                type: "Primary",
                maxHeight: imageHeight,
                tag: item.ImageTags.Primary
            });
        }
        else if (imageTags.Thumb) {

            return apiClient.getScaledImageUrl(item.Id, {
                type: "Thumb",
                maxHeight: imageHeight,
                tag: item.ImageTags.Thumb
            });
        }

        return null;
    }

    function renderTimer(context, item, apiClient) {

        var program = item.ProgramInfo || {};

        var imgUrl = getImageUrl(program, apiClient, 200);
        var imageContainer = context.querySelector('.recordingDialog-imageContainer');

        if (imgUrl) {
            imageContainer.innerHTML = '<img src="' + require.toUrl('.').split('?')[0] + '/empty.png" data-src="' + imgUrl + '" class="recordingDialog-img lazy" />';
            imageContainer.classList.remove('hide');

            imageLoader.lazyChildren(imageContainer);
        } else {
            imageContainer.innerHTML = '';
            imageContainer.classList.add('hide');
        }

        context.querySelector('.recordingDialog-itemName').innerHTML = item.Name;

        context.querySelector('.itemGenres').innerHTML = (program.Genres || []).join(' / ');
        context.querySelector('.itemOverview').innerHTML = program.Overview || '';

        context.querySelector('.itemMiscInfoPrimary').innerHTML = mediaInfo.getPrimaryMediaInfoHtml(program);
        context.querySelector('.itemMiscInfoSecondary').innerHTML = mediaInfo.getSecondaryMediaInfoHtml(program);

        context.querySelector('#txtPrePaddingMinutes').value = item.PrePaddingSeconds / 60;
        context.querySelector('#txtPostPaddingMinutes').value = item.PostPaddingSeconds / 60;

        var timerStausElem = context.querySelector('.timerStatus');

        if (item.Status == 'New') {
            timerStausElem.classList.add('hide');
        } else {
            timerStausElem.classList.remove('hide');
            timerStausElem.innerHTML = 'Status:&nbsp;&nbsp;&nbsp;' + item.Status;
        }

        loading.hide();
    }

    function closeDialog(isSubmitted, isDeleted) {

        recordingUpdated = isSubmitted;
        recordingDeleted = isDeleted;
        dialogHelper.close(currentDialog);
    }

    function onSubmit(e) {

        loading.show();

        var form = this;

        var apiClient = connectionManager.getApiClient(currentServerId);

        apiClient.getLiveTvTimer(currentItemId).then(function (item) {

            item.PrePaddingSeconds = form.querySelector('#txtPrePaddingMinutes').value * 60;
            item.PostPaddingSeconds = form.querySelector('#txtPostPaddingMinutes').value * 60;
            apiClient.updateLiveTvTimer(item).then(function () {
                loading.hide();
                require(['toast'], function (toast) {
                    toast(Globalize.translate('MessageRecordingSaved'));
                    closeDialog(true);
                });
            });
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
                closeDialog(true, true);
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

    function showEditor(itemId, serverId) {

        return new Promise(function (resolve, reject) {

            recordingUpdated = false;
            recordingDeleted = false;
            currentServerId = serverId;
            loading.show();

            require(['text!./recordingeditor.template.html'], function (template) {

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
                dlg.classList.add('recordingDialog');

                var html = '';

                html += globalize.translateDocument(template, 'sharedcomponents');

                dlg.innerHTML = html;

                currentDialog = dlg;

                dlg.addEventListener('close', function () {

                    if (recordingUpdated) {
                        resolve({
                            updated: true,
                            deleted: recordingDeleted
                        });
                    } else {
                        reject();
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
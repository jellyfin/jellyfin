define(['dialogHelper', 'globalize', 'layoutManager', 'mediaInfo', 'apphost', 'connectionManager', 'require', 'loading', 'scrollHelper', 'scrollStyles', 'emby-button', 'emby-collapse', 'emby-input', 'paper-icon-button-light', 'css!./../formdialog', 'css!./recordingcreator', 'material-icons'], function (dialogHelper, globalize, layoutManager, mediaInfo, appHost, connectionManager, require, loading, scrollHelper) {

    var currentDialog;
    var recordingUpdated = false;
    var currentItemId;
    var currentServerId;

    function renderTimer(context, item) {

        var programInfo = item.ProgramInfo || {};

        context.querySelector('.itemName').innerHTML = item.Name;
        context.querySelector('.itemEpisodeName').innerHTML = programInfo.EpisodeTitle || '';

        context.querySelector('.itemGenres').innerHTML = (programInfo.Genres || []).join(' / ');
        context.querySelector('.itemOverview').innerHTML = programInfo.Overview || '';

        var timerPageImageContainer = context.querySelector('.timerPageImageContainer');

        context.querySelector('.itemMiscInfoPrimary').innerHTML = mediaInfo.getPrimaryMediaInfoHtml(programInfo);
        context.querySelector('.itemMiscInfoSecondary').innerHTML = mediaInfo.getSecondaryMediaInfoHtml(programInfo);

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

    function closeDialog(isSubmitted) {

        recordingUpdated = isSubmitted;
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

        context.querySelector('form').addEventListener('submit', onSubmit);

        context.querySelector('.btnHeaderSave').addEventListener('click', function (e) {

            context.querySelector('.btnSubmit').click();
        });
    }

    function reload(context, id) {

        loading.show();
        currentItemId = id;

        var apiClient = connectionManager.getApiClient(currentServerId);
        apiClient.getLiveTvTimer(id).then(function (result) {

            renderTimer(context, result);
            loading.hide();
        });
    }

    function showEditor(itemId, serverId) {

        return new Promise(function (resolve, reject) {

            recordingUpdated = false;
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
                document.body.appendChild(dlg);

                currentDialog = dlg;

                dlg.addEventListener('close', function () {

                    if (recordingUpdated) {
                        resolve({
                            updated: true,
                            deleted: false
                        });
                    } else {
                        reject();
                    }
                });

                if (layoutManager.tv) {
                    scrollHelper.centerFocus.on(dlg.querySelector('.dialogContent'), false);
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
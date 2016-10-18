define(['dialogHelper', 'globalize', 'layoutManager', 'mediaInfo', 'apphost', 'connectionManager', 'require', 'loading', 'scrollHelper', 'datetime', 'imageLoader', 'recordingFields', 'events', 'emby-checkbox', 'emby-button', 'emby-collapse', 'emby-input', 'paper-icon-button-light', 'css!./../formdialog', 'css!./recordingcreator', 'material-icons'], function (dialogHelper, globalize, layoutManager, mediaInfo, appHost, connectionManager, require, loading, scrollHelper, datetime, imageLoader, recordingFields, events) {
    'use strict';

    var currentDialog;
    var closeAction;
    var currentRecordingFields;

    function closeDialog() {

        dialogHelper.close(currentDialog);
    }

    function init(context) {

        context.querySelector('.btnPlay').addEventListener('click', function () {

            closeAction = 'play';
            closeDialog();
        });

        context.querySelector('.btnCancel').addEventListener('click', function () {

            closeAction = null;
            closeDialog();
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

    function renderRecording(context, defaultTimer, program, apiClient, refreshRecordingStateOnly) {

        if (!refreshRecordingStateOnly) {
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

            context.querySelector('.recordingDialog-itemName').innerHTML = program.Name;
            context.querySelector('.formDialogHeaderTitle').innerHTML = program.Name;
            context.querySelector('.itemGenres').innerHTML = (program.Genres || []).join(' / ');
            context.querySelector('.itemOverview').innerHTML = program.Overview || '';

            var formDialogFooter = context.querySelector('.formDialogFooter');
            var now = new Date();
            if (now >= datetime.parseISO8601Date(program.StartDate, true) && now < datetime.parseISO8601Date(program.EndDate, true)) {
                formDialogFooter.classList.remove('hide');
            } else {
                formDialogFooter.classList.add('hide');
            }

            context.querySelector('.itemMiscInfoPrimary').innerHTML = mediaInfo.getPrimaryMediaInfoHtml(program);
        }

        context.querySelector('.itemMiscInfoSecondary').innerHTML = mediaInfo.getSecondaryMediaInfoHtml(program, {
        });

        loading.hide();
    }

    function reload(context, programId, serverId, refreshRecordingStateOnly) {

        loading.show();

        var apiClient = connectionManager.getApiClient(serverId);

        var promise1 = apiClient.getNewLiveTvTimerDefaults({ programId: programId });
        var promise2 = apiClient.getLiveTvProgram(programId, apiClient.getCurrentUserId());

        Promise.all([promise1, promise2]).then(function (responses) {

            var defaults = responses[0];
            var program = responses[1];

            renderRecording(context, defaults, program, apiClient, refreshRecordingStateOnly);
        });
    }

    function executeCloseAction(action, programId, serverId) {

        if (action === 'play') {

            require(['playbackManager'], function (playbackManager) {

                var apiClient = connectionManager.getApiClient(serverId);

                apiClient.getLiveTvProgram(programId, apiClient.getCurrentUserId()).then(function (item) {

                    playbackManager.play(item.ChannelId, serverId);
                });
            });
            return;
        }
    }

    function showEditor(itemId, serverId) {

        return new Promise(function (resolve, reject) {

            closeAction = null;

            loading.show();

            require(['text!./recordingcreator.template.html'], function (template) {

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

                function onRecordingChanged() {
                    reload(dlg, itemId, serverId, true);
                }

                dlg.addEventListener('close', function () {

                    events.off(currentRecordingFields, 'recordingchanged', onRecordingChanged);
                    executeCloseAction(closeAction, itemId, serverId);

                    if (currentRecordingFields && currentRecordingFields.hasChanged()) {
                        resolve();
                    } else {
                        reject();
                    }
                });

                if (layoutManager.tv) {
                    scrollHelper.centerFocus.on(dlg.querySelector('.formDialogContent'), false);
                }

                init(dlg);

                reload(dlg, itemId, serverId);

                currentRecordingFields = new recordingFields({
                    parent: dlg.querySelector('.recordingFields'),
                    programId: itemId,
                    serverId: serverId
                });

                events.on(currentRecordingFields, 'recordingchanged', onRecordingChanged);

                dialogHelper.open(dlg);
            });
        });
    }

    return {
        show: showEditor
    };
});
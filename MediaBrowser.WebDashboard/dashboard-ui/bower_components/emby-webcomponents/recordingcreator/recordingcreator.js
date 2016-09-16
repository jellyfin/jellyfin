define(['dialogHelper', 'globalize', 'layoutManager', 'mediaInfo', 'apphost', 'connectionManager', 'require', 'loading', 'scrollHelper', 'datetime', 'imageLoader', 'shell', 'emby-checkbox', 'emby-button', 'emby-collapse', 'emby-input', 'paper-icon-button-light', 'css!./../formdialog', 'css!./recordingcreator', 'material-icons'], function (dialogHelper, globalize, layoutManager, mediaInfo, appHost, connectionManager, require, loading, scrollHelper, datetime, imageLoader, shell) {

    var currentProgramId;
    var currentServerId;
    var currentDialog;
    var recordingCreated = false;
    var closeAction;

    function hideSeriesRecordingFields(context) {

        slideUpToHide(context.querySelector('.seriesFields'));
        context.querySelector('.btnSubmit').classList.remove('hide');
        context.querySelector('.supporterContainer').classList.add('hide');
    }

    function closeDialog(isSubmitted) {

        recordingCreated = isSubmitted;
        dialogHelper.close(currentDialog);
    }

    function onSubmit(e) {

        loading.show();

        var form = this;

        var apiClient = connectionManager.getApiClient(currentServerId);

        apiClient.getNewLiveTvTimerDefaults({ programId: currentProgramId }).then(function (item) {

            item.RecordNewOnly = form.querySelector('#chkNewOnly').checked;
            item.RecordAnyChannel = form.querySelector('#chkAllChannels').checked;
            item.RecordAnyTime = form.querySelector('#chkAnyTime').checked;

            if (form.querySelector('#chkRecordSeries').checked) {

                apiClient.createLiveTvSeriesTimer(item).then(function () {

                    loading.hide();
                    closeDialog(true);
                });

            } else {
                apiClient.createLiveTvTimer(item).then(function () {

                    loading.hide();
                    closeDialog(true);
                });
            }
        });

        // Disable default form submission
        e.preventDefault();
        return false;
    }

    function getRegistration(apiClient, programId, feature) {

        loading.show();

        return apiClient.getJSON(apiClient.getUrl('LiveTv/Registration', {

            ProgramId: programId,
            Feature: feature

        })).then(function (result) {

            loading.hide();
            return result;

        }, function () {

            loading.hide();

            return {
                TrialVersion: true,
                IsValid: true,
                IsRegistered: false
            };
        });
    }

    function setPlayButtonVisible(context, visible) {

        var btnPlay = context.querySelector('.btnPlay');

        if (!visible) {
            btnPlay.classList.add('hide');
        } else {
            btnPlay.classList.remove('hide');
        }
    }

    function showSeriesRecordingFields(context, apiClient) {

        slideDownToShow(context.querySelector('.seriesFields'));

        getRegistration(apiClient, currentProgramId, 'seriesrecordings').then(function (regInfo) {

            if (regInfo.IsRegistered) {
                context.querySelector('.btnSubmit').classList.remove('hide');
                setPlayButtonVisible(context, true);
                context.querySelector('.supporterContainer').classList.add('hide');

            } else {

                context.querySelector('.supporterContainerText').innerHTML = globalize.translate('sharedcomponents#MessageActiveSubscriptionRequiredSeriesRecordings');
                context.querySelector('.supporterContainer').classList.remove('hide');
                context.querySelector('.btnSubmit').classList.add('hide');
                setPlayButtonVisible(context, false);
            }
        });
    }

    function showSingleRecordingFields(context, apiClient) {

        getRegistration(apiClient, currentProgramId, 'dvr').then(function (regInfo) {

            if (regInfo.IsRegistered) {
                context.querySelector('.btnSubmit').classList.remove('hide');
                setPlayButtonVisible(context, true);
                context.querySelector('.supporterContainer').classList.add('hide');

            } else {

                context.querySelector('.supporterContainerText').innerHTML = globalize.translate('sharedcomponents#DvrSubscriptionRequired');
                context.querySelector('.supporterContainer').classList.remove('hide');
                context.querySelector('.btnSubmit').classList.add('hide');
                setPlayButtonVisible(context, false);
            }
        });
    }

    function slideDownToShow(elem) {

        if (!elem.classList.contains('hide')) {
            return;
        }

        elem.classList.remove('hide');

        elem.style.overflowY = 'hidden';

        requestAnimationFrame(function () {

            elem.animate([{
                height: 0
            }, {
                height: elem.offsetHeight + 'px'

            }], { duration: 400, easing: 'ease' }).onfinish = function () {
                elem.classList.remove('hide');
            };
        });
    }

    function slideUpToHide(elem) {

        if (elem.classList.contains('hide')) {
            return;
        }

        elem.style.overflowY = 'hidden';

        requestAnimationFrame(function () {

            elem.animate([{
                height: elem.offsetHeight + 'px'
            }, {
                height: 0
            }], { duration: 400, easing: 'ease' }).onfinish = function () {
                elem.classList.add('hide');
            };
        });
    }

    function init(context) {

        var apiClient = connectionManager.getApiClient(currentServerId);

        context.querySelector('#chkRecordSeries').addEventListener('change', function () {

            if (this.checked) {
                showSeriesRecordingFields(context, apiClient);
            } else {
                hideSeriesRecordingFields(context);
                showSingleRecordingFields(context, apiClient);
            }
        });

        context.querySelector('.btnPlay').addEventListener('click', function () {

            closeAction = 'play';
            closeDialog(false);
        });

        context.querySelector('.btnCancel').addEventListener('click', function () {

            closeAction = null;
            closeDialog(false);
        });

        context.querySelector('form', context).addEventListener('submit', onSubmit);

        var supporterButtons = context.querySelectorAll('.btnSupporter');
        for (var i = 0, length = supporterButtons.length; i < length; i++) {
            if (appHost.supports('externalpremium')) {
                supporterButtons[i].classList.remove('hide');
            } else {
                supporterButtons[i].classList.add('hide');
            }
        }
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

    function renderRecording(context, defaultTimer, program, apiClient) {

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

        var btnPlay = context.querySelector('.btnPlay');
        var now = new Date();
        if (now >= datetime.parseISO8601Date(program.StartDate, true) && now < datetime.parseISO8601Date(program.EndDate, true)) {
            btnPlay.classList.remove('btnPlay-notplayable');
        } else {
            btnPlay.classList.add('btnPlay-notplayable');
        }

        context.querySelector('.itemMiscInfoPrimary').innerHTML = mediaInfo.getPrimaryMediaInfoHtml(program);
        context.querySelector('.itemMiscInfoSecondary').innerHTML = mediaInfo.getSecondaryMediaInfoHtml(program);

        context.querySelector('#chkNewOnly').checked = defaultTimer.RecordNewOnly;
        context.querySelector('#chkAllChannels').checked = defaultTimer.RecordAnyChannel;
        context.querySelector('#chkAnyTime').checked = defaultTimer.RecordAnyTime;

        if (program.IsSeries) {
            context.querySelector('#eligibleForSeriesFields').classList.remove('hide');
        } else {
            context.querySelector('#eligibleForSeriesFields').classList.add('hide');
        }

        showConvertRecordingsUnlockMessage(context, apiClient);

        loading.hide();
    }

    function showConvertRecordingsUnlockMessage(context, apiClient) {

        apiClient.getPluginSecurityInfo().then(function (regInfo) {

            if (regInfo.IsMBSupporter) {
                context.querySelector('.convertRecordingsContainer').classList.add('hide');
            } else {
                context.querySelector('.convertRecordingsContainer').classList.remove('hide');
            }

        }, function () {

            context.querySelector('.convertRecordingsContainer').classList.remove('hide');
        });
    }

    function onSupporterButtonClick() {
        if (appHost.supports('externalpremium')) {
            shell.openUrl('https://emby.media/premiere');
        } else {

        }
    }

    function reload(context, programId) {

        loading.show();

        var apiClient = connectionManager.getApiClient(currentServerId);

        var promise1 = apiClient.getNewLiveTvTimerDefaults({ programId: programId });
        var promise2 = apiClient.getLiveTvProgram(programId, apiClient.getCurrentUserId());

        Promise.all([promise1, promise2]).then(function (responses) {

            var defaults = responses[0];
            var program = responses[1];

            renderRecording(context, defaults, program, apiClient);
        });
    }

    function executeCloseAction(action, programId, serverId) {

        if (action == 'play') {

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

            recordingCreated = false;
            currentProgramId = itemId;
            currentServerId = serverId;
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

                dlg.addEventListener('close', function () {

                    executeCloseAction(closeAction, currentProgramId, currentServerId);

                    if (recordingCreated) {
                        require(['toast'], function (toast) {
                            toast(globalize.translate('sharedcomponents#RecordingScheduled'));
                        });
                        resolve();
                    } else {
                        reject();
                    }
                });

                if (layoutManager.tv) {
                    scrollHelper.centerFocus.on(dlg.querySelector('.formDialogContent'), false);
                }

                dlg.querySelector('.btnSupporterForConverting').addEventListener('click', onSupporterButtonClick);

                hideSeriesRecordingFields(dlg);
                showSingleRecordingFields(dlg, connectionManager.getApiClient(serverId));
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
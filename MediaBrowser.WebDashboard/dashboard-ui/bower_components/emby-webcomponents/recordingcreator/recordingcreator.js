define(['dialogHelper', 'globalize', 'layoutManager', 'mediaInfo', 'apphost', 'connectionManager', 'require', 'loading', 'scrollHelper', 'emby-checkbox', 'emby-button', 'emby-collapse', 'emby-input', 'paper-icon-button-light', 'css!./../formdialog', 'css!./recordingcreator', 'material-icons'], function (dialogHelper, globalize, layoutManager, mediaInfo, appHost, connectionManager, require, loading, scrollHelper) {

    var currentProgramId;
    var currentServerId;
    var currentDialog;
    var recordingCreated = false;

    function getDaysOfWeek() {

        return [
         'Sunday',
         'Monday',
         'Tuesday',
         'Wednesday',
         'Thursday',
         'Friday',
         'Saturday'
        ];
    }

    function getDays(context) {

        var daysOfWeek = getDaysOfWeek();

        var days = [];

        for (var i = 0, length = daysOfWeek.length; i < length; i++) {

            var day = daysOfWeek[i];

            if (context.querySelector('#chk' + day).checked) {
                days.push(day);
            }

        }

        return days;
    }

    function hideSeriesRecordingFields(context) {

        slideUpToHide(context.querySelector('.seriesFields'));
        slideUpToHide(context.querySelector('.seriesDays'));
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

        apiClient.getNamedConfiguration("livetv").then(function (config) {

            config.EnableRecordingEncoding = form.querySelector('#chkConvertRecordings').checked;

            apiClient.updateNamedConfiguration("livetv", config);
        });

        apiClient.getNewLiveTvTimerDefaults({ programId: currentProgramId }).then(function (item) {

            item.PrePaddingSeconds = form.querySelector('#txtPrePaddingMinutes').value * 60;
            item.PostPaddingSeconds = form.querySelector('#txtPostPaddingMinutes').value * 60;

            item.RecordNewOnly = form.querySelector('#chkNewOnly').checked;
            item.RecordAnyChannel = form.querySelector('#chkAllChannels').checked;
            item.RecordAnyTime = form.querySelector('#chkAnyTime').checked;

            item.Days = getDays(form);

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

    function getRegistration(programId, apiClient) {

        loading.show();

        return apiClient.getJSON(apiClient.getUrl('LiveTv/Registration', {

            ProgramId: programId,
            Feature: 'seriesrecordings'

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

    function showSeriesDays(context) {
        
        if (context.querySelector('#chkAnyTime').checked) {
            slideUpToHide(context.querySelector('.seriesDays'));
        } else {
            slideDownToShow(context.querySelector('.seriesDays'));
        }
    }

    function showSeriesRecordingFields(context, apiClient) {

        slideDownToShow(context.querySelector('.seriesFields'));
        showSeriesDays(context);
        context.querySelector('.btnSubmit').classList.remove('hide');

        getRegistration(currentProgramId, apiClient).then(function (regInfo) {

            if (regInfo.IsValid) {
                context.querySelector('.btnSubmit').classList.remove('hide');
            } else {
                context.querySelector('.btnSubmit').classList.add('hide');
            }

            if (regInfo.IsRegistered) {

                context.querySelector('.supporterContainer').classList.add('hide');

            } else {

                context.querySelector('.supporterContainer').classList.remove('hide');

                if (regInfo.TrialVersion) {
                    context.querySelector('.supporterTrial').classList.remove('hide');
                } else {
                    context.querySelector('.supporterTrial').classList.add('hide');
                }
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
            }
        });

        context.querySelector('.btnCancel').addEventListener('click', function () {

            closeDialog(false);
        });

        context.querySelector('#chkAnyTime').addEventListener('change', function () {

            showSeriesDays(context);
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

        apiClient.getNamedConfiguration("livetv").then(function (config) {

            context.querySelector('#chkConvertRecordings').checked = config.EnableRecordingEncoding;
        });

        if (layoutManager.tv) {
            context.querySelector('.advanced').classList.add('hide');
        } else {
            context.querySelector('.advanced').classList.remove('hide');
        }
    }

    function selectDays(page, days) {

        var daysOfWeek = getDaysOfWeek();

        for (var i = 0, length = daysOfWeek.length; i < length; i++) {

            var day = daysOfWeek[i];

            page.querySelector('#chk' + day).checked = days.indexOf(day) != -1;
        }
    }

    function renderRecording(context, defaultTimer, program, apiClient) {

        context.querySelector('.itemName').innerHTML = program.Name;
        context.querySelector('.itemEpisodeName').innerHTML = program.EpisodeTitle || '';

        context.querySelector('.itemMiscInfoPrimary').innerHTML = mediaInfo.getPrimaryMediaInfoHtml(program);
        context.querySelector('.itemMiscInfoSecondary').innerHTML = mediaInfo.getSecondaryMediaInfoHtml(program);

        context.querySelector('#chkNewOnly').checked = defaultTimer.RecordNewOnly;
        context.querySelector('#chkAllChannels').checked = defaultTimer.RecordAnyChannel;
        context.querySelector('#chkAnyTime').checked = defaultTimer.RecordAnyTime;

        context.querySelector('#txtPrePaddingMinutes').value = defaultTimer.PrePaddingSeconds / 60;
        context.querySelector('#txtPostPaddingMinutes').value = defaultTimer.PostPaddingSeconds / 60;

        if (program.IsSeries) {
            context.querySelector('#eligibleForSeriesFields').classList.remove('hide');
        } else {
            context.querySelector('#eligibleForSeriesFields').classList.add('hide');
        }

        selectDays(context, defaultTimer.Days);

        if (program.ServiceName == 'Emby') {
            context.querySelector('.convertRecordingsContainer').classList.remove('hide');
            showConvertRecordingsUnlockMessage(context, apiClient);
        } else {
            context.querySelector('.convertRecordingsContainer').classList.add('hide');
        }

        loading.hide();
    }

    function showConvertRecordingsUnlockMessage(context, apiClient) {

        apiClient.getPluginSecurityInfo().then(function (regInfo) {

            if (regInfo.IsMBSupporter) {
                context.querySelector('.btnSupporterForConverting').classList.add('hide');
            } else {
                context.querySelector('.btnSupporterForConverting').classList.remove('hide');
            }

        }, function () {

            context.querySelector('.btnSupporterForConverting').classList.remove('hide');
        });
    }

    function onSupporterButtonClick() {
        if (appHost.supports('externalpremium')) {
            require(['shell'], function (shell) {
                shell.openUrl('https://emby.media/premiere');
            });
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

    function showEditor(itemId, serverId) {

        return new Promise(function (resolve, reject) {

            recordingCreated = false;
            currentProgramId = itemId;
            currentServerId = serverId;
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
                document.body.appendChild(dlg);

                currentDialog = dlg;

                dlg.addEventListener('close', function () {

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
                    scrollHelper.centerFocus.on(dlg.querySelector('.dialogContent'), false);
                }

                dlg.querySelector('.btnSupporterForConverting').addEventListener('click', onSupporterButtonClick);

                hideSeriesRecordingFields(dlg);
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
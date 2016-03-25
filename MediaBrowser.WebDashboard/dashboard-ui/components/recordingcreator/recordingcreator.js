define(['dialogHelper', 'jQuery', 'scripts/livetvcomponents', 'livetvcss', 'paper-checkbox', 'paper-input', 'paper-toggle-button'], function (dialogHelper, $) {

    var currentProgramId;
    var currentDialog;
    var recordingCreated = false;

    function getDaysOfWeek() {

        // Do not localize. These are used as values, not text.
        return LiveTvHelpers.getDaysOfWeek().map(function (d) {
            return d.value;
        });
    }

    function getDays(context) {

        var daysOfWeek = getDaysOfWeek();

        var days = [];

        for (var i = 0, length = daysOfWeek.length; i < length; i++) {

            var day = daysOfWeek[i];

            if ($('#chk' + day, context).checked()) {
                days.push(day);
            }

        }

        return days;
    }

    function hideSeriesRecordingFields(context) {
        slideUpToHide(context.querySelector('#seriesFields'));
        context.querySelector('.btnSubmitContainer').classList.remove('hide');
        context.querySelector('.supporterContainer').classList.add('hide');
    }

    function closeDialog(isSubmitted) {

        recordingCreated = isSubmitted;
        dialogHelper.close(currentDialog);
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getNamedConfiguration("livetv").then(function (config) {

            config.EnableRecordingEncoding = $('#chkConvertRecordings', form).checked();

            ApiClient.updateNamedConfiguration("livetv", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        ApiClient.getNewLiveTvTimerDefaults({ programId: currentProgramId }).then(function (item) {

            item.PrePaddingSeconds = $('#txtPrePaddingMinutes', form).val() * 60;
            item.PostPaddingSeconds = $('#txtPostPaddingMinutes', form).val() * 60;

            item.RecordNewOnly = $('#chkNewOnly', form).checked();
            item.RecordAnyChannel = $('#chkAllChannels', form).checked();
            item.RecordAnyTime = $('#chkAnyTime', form).checked();

            item.Days = getDays(form);

            if ($('#chkRecordSeries', form).checked()) {

                ApiClient.createLiveTvSeriesTimer(item).then(function () {

                    Dashboard.hideLoadingMsg();
                    closeDialog(true);
                });

            } else {
                ApiClient.createLiveTvTimer(item).then(function () {

                    Dashboard.hideLoadingMsg();
                    closeDialog(true);
                });
            }
        });

        // Disable default form submission
        return false;
    }

    function getRegistration(programId) {

        Dashboard.showLoadingMsg();

        return ApiClient.getJSON(ApiClient.getUrl('LiveTv/Registration', {

            ProgramId: programId,
            Feature: 'seriesrecordings'

        })).then(function (result) {

            Dashboard.hideLoadingMsg();
            return result;

        }, function () {

            Dashboard.hideLoadingMsg();

            return {
                TrialVersion: true,
                IsValid: true,
                IsRegistered: false
            };
        });
    }

    function showSeriesRecordingFields(context) {
        slideDownToShow(context.querySelector('#seriesFields'));
        context.querySelector('.btnSubmitContainer').classList.remove('hide');

        getRegistration(currentProgramId).then(function (regInfo) {

            if (regInfo.IsValid) {
                context.querySelector('.btnSubmitContainer').classList.remove('hide');
            } else {
                context.querySelector('.btnSubmitContainer').classList.add('hide');
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

        elem.style.overflow = 'hidden';

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

        elem.style.overflow = 'hidden';

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

        $('#chkRecordSeries', context).on('change', function () {

            if (this.checked) {
                showSeriesRecordingFields(context);
            } else {
                hideSeriesRecordingFields(context);
            }
        });

        $('.btnCancel', context).on('click', function () {

            closeDialog(false);
        });

        context.querySelector('.chkAdvanced').addEventListener('change', function (e) {

            var elems = context.querySelectorAll('.advancedToggle');
            var isChecked = e.target.checked;

            for (var i = 0, length = elems.length; i < length; i++) {
                if (isChecked) {
                    slideDownToShow(elems[i]);
                } else {
                    slideUpToHide(elems[i]);
                }
            }
        });

        $('form', context).off('submit', onSubmit).on('submit', onSubmit);

        var supporterButtons = context.querySelectorAll('.btnSupporter');
        for (var i = 0, length = supporterButtons.length; i < length; i++) {
            if (AppInfo.enableSupporterMembership) {
                supporterButtons[i].classList.remove('hide');
            } else {
                supporterButtons[i].classList.add('hide');
            }
        }

        if (AppInfo.enableSupporterMembership) {
            context.querySelector('.btnSupporterForConverting a').href = 'https://emby.media/premiere';
        } else {
            context.querySelector('.btnSupporterForConverting a').href = '#';
        }

        ApiClient.getNamedConfiguration("livetv").then(function (config) {

            $('#chkConvertRecordings', context).checked(config.EnableRecordingEncoding);
        });
    }

    function selectDays(page, days) {

        var daysOfWeek = getDaysOfWeek();

        for (var i = 0, length = daysOfWeek.length; i < length; i++) {

            var day = daysOfWeek[i];

            $('#chk' + day, page).checked(days.indexOf(day) != -1);
        }
    }

    function renderRecording(context, defaultTimer, program) {

        $('.itemName', context).html(program.Name);

        $('.itemEpisodeName', context).html(program.EpisodeTitle || '');

        $('.itemMiscInfo', context).html(LibraryBrowser.getMiscInfoHtml(program));

        $('.itemMiscInfo a').each(function () {
            $(this).replaceWith(this.innerHTML);
        });

        $('#chkNewOnly', context).checked(defaultTimer.RecordNewOnly);
        $('#chkAllChannels', context).checked(defaultTimer.RecordAnyChannel);
        $('#chkAnyTime', context).checked(defaultTimer.RecordAnyTime);

        $('#txtPrePaddingMinutes', context).val(defaultTimer.PrePaddingSeconds / 60);
        $('#txtPostPaddingMinutes', context).val(defaultTimer.PostPaddingSeconds / 60);

        if (program.IsSeries) {
            $('#eligibleForSeriesFields', context).show();
        } else {
            $('#eligibleForSeriesFields', context).hide();
        }

        selectDays(context, defaultTimer.Days);

        if (program.ServiceName == 'Emby') {
            context.querySelector('.convertRecordingsContainer').classList.remove('hide');
            showConvertRecordingsUnlockMessage(context);
        } else {
            context.querySelector('.convertRecordingsContainer').classList.add('hide');
        }

        Dashboard.hideLoadingMsg();
    }

    function showConvertRecordingsUnlockMessage(context) {

        Dashboard.getPluginSecurityInfo().then(function(regInfo) {

            if (regInfo.IsMBSupporter) {
                context.querySelector('.btnSupporterForConverting').classList.add('hide');
            } else {
                context.querySelector('.btnSupporterForConverting').classList.remove('hide');
            }

        }, function() {
            
            context.querySelector('.btnSupporterForConverting').classList.remove('hide');
        });
    }

    function reload(context, programId) {

        Dashboard.showLoadingMsg();

        var promise1 = ApiClient.getNewLiveTvTimerDefaults({ programId: programId });
        var promise2 = ApiClient.getLiveTvProgram(programId, Dashboard.getCurrentUserId());

        Promise.all([promise1, promise2]).then(function (responses) {

            var defaults = responses[0];
            var program = responses[1];

            renderRecording(context, defaults, program);
        });
    }

    function showEditor(itemId) {

        return new Promise(function (resolve, reject) {

            recordingCreated = false;
            currentProgramId = itemId;
            Dashboard.showLoadingMsg();

            var xhr = new XMLHttpRequest();
            xhr.open('GET', 'components/recordingcreator/recordingcreator.template.html', true);

            xhr.onload = function (e) {

                var template = this.response;
                var dlg = dialogHelper.createDialog({
                    removeOnClose: true,
                    size: 'small'
                });

                dlg.classList.add('ui-body-b');
                dlg.classList.add('background-theme-b');

                dlg.classList.add('formDialog');

                var html = '';

                html += Globalize.translateDocument(template);

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                dialogHelper.open(dlg);

                currentDialog = dlg;

                dlg.addEventListener('close', function () {

                    if (recordingCreated) {
                        require(['toast'], function (toast) {
                            toast(Globalize.translate('MessageRecordingScheduled'));
                        });
                        resolve();
                    } else {
                        reject();
                    }
                });

                hideSeriesRecordingFields(dlg);
                init(dlg);

                reload(dlg, itemId);
            }

            xhr.send();
        });
    }

    return {
        show: showEditor
    };
});
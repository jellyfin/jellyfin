(function ($, document) {

    var currentProgram;
    var registrationInfo;
    var lastRegId;

    function getRegistration(programId) {

        var deferred = DeferredBuilder.Deferred();

        if (registrationInfo && (lastRegId == programId)) {
            deferred.resolveWith(null, [registrationInfo]);
            return deferred.promise();
        }

        registrationInfo = null;
        Dashboard.showLoadingMsg();

        ApiClient.getJSON(ApiClient.getUrl('LiveTv/Registration', {

            ProgramId: programId,
            Feature: 'seriesrecordings'
        })).then(function (result) {

            lastRegId = programId;
            registrationInfo = result;
            deferred.resolveWith(null, [registrationInfo]);
            Dashboard.hideLoadingMsg();

        }, function () {

            deferred.resolveWith(null, [
            {
                TrialVersion: true,
                IsValid: true,
                IsRegistered: false
            }]);

            Dashboard.hideLoadingMsg();
        });

        return deferred.promise();
    }

    function renderRecording(page, defaultTimer, program) {

        currentProgram = program;

        $('.itemName', page).html(program.Name);

        $('.itemEpisodeName', page).html(program.EpisodeTitle || '');

        $('.itemCommunityRating', page).html(LibraryBrowser.getRatingHtml(program));

        LibraryBrowser.renderGenres($('.itemGenres', page), program);
        LibraryBrowser.renderOverview(page.querySelectorAll('.itemOverview'), program);

        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(program));

        $('#chkNewOnly', page).checked(defaultTimer.RecordNewOnly);
        $('#chkAllChannels', page).checked(defaultTimer.RecordAnyChannel);
        $('#chkAnyTime', page).checked(defaultTimer.RecordAnyTime);

        $('#txtPrePaddingMinutes', page).val(defaultTimer.PrePaddingSeconds / 60);
        $('#txtPostPaddingMinutes', page).val(defaultTimer.PostPaddingSeconds / 60);

        if (program.IsSeries) {
            $('#eligibleForSeriesFields', page).show();
        } else {
            $('#eligibleForSeriesFields', page).hide();
        }

        selectDays(page, defaultTimer.Days);

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var programId = getParameterByName('programid');

        var promise1 = ApiClient.getNewLiveTvTimerDefaults({ programId: programId });
        var promise2 = ApiClient.getLiveTvProgram(programId, Dashboard.getCurrentUserId());

        Promise.all([promise1, promise2]).then(function (responses) {

            var defaults = responses[0];
            var program = responses[1];

            renderRecording(page, defaults, program);
        });
    }

    function selectDays(page, days) {

        var daysOfWeek = getDaysOfWeek();

        for (var i = 0, length = daysOfWeek.length; i < length; i++) {

            var day = daysOfWeek[i];

            $('#chk' + day, page).checked(days.indexOf(day) != -1);

        }

    }

    function getDaysOfWeek() {

        // Do not localize. These are used as values, not text.
        return LiveTvHelpers.getDaysOfWeek().map(function (d) {
            return d.value;
        });
    }

    function getDays(page) {

        var daysOfWeek = getDaysOfWeek();

        var days = [];

        for (var i = 0, length = daysOfWeek.length; i < length; i++) {

            var day = daysOfWeek[i];

            if ($('#chk' + day, page).checked()) {
                days.push(day);
            }

        }

        return days;
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        var programId = getParameterByName('programid');

        ApiClient.getNewLiveTvTimerDefaults({ programId: programId }).then(function (item) {

            item.PrePaddingSeconds = $('#txtPrePaddingMinutes', form).val() * 60;
            item.PostPaddingSeconds = $('#txtPostPaddingMinutes', form).val() * 60;

            item.RecordNewOnly = $('#chkNewOnly', form).checked();
            item.RecordAnyChannel = $('#chkAllChannels', form).checked();
            item.RecordAnyTime = $('#chkAnyTime', form).checked();

            item.Days = getDays(form);

            if ($('#chkRecordSeries', form).checked()) {

                ApiClient.createLiveTvSeriesTimer(item).then(function () {

                    Dashboard.hideLoadingMsg();
                    Dashboard.navigate('livetv.html');

                });

            } else {
                ApiClient.createLiveTvTimer(item).then(function () {

                    Dashboard.hideLoadingMsg();
                    Dashboard.navigate('livetv.html');

                });
            }

        });

        // Disable default form submission
        return false;
    }

    function hideSeriesRecordingFields(page) {
        $('#seriesFields', page).hide();
        page.querySelector('.btnSubmitContainer').classList.remove('hide');
        page.querySelector('.supporterContainer').classList.add('hide');
    }

    function showSeriesRecordingFields(page) {
        $('#seriesFields', page).show();
        page.querySelector('.btnSubmitContainer').classList.remove('hide');

        getRegistration(getParameterByName('programid')).then(function (regInfo) {

            if (regInfo.IsValid) {
                page.querySelector('.btnSubmitContainer').classList.remove('hide');
            } else {
                page.querySelector('.btnSubmitContainer').classList.add('hide');
            }

            if (regInfo.IsRegistered) {

                page.querySelector('.supporterContainer').classList.add('hide');

            } else {

                page.querySelector('.supporterContainer').classList.remove('hide');

                if (AppInfo.enableSupporterMembership) {
                    page.querySelector('.btnSupporter').classList.remove('hide');
                } else {
                    page.querySelector('.btnSupporter').classList.add('hide');
                }

                if (regInfo.TrialVersion) {
                    page.querySelector('.supporterTrial').classList.remove('hide');
                } else {
                    page.querySelector('.supporterTrial').classList.add('hide');
                }
            }
        });
    }

    $(document).on('pageinit', "#liveTvNewRecordingPage", function () {

        var page = this;

        $('#chkRecordSeries', page).on('change', function () {

            if (this.checked) {
                showSeriesRecordingFields(page);
            } else {
                hideSeriesRecordingFields(page);
            }
        });

        $('#btnCancel', page).on('click', function () {

            var programId = getParameterByName('programid');

            Dashboard.navigate('itemdetails.html?id=' + programId);

        });

        $('.liveTvNewRecordingForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pagebeforeshow', "#liveTvNewRecordingPage", function () {

        var page = this;
        hideSeriesRecordingFields(page);
        reload(page);

    }).on('pagebeforehide', "#liveTvNewRecordingPage", function () {

        currentProgram = null;

    });

})(jQuery, document);
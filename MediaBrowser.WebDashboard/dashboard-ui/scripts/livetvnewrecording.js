(function ($, document) {

    var currentProgram;

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
        $('#chkPrePaddingRequired', page).checked(defaultTimer.IsPrePaddingRequired);
        $('#chkPostPaddingRequired', page).checked(defaultTimer.IsPostPaddingRequired);

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

        $.when(promise1, promise2).done(function (response1, response2) {

            var defaults = response1[0];
            var program = response2[0];

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

        ApiClient.getNewLiveTvTimerDefaults({ programId: programId }).done(function (item) {

            item.PrePaddingSeconds = $('#txtPrePaddingMinutes', form).val() * 60;
            item.PostPaddingSeconds = $('#txtPostPaddingMinutes', form).val() * 60;
            item.IsPrePaddingRequired = $('#chkPrePaddingRequired', form).checked();
            item.IsPostPaddingRequired = $('#chkPostPaddingRequired', form).checked();

            item.RecordNewOnly = $('#chkNewOnly', form).checked();
            item.RecordAnyChannel = $('#chkAllChannels', form).checked();
            item.RecordAnyTime = $('#chkAnyTime', form).checked();

            item.Days = getDays(form);

            if ($('#chkRecordSeries', form).checked()) {

                ApiClient.createLiveTvSeriesTimer(item).done(function () {

                    Dashboard.hideLoadingMsg();
                    Dashboard.navigate('livetv.html');

                });

            } else {
                ApiClient.createLiveTvTimer(item).done(function () {

                    Dashboard.hideLoadingMsg();
                    Dashboard.navigate('livetv.html');

                });
            }

        });

        // Disable default form submission
        return false;
    }

    $(document).on('pageinitdepends', "#liveTvNewRecordingPage", function () {

        var page = this;

        $('#chkRecordSeries', page).on('change', function () {

            if (this.checked) {
                $('#seriesFields', page).show();
            } else {
                $('#seriesFields', page).hide();
            }

        });

        $('#btnCancel', page).on('click', function () {

            var programId = getParameterByName('programid');

            Dashboard.navigate('itemdetails.html?id=' + programId);

        });

        $('.liveTvNewRecordingForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pagebeforeshowready', "#liveTvNewRecordingPage", function () {

        var page = this;

        reload(page);

    }).on('pagebeforehide', "#liveTvNewRecordingPage", function () {

        currentProgram = null;

    });

})(jQuery, document);
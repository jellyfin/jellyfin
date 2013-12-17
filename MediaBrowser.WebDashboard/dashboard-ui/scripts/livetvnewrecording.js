(function ($, document, apiClient) {

    var currentProgram;
    var daysOfWeek = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

    function renderRecording(page, defaultTimer, program) {

        currentProgram = program;

        var context = 'livetv';

        $('.itemName', page).html(program.Name);
        $('.itemChannelNumber', page).html('Channel:&nbsp;&nbsp;&nbsp;<a href="livetvchannel.html?id=' + program.ChannelId + '">' + program.ChannelName + '</a>').trigger('create');

        if (program.EpisodeTitle) {
            $('.itemEpisodeName', page).html('Episode:&nbsp;&nbsp;&nbsp;' + program.EpisodeTitle);
        } else {
            $('.itemEpisodeName', page).html('');
        }

        if (program.CommunityRating) {
            $('.itemCommunityRating', page).html(LibraryBrowser.getRatingHtml(program)).show();
        } else {
            $('.itemCommunityRating', page).hide();
        }

        LibraryBrowser.renderGenres($('.itemGenres', page), program, context);
        LibraryBrowser.renderOverview($('.itemOverview', page), program);

        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(program));

        $('#txtRequestedPrePaddingSeconds', page).val(defaultTimer.RequestedPrePaddingSeconds);
        $('#txtRequestedPostPaddingSeconds', page).val(defaultTimer.RequestedPostPaddingSeconds);
        $('#txtRequiredPrePaddingSeconds', page).val(defaultTimer.RequiredPrePaddingSeconds);
        $('#txtRequiredPostPaddingSeconds', page).val(defaultTimer.RequiredPostPaddingSeconds);

        try {

            var startDate = parseISO8601Date(program.StartDate, { toLocal: true });

            $('#chk' + daysOfWeek[startDate.getDay()], page).checked(true).checkboxradio('refresh');

        }
        catch (e) {
            console.log("Error parsing date: " + program.StartDate);
        }

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var programid = getParameterByName('programid');

        var promise1 = apiClient.getNewLiveTvTimerDefaults();
        var promise2 = apiClient.getLiveTvProgram(programid, Dashboard.getCurrentUserId());

        $.when(promise1, promise2).done(function (response1, response2) {

            var defaults = response1[0];
            var program = response2[0];

            renderRecording(page, defaults, program);
        });
    }

    function getDays(page) {

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

        apiClient.getNewLiveTvTimerDefaults().done(function (item) {

            item.RequestedPrePaddingSeconds = $('#txtRequestedPrePaddingSeconds', form).val();
            item.RequestedPostPaddingSeconds = $('#txtRequestedPostPaddingSeconds', form).val();
            item.RequiredPrePaddingSeconds = $('#txtRequiredPrePaddingSeconds', form).val();
            item.RequiredPostPaddingSeconds = $('#txtRequiredPostPaddingSeconds', form).val();

            item.RecordNewOnly = $('#chkNewOnly', form).checked();
            item.RecordAnyChannel = $('#chkAllChannels', form).checked();
            item.RecordAnyTime = $('#chkAnyTime', form).checked();

            item.Days = getDays(form);

            item.Name = currentProgram.Name;
            item.ProgramId = currentProgram.Id;
            item.ChannelName = currentProgram.ChannelName;
            item.ChannelId = currentProgram.ChannelId;
            item.Overview = currentProgram.Overview;
            item.StartDate = currentProgram.StartDate;
            item.EndDate = currentProgram.EndDate;

            if ($('#chkRecordSeries', form).checked()) {

                apiClient.createLiveTvSeriesTimer(item).done(function () {

                    Dashboard.hideLoadingMsg();
                    Dashboard.navigate('livetvseriestimers.html');

                });

            } else {
                apiClient.createLiveTvTimer(item).done(function () {

                    Dashboard.hideLoadingMsg();
                    Dashboard.navigate('livetvtimers.html');

                });
            }

        });

        // Disable default form submission
        return false;
    }

    function liveTvNewRecordingPage() {

        var self = this;

        self.onSubmit = onSubmit;

    }

    window.LiveTvNewRecordingPage = new liveTvNewRecordingPage();

    $(document).on('pageinit', "#liveTvNewRecordingPage", function () {

        var page = this;

        $('#chkRecordSeries', page).on('change', function () {

            if (this.checked) {
                $('#seriesFields', page).show();
            } else {
                $('#seriesFields', page).hide();
            }

        });

    }).on('pagebeforeshow', "#liveTvNewRecordingPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvNewRecordingPage", function () {

        currentProgram = null;

    });

})(jQuery, document, ApiClient);
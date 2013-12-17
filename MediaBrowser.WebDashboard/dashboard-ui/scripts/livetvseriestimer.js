(function (window, $, document, apiClient) {

    var currentItem;

    function deleteTimer(page, id) {

        Dashboard.confirm("Are you sure you wish to cancel this series?", "Confirm Series Timer Cancellation", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvSeriesTimer(id).done(function () {

                    Dashboard.alert('Timer cancelled.');

                    reload(page);
                });
            }

        });
    }

    function renderTimer(page, item) {

        currentItem = item;

        $('.itemName', page).html(item.Name);
        $('.overview', page).html(item.Overview || '');

        $('#txtRequestedPrePaddingSeconds', page).val(item.RequestedPrePaddingSeconds);
        $('#txtRequestedPostPaddingSeconds', page).val(item.RequestedPostPaddingSeconds);
        $('#txtRequiredPrePaddingSeconds', page).val(item.RequiredPrePaddingSeconds);
        $('#txtRequiredPostPaddingSeconds', page).val(item.RequiredPostPaddingSeconds);

        $('#chkNewOnly', page).checked(item.RecordNewOnly).checkboxradio('refresh');
        $('#chkAllChannels', page).checked(item.RecordAnyChannel).checkboxradio('refresh');
        $('#chkAnyTime', page).checked(item.RecordAnyTime).checkboxradio('refresh');

        var channelHtml = '';

        if (item.RecurrenceType == 'NewProgramEventsAllChannels' || item.RecurrenceType == 'AllProgramEventsAllChannels') {
            channelHtml += 'All Channels';
        }
        else if (item.ChannelId) {
            channelHtml += '<a href="livetvchannel.html?id=' + item.ChannelId + '">' + item.ChannelName + '</a>';
        }

        $('.channel', page).html('Channel:&nbsp;&nbsp;&nbsp;' + channelHtml).trigger('create');

        selectDays(page, item.Days);


        $('.time', page).html('Time:&nbsp;&nbsp;&nbsp;' + LiveTvHelpers.getDisplayTime(item.StartDate));

        Dashboard.hideLoadingMsg();
    }

    function selectDays(page, days) {

        var daysOfWeek = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

        for (var i = 0, length = daysOfWeek.length; i < length; i++) {

            var day = daysOfWeek[i];

            $('#chk' + day, page).checked(days.indexOf(day) != -1).checkboxradio('refresh');

        }

    }

    function getDays(page) {

        var daysOfWeek = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

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

        apiClient.getLiveTvSeriesTimer(currentItem.Id).done(function (item) {

            item.RequestedPrePaddingSeconds = $('#txtRequestedPrePaddingSeconds', form).val();
            item.RequestedPostPaddingSeconds = $('#txtRequestedPostPaddingSeconds', form).val();
            item.RequiredPrePaddingSeconds = $('#txtRequiredPrePaddingSeconds', form).val();
            item.RequiredPostPaddingSeconds = $('#txtRequiredPostPaddingSeconds', form).val();

            item.RecordNewOnly = $('#chkNewOnly', form).checked();
            item.RecordAnyChannel = $('#chkAllChannels', form).checked();
            item.RecordAnyTime = $('#chkAnyTime', form).checked();

            item.Days = getDays(form);

            ApiClient.updateLiveTvSeriesTimer(item).done(function () {
                Dashboard.alert('Timer Saved');
            });
        });

        // Disable default form submission
        return false;

    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        apiClient.getLiveTvSeriesTimer(id).done(function (result) {

            renderTimer(page, result);

        });
    }

    $(document).on('pageinit', "#liveTvSeriesTimerPage", function () {

        var page = this;

        $('#btnCancelTimer', page).on('click', function () {

            deleteTimer(page, currentItem.Id);

        });

    }).on('pagebeforeshow', "#liveTvSeriesTimerPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvSeriesTimerPage", function () {

        currentItem = null;
    });

    function liveTvSeriesTimerPage() {

        var self = this;

        self.onSubmit = onSubmit;
    }

    window.LiveTvSeriesTimerPage = new liveTvSeriesTimerPage();

})(window, jQuery, document, ApiClient);
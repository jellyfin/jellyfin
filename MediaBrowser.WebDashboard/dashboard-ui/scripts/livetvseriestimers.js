(function ($, document, apiClient) {

    function deleteTimer(page, id) {

        Dashboard.confirm("Are you sure you wish to cancel this series?", "Confirm Series Cancellation", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvSeriesTimer(id).done(function () {

                    Dashboard.alert('Series cancelled.');

                    reload(page);
                });
            }

        });
    }

    function renderTimers(page, timers) {

        var html = '';

        html += '<ul data-role="listview" data-inset="true">';

        html += '<li data-role="list-divider">Series Recordings</li>';

        for (var i = 0, length = timers.length; i < length; i++) {

            var timer = timers[i];

            html += '<li><a href="livetvseriestimer.html?id=' + timer.Id + '">';

            html += '<h3>';
            html += timer.Name;
            html += '</h3>';

            html += '<p>';
            if (timer.DayPattern) {
                html += timer.DayPattern;
            }
            else {
                var days = timer.Days || [];

                html += days.join(', ');
            }
            
            if (timer.RecordAnyTime) {

                html += ' - Any time.';
            } else {
                html += ' - ' + LiveTvHelpers.getDisplayTime(timer.StartDate);
            }
            html += '</p>';

            html += '<p>';
            if (timer.RecordAnyChannel) {
                html += 'All Channels';
            }
            else if (timer.ChannelId) {
                html += timer.ChannelName;
            }
            html += '</p>';

            html += '</li>';
        }

        html += '</a></ul>';

        $('#items', page).html(html).trigger('create');

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        apiClient.getLiveTvSeriesTimers().done(function (result) {

            renderTimers(page, result.Items);

        });
    }

    $(document).on('pagebeforeshow', "#liveTvSeriesTimersPage", function () {

        var page = this;

        reload(page);
    });

})(jQuery, document, ApiClient);
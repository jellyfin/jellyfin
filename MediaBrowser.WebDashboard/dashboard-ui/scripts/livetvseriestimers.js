(function ($, document, apiClient) {

    function deleteTimer(page, id) {

        Dashboard.confirm("Are you sure you wish to cancel this series?", "Confirm Series Timer Cancellation", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvSeriesTimer(id).done(function () {

                    Dashboard.alert('Series Timer deleted');

                    reload(page);
                });
            }

        });
    }

    function renderTimers(page, timers) {

        var html = '';

        var cssClass = "detailTable";

        html += '<div class="detailTableContainer"><table class="' + cssClass + '">';

        html += '<tr>';

        html += '<th class="tabletColumn">&nbsp;</th>';
        html += '<th>Name</th>';
        html += '<th class="desktopColumn">Channel</th>';
        html += '<th>Days</th>';
        html += '<th>Time</th>';

        html += '</tr>';

        for (var i = 0, length = timers.length; i < length; i++) {

            var timer = timers[i];

            html += '<tr>';

            html += '<td class="tabletColumn">';
            html += '<button data-timerid="' + timer.Id + '" class="btnDeleteTimer" type="button" data-icon="delete" data-inline="true" data-mini="true" data-iconpos="notext">Delete</button>';
            html += '</td>';

            html += '<td>';
            html += '<a href="livetvseriestimer.html?id=' + timer.Id + '">' + timer.Name + '</a>';
            html += '</td>';

            html += '<td class="desktopColumn">';
            
            if (timer.RecordAnyChannel) {
                html += 'All Channels';
            }
            else if (timer.ChannelId) {
                html += '<a href="livetvchannel.html?id=' + timer.ChannelId + '">' + timer.ChannelName + '</a>';
            }
            html += '</td>';

            html += '<td>';

            if (timer.DayPattern) {
                html += timer.DayPattern;
            }
            else {
                var days = timer.Days || [];

                html += days.join(', ');
            }

            html += '</td>';

            html += '<td>' + LiveTvHelpers.getDisplayTime(timer.StartDate) + '</td>';

            html += '</tr>';
        }

        html += '</table></div>';

        var elem = $('#items', page).html(html).trigger('create');

        $('.btnDeleteTimer', elem).on('click', function () {

            var id = this.getAttribute('data-timerid');

            deleteTimer(page, id);
        });

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
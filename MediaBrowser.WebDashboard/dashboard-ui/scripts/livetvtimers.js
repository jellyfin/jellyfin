(function ($, document, apiClient) {

    function renderTimers(page, timers) {

        var html = '';

        var cssClass = "detailTable";

        html += '<div class="detailTableContainer"><table class="' + cssClass + '">';

        html += '<tr>';

        html += '<th>&nbsp;</th>';
        html += '<th>Name</th>';
        html += '<th>Channel</th>';
        html += '<th>Date</th>';
        html += '<th>Start</th>';
        html += '<th>End</th>';
        html += '<th>Status</th>';
        html += '<th>Recurring Schedule</th>';

        html += '</tr>';

        for (var i = 0, length = timers.length; i < length; i++) {

            var timer = timers[i];

            html += '<tr>';

            html += '<td>';
            html += '<button data-timerid="' + timer.Id + '" class="btnEditTimer" type="button" data-icon="pencil" data-inline="true" data-mini="true" data-iconpos="notext">Edit</button>';
            html += '<button data-timerid="' + timer.Id + '" class="btnDeleteTimer" type="button" data-icon="delete" data-inline="true" data-mini="true" data-iconpos="notext">Delete</button>';
            html += '</td>';

            html += '<td>' + (timer.Name || '') + '</td>';
            html += '<td>' + (timer.ChannelName || '') + '</td>';

            var startDate = timer.StartDate;

            try {

                startDate = parseISO8601Date(startDate, { toLocal: true });

            } catch (err) {

            }

            html += '<td>' + startDate.toLocaleDateString() + '</td>';

            html += '<td>' + LiveTvHelpers.getDisplayTime(timer.StartDate) + '</td>';

            html += '<td>' + LiveTvHelpers.getDisplayTime(timer.EndDate) + '</td>';

            html += '<td>' + (timer.Status || '') + '</td>';

            html += '<td></td>';

            html += '</tr>';
        }

        html += '</table></div>';

        var elem = $('#items', page).html(html).trigger('create');
    }

    function reload(page) {

        apiClient.getLiveTvTimers().done(function (result) {

            renderTimers(page, result.Items);

        });
    }

    $(document).on('pagebeforeshow', "#liveTvTimersPage", function () {

        var page = this;

        reload(page);
    });

})(jQuery, document, ApiClient);
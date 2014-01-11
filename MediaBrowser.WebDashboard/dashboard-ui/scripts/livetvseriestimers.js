(function ($, document, apiClient) {

    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending"
    };

    function deleteSeriesTimer(page, id) {

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

        html += '<ul data-role="listview" data-inset="true" data-split-icon="delete">';

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
            html += '</a>';

            html += '<a data-seriestimerid="' + timer.Id + '" href="#" title="Cancel Series" class="btnCancelSeries">Cancel Series</a>';

            html += '</li>';
        }

        html += '</ul>';

        var elem = $('#items', page).html(html).trigger('create');

        $('.btnCancelSeries', elem).on('click', function() {

            deleteSeriesTimer(page, this.getAttribute('data-seriestimerid'));

        });

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        apiClient.getLiveTvSeriesTimers(query).done(function (result) {

            renderTimers(page, result.Items);

        });
    }

    function updateFilterControls(page) {

        // Reset form values using the last used query
        $('.radioSortBy', page).each(function () {

            this.checked = (query.SortBy || '').toLowerCase() == this.getAttribute('data-sortby').toLowerCase();

        }).checkboxradio('refresh');

        $('.radioSortOrder', page).each(function () {

            this.checked = (query.SortOrder || '').toLowerCase() == this.getAttribute('data-sortorder').toLowerCase();

        }).checkboxradio('refresh');
    }

    $(document).on('pagebeforeshow', "#liveTvSeriesTimersPage", function () {

        var page = this;

        reload(page);
        
    }).on('pageinit', "#liveTvSeriesTimersPage", function () {

        var page = this;

        $('.radioSortBy', this).on('click', function () {
            query.StartIndex = 0;
            query.SortBy = this.getAttribute('data-sortby');
            reload(page);
        });

        $('.radioSortOrder', this).on('click', function () {
            query.StartIndex = 0;
            query.SortOrder = this.getAttribute('data-sortorder');
            reload(page);
        });

    }).on('pageshow', "#liveTvSeriesTimersPage", function () {

        updateFilterControls(this);
    });

})(jQuery, document, ApiClient);
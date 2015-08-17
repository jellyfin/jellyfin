(function ($, document) {

    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending"
    };

    function deleteSeriesTimer(page, id) {

        Dashboard.confirm(Globalize.translate('MessageConfirmSeriesCancellation'), Globalize.translate('HeaderConfirmSeriesCancellation'), function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvSeriesTimer(id).done(function () {

                    Dashboard.alert(Globalize.translate('MessageSeriesCancelled'));

                    reload(page);
                });
            }

        });
    }

    function renderTimers(page, timers) {

        var html = '';

        if (timers.length) {
            html += '<div class="paperList">';
        }

        for (var i = 0, length = timers.length; i < length; i++) {

            var timer = timers[i];

            html += '<paper-icon-item>';

            html += '<paper-fab class="listAvatar" icon="live-tv" item-icon></paper-fab>';

            html += '<paper-item-body three-line>';
            html += '<a class="clearLink" href="livetvseriestimer.html?id=' + timer.Id + '">';

            html += '<div>';
            html += timer.Name;
            html += '</div>';

            html += '<div secondary>';
            if (timer.DayPattern) {
                html += timer.DayPattern;
            }
            else {
                var days = timer.Days || [];

                html += days.join(', ');
            }

            if (timer.RecordAnyTime) {

                html += ' - ' + Globalize.translate('LabelAnytime');
            } else {
                html += ' - ' + LibraryBrowser.getDisplayTime(timer.StartDate);
            }
            html += '</div>';

            html += '<div secondary>';
            if (timer.RecordAnyChannel) {
                html += Globalize.translate('LabelAllChannels');
            }
            else if (timer.ChannelId) {
                html += timer.ChannelName;
            }
            html += '</div>';
            html += '</a>';

            html += '</paper-item-body>';

            html += '<paper-icon-button icon="delete" data-seriestimerid="' + timer.Id + '" title="' + Globalize.translate('ButtonCancelSeries') + '" class="btnCancelSeries"></paper-icon-button>';

            html += '</paper-icon-item>';
        }

        if (timers.length) {
            html += '</div>';
        }

        var elem = $('#items', page).html(html).trigger('create');

        $('.btnCancelSeries', elem).on('click', function () {

            deleteSeriesTimer(page, this.getAttribute('data-seriestimerid'));

        });

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvSeriesTimers(query).done(function (result) {

            renderTimers(page, result.Items);

            LibraryBrowser.setLastRefreshed(page);
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

    $(document).on('pageinitdepends', "#liveTvSuggestedPage", function () {

        var page = this;

        $(page.querySelector('neon-animated-pages')).on('tabchange', function () {

            if (parseInt(this.selected) == 5) {
                var tabContent = page.querySelector('.seriesTimersTabContent');

                if (LibraryBrowser.needsRefresh(tabContent)) {
                    reload(tabContent);
                }
            }
        });

    }).on('pageinitdepends', "#liveTvSuggestedPage", function () {

        var page = this.querySelector('.seriesTimersTabContent');

        $('.radioSortBy', page).on('click', function () {
            query.StartIndex = 0;
            query.SortBy = this.getAttribute('data-sortby');
            reload(page);
        });

        $('.radioSortOrder', page).on('click', function () {
            query.StartIndex = 0;
            query.SortOrder = this.getAttribute('data-sortorder');
            reload(page);
        });

        updateFilterControls(this);
    });

})(jQuery, document);
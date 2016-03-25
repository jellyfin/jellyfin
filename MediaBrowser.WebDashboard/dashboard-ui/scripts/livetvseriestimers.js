define(['jQuery'], function ($) {

    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending"
    };

    function deleteSeriesTimer(page, id) {

        require(['confirm'], function (confirm) {

            confirm(Globalize.translate('MessageConfirmSeriesCancellation'), Globalize.translate('HeaderConfirmSeriesCancellation')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvSeriesTimer(id).then(function () {

                    require(['toast'], function (toast) {
                        toast(Globalize.translate('MessageSeriesCancelled'));
                    });

                    reload(page);
                });
            });
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

            html += '<paper-fab mini icon="live-tv" item-icon></paper-fab>';

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

            html += '<paper-icon-button icon="cancel" data-seriestimerid="' + timer.Id + '" title="' + Globalize.translate('ButtonCancelSeries') + '" class="btnCancelSeries"></paper-icon-button>';

            html += '</paper-icon-item>';
        }

        if (timers.length) {
            html += '</div>';
        }

        var elem = $('#items', page).html(html);

        $('.btnCancelSeries', elem).on('click', function () {

            deleteSeriesTimer(page, this.getAttribute('data-seriestimerid'));

        });

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvSeriesTimers(query).then(function (result) {

            require(['paper-fab', 'paper-item-body', 'paper-icon-item'], function () {
                renderTimers(page, result.Items);
            });

            LibraryBrowser.setLastRefreshed(page);
        });
    }

    window.LiveTvPage.renderSeriesTimersTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            reload(tabContent);
        }
    };

});
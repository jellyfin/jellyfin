(function ($, document) {

    function deleteTimer(page, id) {

        Dashboard.confirm(Globalize.translate('MessageConfirmRecordingCancellation'), Globalize.translate('HeaderConfirmRecordingCancellation'), function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).done(function () {

                    Dashboard.alert(Globalize.translate('MessageRecordingCancelled'));

                    reload(page);
                });
            }

        });
    }

    function renderTimers(page, timers) {

        var html = '';

        var index = '';

        for (var i = 0, length = timers.length; i < length; i++) {

            var timer = timers[i];

            var startDateText = LibraryBrowser.getFutureDateText(parseISO8601Date(timer.StartDate, { toLocal: true }));

            if (startDateText != index) {

                if (index) {
                    html += '</div>';
                    html += '</div>';
                }

                html += '<div class="homePageSection">';
                html += '<h1>' + startDateText + '</h1>';
                html += '<div class="paperList">';
                index = startDateText;
            }

            html += '<paper-icon-item>';

            var program = timer.ProgramInfo || {};
            var imgUrl;

            if (program.ImageTags && program.ImageTags.Primary) {

                imgUrl = ApiClient.getScaledImageUrl(program.Id, {
                    height: 80,
                    tag: program.ImageTags.Primary,
                    type: "Primary"
                });
            }

            if (imgUrl) {
                html += '<paper-fab class="listAvatar blue" style="background-image:url(\'' + imgUrl + '\');background-repeat:no-repeat;background-position:center center;background-size: cover;" item-icon></paper-fab>';
            }
            else if (program.IsKids) {
                html += '<paper-fab class="listAvatar" style="background:#2196F3;" icon="person" item-icon></paper-fab>';
            }
            else if (program.IsSports) {
                html += '<paper-fab class="listAvatar" style="background:#8BC34A;" icon="person" item-icon></paper-fab>';
            }
            else if (program.IsMovie) {
                html += '<paper-fab class="listAvatar" icon="movie" item-icon></paper-fab>';
            }
            else if (program.IsNews) {
                html += '<paper-fab class="listAvatar" style="background:#673AB7;" icon="new-releases" item-icon></paper-fab>';
            }
            else {
                html += '<paper-fab class="listAvatar blue" icon="live-tv" item-icon></paper-fab>';
            }

            html += '<paper-item-body two-line>';
            html += '<a class="clearLink" href="livetvtimer.html?id=' + timer.Id + '">';

            html += '<div>';
            html += timer.Name;
            html += '</div>';

            html += '<div secondary>';
            html += LibraryBrowser.getDisplayTime(timer.StartDate);
            html += ' - ' + LibraryBrowser.getDisplayTime(timer.EndDate);
            html += '</div>';

            html += '</a>';
            html += '</paper-item-body>';

            if (timer.SeriesTimerId) {
                html += '<div class="ui-li-aside" style="right:0;">';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '</div>';
            }

            html += '<paper-icon-button icon="cancel" data-timerid="' + timer.Id + '" title="' + Globalize.translate('ButonCancelRecording') + '" class="btnDeleteTimer"></paper-icon-button>';

            html += '</paper-icon-item>';
        }

        if (timers.length) {
            html += '</div>';
            html += '</div>';
        }

        var elem = $('#items', page).html(html).trigger('create');

        $('.btnDeleteTimer', elem).on('click', function () {

            var id = this.getAttribute('data-timerid');

            deleteTimer(page, id);
        });

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvTimers().done(function (result) {

            renderTimers(page, result.Items);
        });
    }

    window.LiveTvPage.renderTimersTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            reload(tabContent);
        }
    };

})(jQuery, document);
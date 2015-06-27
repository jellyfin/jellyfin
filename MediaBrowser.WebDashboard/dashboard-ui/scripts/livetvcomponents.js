(function () {

    window.LiveTvHelpers = {

        getDaysOfWeek: function () {

            var days = [
                'Sunday',
                'Monday',
                'Tuesday',
                'Wednesday',
                'Thursday',
                'Friday',
                'Saturday'
            ];

            return days.map(function (d) {

                return {
                    name: d,
                    value: d
                };

            });
        },

        renderMiscProgramInfo: function (elem, obj) {

            var html = [];

            if (obj.IsSeries && !obj.IsRepeat) {

                html.push('<span class="newTvProgram">' + Globalize.translate('LabelNewProgram') + '</span>');

            }

            if (obj.IsLive) {

                html.push('<span class="liveTvProgram">' + Globalize.translate('LabelLiveProgram') + '</span>');

            }

            if (obj.ChannelId) {
                html.push('<a class="textlink" href="livetvchannel.html?id=' + obj.ChannelId + '">' + obj.ChannelName + '</a>');
            }

            if (obj.IsHD) {

                html.push(Globalize.translate('LabelHDProgram'));

            }

            if (obj.Audio) {

                html.push(obj.Audio);

            }

            html = html.join('&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;');

            if (obj.SeriesTimerId) {
                html += '<a href="livetvseriestimer.html?id=' + obj.SeriesTimerId + '" title="' + Globalize.translate('ButtonViewSeriesRecording') + '">';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '</a>';
            }
            else if (obj.TimerId) {

                html += '<a href="livetvtimer.html?id=' + obj.TimerId + '">';
                html += '<div class="timerCircle"></div>';
                html += '</a>';
            }

            elem.html(html).trigger('create');
        },

        renderOriginalAirDate: function (elem, item) {

            var airDate = item.OriginalAirDate;

            if (airDate && item.IsRepeat) {

                try {
                    airDate = parseISO8601Date(airDate, { toLocal: true }).toLocaleDateString();
                }
                catch (e) {
                    Logger.log("Error parsing date: " + airDate);
                }


                elem.html(Globalize.translate('ValueOriginalAirDate').replace('{0}', airDate)).show();
            } else {
                elem.hide();
            }
        }

    };
})();

(function ($, document, window) {

    var showOverlayTimeout;
    var hideOverlayTimeout;
    var currentPosterItem;

    function onOverlayMouseOver() {

        if (hideOverlayTimeout) {
            clearTimeout(hideOverlayTimeout);
            hideOverlayTimeout = null;
        }
    }

    function onOverlayMouseOut() {

        startHideOverlayTimer();
    }

    function getOverlayHtml(item) {

        var html = '';

        html += '<div class="itemOverlayContent">';

        if (item.EpisodeTitle) {
            html += '<p>';
            html += item.EpisodeTitle;
            html += '</p>';
        }

        html += '<p class="itemMiscInfo miscTvProgramInfo"></p>';

        html += '<p style="margin: 1.25em 0;">';
        html += '<span class="itemCommunityRating">';
        html += LibraryBrowser.getRatingHtml(item);
        html += '</span>';
        html += '<span class="userDataIcons">';
        html += LibraryBrowser.getUserDataIconsHtml(item);
        html += '</span>';
        html += '</p>';

        html += '<p class="itemGenres"></p>';

        html += '<p class="itemOverlayHtml">';
        html += (item.Overview || '');
        html += '</p>';

        html += '</div>';

        return html;
    }

    function showOverlay(elem, item) {

        $('.itemFlyout').popup('close').remove();

        var html = '<div data-role="popup" class="itemFlyout" data-theme="b" data-arrow="true" data-history="false">';

        html += '<div class="ui-bar-b" style="text-align:center;">';
        html += '<h3 style="margin: .5em 0;padding:0 1em;font-weight:normal;">' + item.Name + '</h3>';
        html += '</div>';

        html += '<div style="padding: 0 1em;">';
        html += getOverlayHtml(item);
        html += '</div>';

        html += '</div>';

        $('.itemFlyout').popup('close').popup('destroy').remove();

        $(document.body).append(html);

        var popup = $('.itemFlyout').on('mouseenter', onOverlayMouseOver).on('mouseleave', onOverlayMouseOut).popup({

            positionTo: elem

        }).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").off("mouseenter").off("mouseleave").remove();
        });

        LibraryBrowser.renderGenres($('.itemGenres', popup), item, 'livetv', 3);
        LiveTvHelpers.renderMiscProgramInfo($('.miscTvProgramInfo', popup), item);

        popup.parents().prev('.ui-popup-screen').remove();
        currentPosterItem = elem;
    }

    function onProgramClicked() {

        if (showOverlayTimeout) {
            clearTimeout(showOverlayTimeout);
            showOverlayTimeout = null;
        }

        if (hideOverlayTimeout) {
            clearTimeout(hideOverlayTimeout);
            hideOverlayTimeout = null;
        }

        hideOverlay();
    }

    function hideOverlay() {

        $('.itemFlyout').popup('close').remove();

        if (currentPosterItem) {

            $(currentPosterItem).off('click.overlay');
            currentPosterItem = null;
        }
    }

    function startHideOverlayTimer() {

        if (hideOverlayTimeout) {
            clearTimeout(hideOverlayTimeout);
            hideOverlayTimeout = null;
        }

        hideOverlayTimeout = setTimeout(hideOverlay, 200);
    }

    function onHoverOut() {

        if (showOverlayTimeout) {
            clearTimeout(showOverlayTimeout);
            showOverlayTimeout = null;
        }

        startHideOverlayTimer();
    }

    $.fn.createGuideHoverMenu = function (childSelector) {

        function onShowTimerExpired(elem) {

            var id = elem.getAttribute('data-programid');

            ApiClient.getLiveTvProgram(id, Dashboard.getCurrentUserId()).done(function (item) {

                showOverlay(elem, item);

            });
        }

        function onHoverIn() {

            if (showOverlayTimeout) {
                clearTimeout(showOverlayTimeout);
                showOverlayTimeout = null;
            }

            if (hideOverlayTimeout) {
                clearTimeout(hideOverlayTimeout);
                hideOverlayTimeout = null;
            }

            var elem = this;

            if (currentPosterItem) {
                if (currentPosterItem && currentPosterItem == elem) {
                    return;
                } else {
                    hideOverlay();
                }
            }

            showOverlayTimeout = setTimeout(function () {

                onShowTimerExpired(elem);

            }, 1000);
        }

        // https://hacks.mozilla.org/2013/04/detecting-touch-its-the-why-not-the-how/

        if (('ontouchstart' in window) || (navigator.maxTouchPoints > 0) || (navigator.msMaxTouchPoints > 0)) {
            /* browser with either Touch Events of Pointer Events
               running on touch-capable device */
            return this;
        }

        return this.on('mouseenter', childSelector, onHoverIn)
            .on('mouseleave', childSelector, onHoverOut)
            .on('click', childSelector, onProgramClicked);
    };

})(jQuery, document, window);
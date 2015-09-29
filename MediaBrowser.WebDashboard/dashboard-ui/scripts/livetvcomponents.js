(function () {

    function getTimersHtml(timers) {
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

        return html;
    }

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

        renderOriginalAirDate: function (elem, item) {

            var airDate = item.PremiereDate;

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
        },
        getTimersHtml: getTimersHtml

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

        html += '<div style="text-align:center;padding-bottom:.5em;">';

        var endDate;
        var startDate;
        var now = new Date().getTime();

        try {

            endDate = parseISO8601Date(item.EndDate, { toLocal: true });

        } catch (err) {
            endDate = now;
        }

        try {

            startDate = parseISO8601Date(item.StartDate, { toLocal: true });

        } catch (err) {
            startDate = now;
        }


        if (now < endDate && now >= startDate) {
            html += '<paper-button data-id="' + item.ChannelId + '" raised class="accent mini btnPlay"><iron-icon icon="play-arrow"></iron-icon><span>' + Globalize.translate('ButtonPlay') + '</span></paper-button>';
        }

        if (!item.TimerId && !item.SeriesTimerId) {
            html += '<paper-button data-id="' + item.Id + '" raised class="mini btnRecord" style="background-color:#cc3333;"><iron-icon icon="videocam"></iron-icon><span>' + Globalize.translate('ButtonRecord') + '</span></paper-button>';
        }

        html += '<div>';

        html += '</div>';

        return html;
    }

    function onPlayClick() {

        hideOverlay();

        MediaController.play({
            ids: [this.getAttribute('data-id')]
        });
    }

    function onRecordClick() {
        hideOverlay();

        Dashboard.navigate('livetvnewrecording.html?programid=' + this.getAttribute('data-id'));
    }

    function showOverlay(elem, item) {

        require(['jqmpopup'], function () {
            hideOverlay();

            var html = '<div data-role="popup" class="itemFlyout" data-theme="b" data-arrow="true" data-history="false">';

            html += '<div class="ui-bar-b" style="text-align:center;">';
            html += '<h3 style="margin: .5em 0;padding:.5em 1em;font-weight:normal;">' + item.Name + '</h3>';
            html += '</div>';

            html += '<div style="padding: 0 1em;">';
            html += getOverlayHtml(item);
            html += '</div>';

            html += '</div>';

            $(document.body).append(html);

            var popup = $('.itemFlyout').on('mouseenter', onOverlayMouseOver).on('mouseleave', onOverlayMouseOut).popup({

                positionTo: elem

            }).trigger('create').popup("open").on("popupafterclose", function () {

                $(this).off("popupafterclose").off("mouseenter").off("mouseleave").remove();
            });

            $('.btnPlay', popup).on('click', onPlayClick);
            $('.btnRecord', popup).on('click', onRecordClick);

            LibraryBrowser.renderGenres($('.itemGenres', popup), item, 3);
            $('.miscTvProgramInfo', popup).html(LibraryBrowser.getMiscInfoHtml(item)).trigger('create');

            popup.parents().prev('.ui-popup-screen').remove();
            currentPosterItem = elem;
        });
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

        var flyout = document.querySelectorAll('.itemFlyout');

        if (flyout.length) {
            $(flyout).popup('close').popup('destroy').remove();
        }

        if (currentPosterItem) {

            $(currentPosterItem).off('click');
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
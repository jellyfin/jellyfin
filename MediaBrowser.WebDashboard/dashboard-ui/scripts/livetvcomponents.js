(function () {

    function getTimersHtml(timers) {

        return new Promise(function (resolve, reject) {

            require(['paper-fab', 'paper-item-body', 'paper-icon-item'], function () {
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
                        html += '<paper-fab mini class="blue" style="background-image:url(\'' + imgUrl + '\');background-repeat:no-repeat;background-position:center center;background-size: cover;" item-icon></paper-fab>';
                    }
                    else if (program.IsKids) {
                        html += '<paper-fab mini style="background:#2196F3;" icon="person" item-icon></paper-fab>';
                    }
                    else if (program.IsSports) {
                        html += '<paper-fab mini style="background:#8BC34A;" icon="person" item-icon></paper-fab>';
                    }
                    else if (program.IsMovie) {
                        html += '<paper-fab mini icon="movie" item-icon></paper-fab>';
                    }
                    else if (program.IsNews) {
                        html += '<paper-fab mini style="background:#673AB7;" icon="new-releases" item-icon></paper-fab>';
                    }
                    else {
                        html += '<paper-fab mini class="blue" icon="live-tv" item-icon></paper-fab>';
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

                resolve(html);
            });
        });
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

        require(['components/paperdialoghelper', 'scale-up-animation', 'fade-out-animation'], function () {

            var dlg = document.createElement('paper-dialog');

            dlg.setAttribute('with-backdrop', 'with-backdrop');
            dlg.setAttribute('role', 'alertdialog');

            // seeing max call stack size exceeded in the debugger with this
            dlg.setAttribute('noAutoFocus', 'noAutoFocus');
            dlg.entryAnimation = 'scale-up-animation';
            dlg.exitAnimation = 'fade-out-animation';
            dlg.classList.add('ui-body-b');
            dlg.classList.add('background-theme-b');
            dlg.classList.add('tvProgramOverlay');

            var html = '';
            html += '<h2 class="dialogHeader">';
            html += item.Name;
            html += '</h2>';

            html += '<div>';
            html += getOverlayHtml(item);
            html += '</div>';

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            // Has to be assigned a z-index after the call to .open() 
            $(dlg).on('iron-overlay-closed', function () {

                $(dlg).off('mouseenter', onOverlayMouseOver);
                $(dlg).off('mouseleave', onOverlayMouseOut);

                this.parentNode.removeChild(this);

                if (currentPosterItem) {

                    currentPosterItem = null;
                }
            });

            $('.btnPlay', dlg).on('click', onPlayClick);
            $('.btnRecord', dlg).on('click', onRecordClick);

            LibraryBrowser.renderGenres($('.itemGenres', dlg), item, 3);
            $('.miscTvProgramInfo', dlg).html(LibraryBrowser.getMiscInfoHtml(item));

            PaperDialogHelper.positionTo(dlg, elem);

            dlg.open();

            $(dlg).on('mouseenter', onOverlayMouseOver);
            $(dlg).on('mouseleave', onOverlayMouseOut);

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

        var flyout = document.querySelector('.tvProgramOverlay');

        if (flyout) {
            flyout.close();
        }
    }

    function startHideOverlayTimer() {

        if (hideOverlayTimeout) {
            clearTimeout(hideOverlayTimeout);
            hideOverlayTimeout = null;
        }

        hideOverlayTimeout = setTimeout(hideOverlay, 200);
    }

    $.fn.createGuideHoverMenu = function (childSelector) {

        function onShowTimerExpired(elem) {

            var id = elem.getAttribute('data-programid');

            ApiClient.getLiveTvProgram(id, Dashboard.getCurrentUserId()).then(function (item) {

                showOverlay(elem, item);

            });
        }

        function onHoverOut() {
            if (showOverlayTimeout) {
                clearTimeout(showOverlayTimeout);
                showOverlayTimeout = null;
            }
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

        if (AppInfo.isTouchPreferred) {
            /* browser with either Touch Events of Pointer Events
               running on touch-capable device */
            return this;
        }

        return this.on('mouseenter', childSelector, onHoverIn)
            .on('mouseleave', childSelector, onHoverOut)
            .on('click', childSelector, onProgramClicked);
    };

})(jQuery, document, window);
define([], function () {

    function getTimersHtml(timers) {

        return new Promise(function (resolve, reject) {

            require(['paper-fab', 'paper-item-body', 'paper-icon-item'], function () {
                var html = '';
                var index = '';
                var imgUrl;

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

                    imgUrl = null;
                    if (program.ImageTags && program.ImageTags.Primary) {

                        imgUrl = ApiClient.getScaledImageUrl(program.Id, {
                            height: 80,
                            tag: program.ImageTags.Primary,
                            type: "Primary"
                        });
                    }

                    if (imgUrl) {
                        html += '<paper-fab mini class="blue lazy" data-src="' + imgUrl + '" style="background-repeat:no-repeat;background-position:center center;background-size: cover;" item-icon></paper-fab>';
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
                    console.log("Error parsing date: " + airDate);
                }


                elem.html(Globalize.translate('ValueOriginalAirDate').replace('{0}', airDate)).show();
            } else {
                elem.hide();
            }
        },
        getTimersHtml: getTimersHtml

    };
});
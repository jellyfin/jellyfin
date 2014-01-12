(function ($, document, apiClient) {

    function deleteTimer(page, id) {

        Dashboard.confirm("Are you sure you wish to cancel this recording?", "Confirm Recording Cancellation", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).done(function () {

                    Dashboard.alert('Recording cancelled.');

                    reload(page);
                });
            }

        });
    }

    function renderTimers(page, timers) {

        var html = '';

        html += '<ul data-role="listview" data-inset="true" data-split-icon="delete">';

        var index = '';

        for (var i = 0, length = timers.length; i < length; i++) {

            var timer = timers[i];

            var startDateText = LibraryBrowser.getFutureDateText(parseISO8601Date(timer.StartDate, { toLocal: true }));

            if (startDateText != index) {
                html += '<li data-role="list-divider">' + startDateText + '</li>';
                index = startDateText;
            }

            html += '<li><a href="livetvtimer.html?id=' + timer.Id + '">';

            var program = timer.ProgramInfo || {};
            var imgUrl;
            
            if (program.ImageTags && program.ImageTags.Primary) {

                imgUrl = ApiClient.getImageUrl(program.Id, {
                    height: 160,
                    tag: program.ImageTags.Primary,
                    type: "Primary"
                });
            } else {
                imgUrl = "css/images/items/searchhintsv2/tv.png";
            }

            html += '<img src="css/images/items/searchhintsv2/tv.png" style="display:none;">';
            html += '<div class="ui-li-thumb" style="background-image:url(\'' + imgUrl + '\');width:5em;height:5em;background-repeat:no-repeat;background-position:center center;background-size: cover;"></div>';

            html += '<h3>';
            html += timer.Name;
            html += '</h3>';

            html += '<p>';
            html += LiveTvHelpers.getDisplayTime(timer.StartDate);
            html += ' - ' + LiveTvHelpers.getDisplayTime(timer.EndDate);
            html += '</p>';


            if (timer.SeriesTimerId) {
                html += '<div class="ui-li-aside" style="right:0;">';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '</div>';
            }

            html += '</a>';

            html += '<a data-timerid="' + timer.Id + '" href="#" title="Cancel Recording" class="btnDeleteTimer">Cancel Recording</a>';

            html += '</li>';
        }

        html += '</ul>';

        //var cssClass = "detailTable";

        //html += '<div class="detailTableContainer"><table class="detailTable" >';

        //html += '<thead>';
        //html += '<tr>';

        //html += '<th class="tabletColumn">&nbsp;</th>';
        //html += '<th>Name</th>';
        //html += '<th class="desktopColumn">Channel</th>';
        //html += '<th>Date</th>';
        //html += '<th>Time</th>';
        //html += '<th class="tabletColumn">Length</th>';
        //html += '<th class="tabletColumn">Status</th>';
        //html += '<th class="desktopColumn">Series</th>';

        //html += '</tr>';
        //html += '</thead>';

        //html += '<tbody>';

        //for (var i = 0, length = timers.length; i < length; i++) {

        //    var timer = timers[i];

        //    html += '<tr>';

        //    html += '<td class="tabletColumn">';
        //    html += '<button data-timerid="' + timer.Id + '" class="btnDeleteTimer" type="button" data-icon="delete" data-inline="true" data-mini="true" data-iconpos="notext">Cancel</button>';
        //    html += '</td>';

        //    html += '<td>';
        //    html += '<a href="livetvtimer.html?id=' + timer.Id + '">' + timer.Name + '</a>';
        //    html += '</td>';

        //    html += '<td class="desktopColumn">';
        //    if (timer.ChannelId) {
        //        html += '<a href="livetvchannel.html?id=' + timer.ChannelId + '">' + timer.ChannelName + '</a>';
        //    }
        //    html += '</td>';

        //    var startDate = timer.StartDate;

        //    try {

        //        startDate = parseISO8601Date(startDate, { toLocal: true });

        //    } catch (err) {

        //    }

        //    html += '<td>' + startDate.toLocaleDateString() + '</td>';

        //    html += '<td>' + LiveTvHelpers.getDisplayTime(timer.StartDate) + '</td>';

        //    var minutes = timer.RunTimeTicks / 600000000;

        //    minutes = minutes || 1;

        //    html += '<td class="tabletColumn">' + Math.round(minutes) + 'min</td>';

        //    html += '<td class="tabletColumn">';

        //    if (timer.Status == 'ConflictedNotOk' || timer.Status == 'Error') {

        //        html += '<span style="color:red;">';
        //        html += timer.Status;
        //        html += '</span>';

        //    } else {
        //        html += timer.Status;
        //    }

        //    html += '</td>';

        //    html += '<td class="desktopColumn">';

        //    if (timer.SeriesTimerId) {
        //        html += '<a href="livetvseriestimer.html?id=' + timer.SeriesTimerId + '" title="View Series Recording">';
        //        html += '<div class="timerCircle seriesTimerCircle"></div>';
        //        html += '<div class="timerCircle seriesTimerCircle"></div>';
        //        html += '<div class="timerCircle seriesTimerCircle"></div>';
        //        html += '</a>';
        //    }

        //    html += '</td>';

        //    html += '</tr>';
        //}

        //html += '</tbody>';
        //html += '</table></div>';

        var elem = $('#items', page).html(html).trigger('create');

        $('.btnDeleteTimer', elem).on('click', function () {

            var id = this.getAttribute('data-timerid');

            deleteTimer(page, id);
        });

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        apiClient.getLiveTvTimers().done(function (result) {

            renderTimers(page, result.Items);

        });
    }

    $(document).on('pagebeforeshow', "#liveTvTimersPage", function () {

        var page = this;

        reload(page);
    });

})(jQuery, document, ApiClient);
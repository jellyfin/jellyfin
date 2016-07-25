define(['datetime', 'tvguide'], function (datetime) {

    function renderPrograms(page, result) {

        var html = '';

        var currentIndexValue;

        var now = new Date();

        for (var i = 0, length = result.Items.length; i < length; i++) {

            var program = result.Items[i];

            var startDate = datetime.parseISO8601Date(program.StartDate, true);
            var startDateText = LibraryBrowser.getFutureDateText(startDate);

            var endDate = datetime.parseISO8601Date(program.EndDate, true);

            if (startDateText != currentIndexValue) {

                html += '<h1 tvProgramSectionHeader" style="margin-bottom:1em;margin-top:2em;">' + startDateText + '</h1>';
                currentIndexValue = startDateText;
            }

            html += '<a href="itemdetails.html?id=' + program.Id + '" class="tvProgram">';

            var cssClass = "tvProgramTimeSlot";

            if (now >= startDate && now < endDate) {
                cssClass += " tvProgramCurrentTimeSlot";
            }

            html += '<div class="' + cssClass + '">';
            html += '<div class="tvProgramTimeSlotInner">' + datetime.getDisplayTime(startDate) + '</div>';
            html += '</div>';

            cssClass = "tvProgramInfo";

            if (program.IsKids) {
                cssClass += " childProgramInfo";
            }
            else if (program.IsSports) {
                cssClass += " sportsProgramInfo";
            }
            else if (program.IsNews) {
                cssClass += " newsProgramInfo";
            }
            else if (program.IsMovie) {
                cssClass += " movieProgramInfo";
            }

            html += '<div data-programid="' + program.Id + '" class="' + cssClass + '">';

            var name = program.Name;

            html += '<div class="tvProgramName">' + name + '</div>';

            html += '<div class="tvProgramTime">';

            if (program.IsLive) {
                html += '<span class="liveTvProgram">' + Globalize.translate('AttributeLive') + '&nbsp;&nbsp;</span>';
            }
            else if (program.IsPremiere) {
                html += '<span class="premiereTvProgram">' + Globalize.translate('AttributePremiere') + '&nbsp;&nbsp;</span>';
            }
            else if (program.IsSeries && !program.IsRepeat) {
                html += '<span class="newTvProgram">' + Globalize.translate('AttributeNew') + '&nbsp;&nbsp;</span>';
            }

            var minutes = program.RunTimeTicks / 600000000;

            minutes = Math.round(minutes || 1) + ' min';

            if (program.EpisodeTitle) {

                html += program.EpisodeTitle + '&nbsp;&nbsp;(' + minutes + ')';
            } else {
                html += minutes;
            }

            if (program.SeriesTimerId) {
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
                html += '<div class="timerCircle seriesTimerCircle"></div>';
            }
            else if (program.TimerId) {

                html += '<div class="timerCircle"></div>';
            }

            html += '</div>';
            html += '<div class="programAccent"></div>';
            html += '</div>';

            html += '</a>';
        }

        page.querySelector('#childrenContent').innerHTML = html;
    }

    function loadPrograms(page, channelId) {

        ApiClient.getLiveTvPrograms({

            ChannelIds: channelId,
            UserId: Dashboard.getCurrentUserId(),
            HasAired: false,
            SortBy: "StartDate"

        }).then(function (result) {

            renderPrograms(page, result);
            Dashboard.hideLoadingMsg();
        });
    }

    window.LiveTvChannelPage = {
        renderPrograms: loadPrograms
    };

});
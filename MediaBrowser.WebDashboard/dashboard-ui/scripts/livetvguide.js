(function ($, document, apiClient) {

    // 30 mins
    var cellDurationMs = 30 * 60 * 1000;

    function formatDigit(i) {
        return i < 10 ? "0" + i : i;
    }

    function getDateFormat(date) {

        // yyyyMMddHHmmss
        // Convert to UTC
        // http://stackoverflow.com/questions/948532/how-do-you-convert-a-javascript-date-to-utc/14610512#14610512
        var d = new Date(date.getTime());

        return "" + d.getFullYear() + formatDigit(d.getMonth() + 1) + formatDigit(d.getDate()) + formatDigit(d.getHours()) + formatDigit(d.getMinutes()) + formatDigit(d.getSeconds());
    }

    function normalizeDateToTimeslot(date) {

        var minutesOffset = date.getMinutes() - 30;

        if (minutesOffset >= 0) {

            date.setTime(date.getTime() - (minutesOffset * 60 * 1000));

        } else {

            date.setTime(date.getTime() - (date.getMinutes() * 60 * 1000));
        }

        date.setTime(date.getTime() - (date.getSeconds() * 1000));

        date.setHours(date.getHours(), date.getMinutes(), 0, 0);

        return date;
    }

    var currentDate;
    var channelsPromise;

    function reloadGuide(page) {

        channelsPromise = channelsPromise || apiClient.getLiveTvChannels({

            userId: Dashboard.getCurrentUserId()

        });;

        var date = currentDate;

        var nextDay = new Date(date.getTime());
        nextDay.setDate(nextDay.getDate() + 2);
        nextDay.setHours(1, 0, 0, 0);

        var promise1 = channelsPromise;
        var promise2 = apiClient.getLiveTvPrograms({

            UserId: Dashboard.getCurrentUserId(),
            MaxStartDate: getDateFormat(nextDay)

        });

        $.when(promise1, promise2).done(function (response1, response2) {

            var channels = response1[0].Items;
            var programs = response2[0].Items;

            renderGuide(page, date, channels, programs);
        });
    }

    function getTimeslotHeadersHtml(date) {

        var html = '';

        date = new Date(date.getTime());
        var dateNumber = date.getDate();

        while (date.getDate() == dateNumber) {

            html += '<div class="timeslotHeader">';
            html += '<div class="timeslotHeaderInner">';
            html += LiveTvHelpers.getDisplayTime(date);
            html += '</div>';
            html += '</div>';

            // Add 30 mins
            date.setTime(date.getTime() + cellDurationMs);
        }

        return html;
    }

    function findProgramStartingInCell(programs, startIndex, cellStart, cellEnd, cellIndex) {

        for (var i = startIndex, length = programs.length; i < length; i++) {

            var program = programs[i];

            if (!program.StartDateLocal) {
                try {

                    program.StartDateLocal = parseISO8601Date(program.StartDate, { toLocal: true });

                } catch (err) {

                }

            }

            if (!program.EndDateLocal) {
                try {

                    program.EndDateLocal = parseISO8601Date(program.EndDate, { toLocal: true });

                } catch (err) {

                }

            }

            var localTime = program.StartDateLocal.getTime();
            if ((localTime >= cellStart || cellIndex == 0) && localTime < cellEnd && program.EndDateLocal > cellStart) {

                return {

                    index: i,
                    program: program
                };

            }
        }

        return null;
    }

    function getChannelProgramsHtml(page, date, channel, programs) {

        var html = '';

        var dateNumber = date.getDate();

        programs = programs.filter(function (curr) {
            return curr.ChannelId == channel.Id;
        });

        html += '<div class="channelPrograms">';

        var programIndex = 0;
        var cellIndex = 0;
        
        while (date.getDate() == dateNumber) {

            // Add 30 mins
            var cellEndDate = new Date(date.getTime() + cellDurationMs);

            var program = findProgramStartingInCell(programs, programIndex, date, cellEndDate, cellIndex);

            if (program) {
                programIndex = program.index + 1;
                program = program.program;
            }

            html += '<div class="timeslotCell">';

            var cssClass = "timeslotCellInner";

            if (program) {
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
            }

            html += '<div class="' + cssClass + '">';
            
            if (program) {

                html += '<div class="guideProgramName">';
                html += program.Name;
                html += '</div>';
                
                html += '<div class="guideProgramTime">';

                if (program.IsLive) {
                    html += '<span class="liveTvProgram">LIVE&nbsp;&nbsp;</span>';
                }
                else if (program.IsPremiere) {
                    html += '<span class="premiereTvProgram">PREMIERE&nbsp;&nbsp;</span>';
                }
                else if (program.IsSeries && !program.IsRepeat) {
                    html += '<span class="newTvProgram">NEW&nbsp;&nbsp;</span>';
                }

                html += LiveTvHelpers.getDisplayTime(program.StartDateLocal);
                html += ' - ';
                html += LiveTvHelpers.getDisplayTime(program.EndDateLocal);

                html += '</div>';

            } else {
                html += '&nbsp;';
            }

            html += '</div>';
            html += '</div>';

            date = cellEndDate;
            cellIndex++;
        }
        html += '</div>';

        return html;
    }

    function renderPrograms(page, date, channels, programs) {

        var html = [];

        for (var i = 0, length = channels.length; i < length; i++) {

            html.push(getChannelProgramsHtml(page, date, channels[i], programs));
        }

        $('.programGrid', page).html(html.join(''));
    }

    function renderChannelHeaders(page, channels) {

        var html = '';

        for (var i = 0, length = channels.length; i < length; i++) {

            var channel = channels[i];

            html += '<div class="channelHeaderCellContainer">';

            html += '<div class="channelHeaderCell"><div class="channelHeaderCellInner">';
            html += channel.Name + '<br/>' + channel.Number;
            html += '</div>';
            html += '</div>';

            html += '</div>';
        }

        $('.channelList', page).html(html);
    }

    function renderGuide(page, date, channels, programs) {

        renderChannelHeaders(page, channels);
        $('.timeslotHeaders', page).html(getTimeslotHeadersHtml(date));
        renderPrograms(page, date, channels, programs);
    }

    function onProgramGridScroll(page, elem) {

        var grid = $(elem);

        grid.prev().scrollTop(grid.scrollTop());
        $('.timeslotHeaders', page).scrollLeft(grid.scrollLeft());
    }

    $(document).on('pageinit', "#liveTvGuidePage", function () {

        var page = this;

        $('.programGrid', page).on('scroll', function () {

            onProgramGridScroll(page, this);
        });

    }).on('pagebeforeshow', "#liveTvGuidePage", function () {

        var page = this;

        currentDate = normalizeDateToTimeslot(new Date());

        reloadGuide(page);
    });

})(jQuery, document, ApiClient);
(function ($, document, apiClient) {

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
        nextDay.setDate(nextDay.getDate() + 1);
        nextDay.setHours(1, 0, 0, 0);

        var promise1 = channelsPromise;
        var promise2 = apiClient.getLiveTvPrograms({

            UserId: Dashboard.getCurrentUserId(),
            MaxEndDate: getDateFormat(nextDay)

        });

        $.when(promise1, promise2).done(function (response1, response2) {

            var channels = response1[0].Items;
            var programs = response2[0].Items;

            renderGuide(page, date, channels, programs);
        });
    }

    function renderDate(page, date) {

        $('.guideDate', page).html(LibraryBrowser.getFutureDateText(date));

        $('.timeslotHeaders', page).html(getTimeslotHeadersHtml(date));
    }

    function getTimeslotHeadersHtml(date) {

        var html = '';

        html += '<div class="timeslotHeader channelTimeslotHeader">&nbsp;</div>';

        date = new Date(date.getTime());
        var dateNumber = date.getDate();

        while (date.getDate() == dateNumber) {

            html += '<div class="timeslotHeader">';
            html += LiveTvHelpers.getDisplayTime(date);
            html += '</div>';

            // Add 30 mins
            date.setTime(date.getTime() + (30 * 60 * 1000));
        }

        return html;
    }

    function getChannelHtml(page, date, channel, programs) {

        var html = '';

        html += '<div class="guideChannel">';

        html += '<div class="timeslotCell channelTimeslotCell">';
        html += channel.Name + '<br/>' + channel.Number;
        html += '</div>';

        html += '</div>';

        return html;
    }

    function renderChannels(page, date, channels, programs) {

        var html = [];

        for (var i = 0, length = channels.length; i < length; i++) {

            html.push(getChannelHtml(page, date, channels[i], programs));
        }

        $('#guide', page).html(html.join(''));
    }

    function renderGuide(page, date, channels, programs) {

        renderDate(page, date);
        renderChannels(page, date, channels, programs);
    }

    $(document).on('pagebeforeshow', "#liveTvGuidePage", function () {

        var page = this;

        currentDate = normalizeDateToTimeslot(new Date());

        reloadGuide(page);
    });

})(jQuery, document, ApiClient);
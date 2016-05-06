define(['datetime'], function (datetime) {

    function getTimersHtml(timers) {

        var items = timers.map(function (t) {
            t.Type = 'Timer';
            return t;
        });

        var html = LibraryBrowser.getPosterViewHtml({
            items: items,
            shape: "square",
            showTitle: true,
            showAirTime: true,
            showChannelName: true,
            lazy: true,
            cardLayout: true,
            showDetailsMenu: true
        });

        return Promise.resolve(html);
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
                    airDate = datetime.parseISO8601Date(airDate, true).toLocaleDateString();
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
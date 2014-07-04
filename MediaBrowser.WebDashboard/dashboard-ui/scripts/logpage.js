(function () {

    $(document).on('pagebeforeshow', "#logPage", function () {

        var page = this;

        ApiClient.getJSON(ApiClient.getUrl('System/Logs')).done(function (logs) {

            var html = '';

            html += '<ul data-role="listview" data-inset="true">';

            html += logs.map(function (log) {

                var logUrl = ApiClient.getUrl('System/Logs/Log', {
                    name: log.Name
                });
                var logHtml = '<li><a href="' + logUrl + '" target="_blank">';

                logHtml += '<h3>';
                logHtml += log.Name;
                logHtml += '</h3>';

                var date = parseISO8601Date(log.DateModified, { toLocal: true });

                var text = date.toLocaleDateString();

                text += ' ' + LiveTvHelpers.getDisplayTime(date);

                logHtml += '<p>' + text + '</p>';

                logHtml += '</li>';

                return logHtml;

            })
                .join('');

            html += '</ul>';

            $('.serverLogs', page).html(html).trigger('create');

        });


    });

})();
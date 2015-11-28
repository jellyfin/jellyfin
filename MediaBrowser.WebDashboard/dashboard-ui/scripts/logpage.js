(function () {

    $(document).on('pageshow', "#logPage", function () {

        var page = this;

        var apiClient = ApiClient;

        apiClient.getJSON(apiClient.getUrl('System/Logs')).then(function (logs) {

            var html = '';

            html += '<ul data-role="listview" data-inset="true">';

            html += logs.map(function (log) {

                var logUrl = apiClient.getUrl('System/Logs/Log', {
                    name: log.Name
                });

                logUrl += "&api_key=" + apiClient.accessToken();

                var logHtml = '<li><a href="' + logUrl + '" target="_blank">';

                logHtml += '<h3>';
                logHtml += log.Name;
                logHtml += '</h3>';

                var date = parseISO8601Date(log.DateModified, { toLocal: true });

                var text = date.toLocaleDateString();

                text += ' ' + LibraryBrowser.getDisplayTime(date);

                logHtml += '<p>' + text + '</p>';

                logHtml += '</li>';

                return logHtml;

            })
                .join('');

            html += '</ul>';

            Events.trigger($('.serverLogs', page).html(html)[0], 'create');

        });
    });

})();
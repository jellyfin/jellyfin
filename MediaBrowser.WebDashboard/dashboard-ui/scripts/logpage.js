(function () {

    $(document).on('pagebeforeshow', "#logPage", function () {

        var page = this;
        Dashboard.showLoadingMsg();

        require(['paper-fab', 'paper-item-body', 'paper-icon-item'], function () {

            var apiClient = ApiClient;

            apiClient.getJSON(apiClient.getUrl('System/Logs')).then(function (logs) {

                var html = '';

                html += '<div class="paperList">';

                html += logs.map(function (log) {

                    var logUrl = apiClient.getUrl('System/Logs/Log', {
                        name: log.Name
                    });

                    logUrl += "&api_key=" + apiClient.accessToken();

                    var logHtml = '';
                    logHtml += '<paper-icon-item>';

                    logHtml += '<a item-icon class="clearLink" href="' + logUrl + '" target="_blank">';
                    logHtml += '<paper-fab mini icon="schedule" class="blue" item-icon></paper-fab>';
                    logHtml += "</a>";

                    logHtml += '<paper-item-body two-line>';
                    logHtml += '<a class="clearLink" href="' + logUrl + '" target="_blank">';

                    logHtml += "<div>" + log.Name + "</div>";

                    var date = parseISO8601Date(log.DateModified, { toLocal: true });

                    var text = date.toLocaleDateString();

                    text += ' ' + LibraryBrowser.getDisplayTime(date);

                    logHtml += '<div secondary>' + text + '</div>';

                    logHtml += "</a>";
                    logHtml += '</paper-item-body>';

                    logHtml += '</paper-icon-item>';

                    return logHtml;

                })
                    .join('');

                html += '</div>';

                $('.serverLogs', page).html(html).trigger('create');
                Dashboard.hideLoadingMsg();

            });
        });
    });

})();
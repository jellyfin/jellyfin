define(['datetime', 'listViewStyle'], function (datetime) {

    return function (view, params) {

        view.querySelector('#chkDebugLog').addEventListener('change', function () {

            ApiClient.getServerConfiguration().then(function (config) {

                config.EnableDebugLevelLogging = view.querySelector('#chkDebugLog').checked;

                ApiClient.updateServerConfiguration(config);
            });
        });

        view.addEventListener('viewbeforeshow', function () {

            Dashboard.showLoadingMsg();

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
                    logHtml += '<div class="listItem">';

                    logHtml += '<a item-icon class="clearLink" href="' + logUrl + '" target="_blank">';
                    logHtml += '<i class="md-icon listItemIcon">schedule</i>';
                    logHtml += "</a>";

                    logHtml += '<div class="listItemBody two-line">';
                    logHtml += '<a class="clearLink" href="' + logUrl + '" target="_blank">';

                    logHtml += "<h3 class='listItemBodyText'>" + log.Name + "</h3>";

                    var date = datetime.parseISO8601Date(log.DateModified, true);

                    var text = datetime.toLocaleDateString(date);

                    text += ' ' + datetime.getDisplayTime(date);

                    logHtml += '<div class="listItemBodyText secondary">' + text + '</div>';

                    logHtml += "</a>";
                    logHtml += '</div>';

                    logHtml += '</div>';

                    return logHtml;

                })
                    .join('');

                html += '</div>';

                view.querySelector('.serverLogs').innerHTML = html;
                Dashboard.hideLoadingMsg();
            });

            apiClient.getServerConfiguration().then(function (config) {

                view.querySelector('#chkDebugLog').checked = config.EnableDebugLevelLogging;
            });
        });

    };
});
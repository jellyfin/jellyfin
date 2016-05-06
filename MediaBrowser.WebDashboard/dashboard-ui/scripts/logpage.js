define(['datetime', 'jQuery', 'paper-fab', 'paper-item-body', 'paper-icon-item'], function (datetime, $) {

    function getTabs() {
        return [
        {
            href: 'about.html',
            name: Globalize.translate('TabAbout')
        },
         {
             href: 'log.html',
             name: Globalize.translate('TabLogs')
         },
         {
             href: 'supporterkey.html',
             name: Globalize.translate('TabEmbyPremiere')
         }];
    }

    return function (view, params) {

        view.querySelector('#chkDebugLog').addEventListener('change', function () {

            ApiClient.getServerConfiguration().then(function (config) {

                config.EnableDebugLevelLogging = view.querySelector('#chkDebugLog').checked;

                ApiClient.updateServerConfiguration(config);
            });
        });

        view.addEventListener('viewbeforeshow', function () {

            LibraryMenu.setTabs('helpadmin', 1, getTabs);
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
                    logHtml += '<paper-icon-item>';

                    logHtml += '<a item-icon class="clearLink" href="' + logUrl + '" target="_blank">';
                    logHtml += '<paper-fab mini icon="schedule" class="blue" item-icon></paper-fab>';
                    logHtml += "</a>";

                    logHtml += '<paper-item-body two-line>';
                    logHtml += '<a class="clearLink" href="' + logUrl + '" target="_blank">';

                    logHtml += "<div>" + log.Name + "</div>";

                    var date = datetime.parseISO8601Date(log.DateModified, true);

                    var text = date.toLocaleDateString();

                    text += ' ' + datetime.getDisplayTime(date);

                    logHtml += '<div secondary>' + text + '</div>';

                    logHtml += "</a>";
                    logHtml += '</paper-item-body>';

                    logHtml += '</paper-icon-item>';

                    return logHtml;

                })
                    .join('');

                html += '</div>';

                $('.serverLogs', view).html(html);
                Dashboard.hideLoadingMsg();
            });

            apiClient.getServerConfiguration().then(function (config) {

                view.querySelector('#chkDebugLog').checked = config.EnableDebugLevelLogging;
            });
        });

    };
});
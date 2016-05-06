define(['datetime', 'jQuery'], function (datetime, $) {

    function revoke(page, key) {

        require(['confirm'], function (confirm) {

            confirm(Globalize.translate('MessageConfirmRevokeApiKey'), Globalize.translate('HeaderConfirmRevokeApiKey')).then(function () {
                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl('Auth/Keys/' + key)

                }).then(function () {

                    loadData(page);
                });
            });

        });
    }

    function renderKeys(page, keys, users) {

        var rows = keys.map(function (item) {

            var html = '';

            html += '<tr>';

            html += '<td>';
            html += '<button data-token="' + item.AccessToken + '" class="btnRevoke" data-mini="true" title="' + Globalize.translate('ButtonRevoke') + '" style="margin:0;">' + Globalize.translate('ButtonRevoke') + '</button>';
            html += '</td>';

            html += '<td style="vertical-align:middle;">';
            html += (item.AccessToken);
            html += '</td>';

            html += '<td style="vertical-align:middle;">';
            html += (item.AppName || '');
            html += '</td>';

            html += '<td style="vertical-align:middle;">';
            html += (item.DeviceName || '');
            html += '</td>';

            html += '<td style="vertical-align:middle;">';

            var user = users.filter(function (u) {

                return u.Id == item.UserId;
            })[0];

            if (user) {
                html += user.Name;
            }

            html += '</td>';

            html += '<td style="vertical-align:middle;">';

            var date = datetime.parseISO8601Date(item.DateCreated, true);

            html += datetime.toLocaleDateString(date) + ' ' + datetime.getDisplayTime(date);

            html += '</td>';

            html += '</tr>';

            return html;

        }).join('');

        var elem = $('.resultBody', page).html(rows).parents('.tblApiKeys').table("refresh").trigger('create');

        $('.btnRevoke', elem).on('click', function () {

            revoke(page, this.getAttribute('data-token'));
        });

        Dashboard.hideLoadingMsg();
    }

    function loadData(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getUsers().then(function (users) {

            ApiClient.getJSON(ApiClient.getUrl('Auth/Keys')).then(function (result) {

                renderKeys(page, result.Items, users);
            });
        });
    }

    function showNewKeyPrompt(page) {
        require(['prompt'], function (prompt) {
            
            // HeaderNewApiKeyHelp not used

            prompt({
                title: Globalize.translate('HeaderNewApiKey'),
                label: Globalize.translate('LabelAppName'),
                description: Globalize.translate('LabelAppNameExample')

            }).then(function (value) {

                ApiClient.ajax({
                    type: "POST",
                    url: ApiClient.getUrl('Auth/Keys', {

                        App: value

                    })

                }).then(function () {

                    loadData(page);
                });
            });

        });
    }

    function getTabs() {
        return [
        {
            href: 'dashboardhosting.html',
            name: Globalize.translate('TabHosting')
        },
         {
             href: 'serversecurity.html',
             name: Globalize.translate('TabSecurity')
         }];
    }

    pageIdOn('pageinit', "serverSecurityPage", function () {

        var page = this;

        $('.btnNewKey', page).on('click', function () {

            showNewKeyPrompt(page);

        });

    });
    pageIdOn('pagebeforeshow', "serverSecurityPage", function () {

        LibraryMenu.setTabs('adminadvanced', 1, getTabs);
        var page = this;

        loadData(page);
    });

});
(function ($, document) {

    function revoke(page, key) {

        Dashboard.confirm(Globalize.translate('MessageConfirmRevokeApiKey'), Globalize.translate('HeaderConfirmRevokeApiKey'), function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl('Auth/Keys/' + key)

                }).then(function () {

                    loadData(page);
                });
            }

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

            var date = parseISO8601Date(item.DateCreated, { toLocal: true });

            html += date.toLocaleDateString() + ' ' + LibraryBrowser.getDisplayTime(date);

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

    function onSubmit() {
        var form = this;
        var page = $(form).parents('.page');

        Dashboard.showLoadingMsg();

        ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl('Auth/Keys', {

                App: $('#txtAppName', form).val()

            })

        }).then(function () {

            $('.newKeyPanel', page).panel('close');

            loadData(page);
        });

        return false;
    }

    pageIdOn('pageinit', "serverSecurityPage", function () {

        var page = this;

        $('.btnNewKey', page).on('click', function () {

            $('.newKeyPanel', page).panel('toggle');

            $('#txtAppName', page).val('').focus();

        });

        $('.newKeyForm').off('submit', onSubmit).on('submit', onSubmit);

    });
    pageIdOn('pagebeforeshow', "serverSecurityPage", function () {

        var page = this;

        loadData(page);
    });

})(jQuery, document);
define(['jQuery'], function ($) {

    function getApiClient() {
        return ApiClient;
    }

    function onUpdateUserComplete(result) {

        Dashboard.hideLoadingMsg();

        if (result.UserLinkResult) {

            var msgKey = result.UserLinkResult.IsPending ? 'MessagePendingEmbyAccountAdded' : 'MessageEmbyAccountAdded';

            Dashboard.alert({
                message: Globalize.translate(msgKey),
                title: Globalize.translate('HeaderEmbyAccountAdded'),

                callback: function () {
                    Dashboard.navigate('wizardlibrary.html');
                }

            });

        } else {
            Dashboard.navigate('wizardlibrary.html');
        }
    }

    function submit(form) {

        Dashboard.showLoadingMsg();

        var apiClient = getApiClient();

        apiClient.ajax({

            type: 'POST',
            data: {

                Name: form.querySelector('#txtUsername').value,
                ConnectUserName: form.querySelector('#txtConnectUserName').value

            },
            url: apiClient.getUrl('Startup/User'),
            dataType: 'json'

        }).then(onUpdateUserComplete, function () {

            showEmbyConnectErrorMessage(form.querySelector('#txtConnectUserName').value);
        });
    }

    function showEmbyConnectErrorMessage(username) {

        var msg;

        if (username) {

            msg = Globalize.translate('ErrorAddingEmbyConnectAccount1', '<a href="https://emby.media/connect" target="_blank">https://emby.media/connect</a>');
            msg += '<br/><br/>' + Globalize.translate('ErrorAddingEmbyConnectAccount2', 'apps@emby.media');

        } else {
            msg = Globalize.translate('DefaultErrorMessage');
        }

        Dashboard.alert({

            message: msg

        });
    }

    function onSubmit() {
        var form = this;

        submit(form);

        return false;
    }

    $(document).on('pageinit', "#wizardUserPage", function () {

        $('.wizardUserForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshow', "#wizardUserPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var apiClient = getApiClient();

        apiClient.getJSON(apiClient.getUrl('Startup/User')).then(function (user) {

            page.querySelector('#txtUsername').value = user.Name || '';
            page.querySelector('#txtConnectUserName').value = user.ConnectUserName || '';

            Dashboard.hideLoadingMsg();
        });
    });

});
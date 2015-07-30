(function ($, document, window) {

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
                ConnectUserName: page.querySelector('#txtConnectUserName').value

            },
            url: apiClient.getUrl('Startup/User'),
            dataType: 'json'

        }).done(onUpdateUserComplete);
    }

    function onSubmit() {
        var form = this;

        submit(form);

        return false;
    }

    $(document).on('pageinitdepends', "#wizardUserPage", function () {

        $('.wizardUserForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pageshowready', "#wizardUserPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var apiClient = getApiClient();

        apiClient.getJSON(apiClient.getUrl('Startup/User')).done(function (user) {

            page.querySelector('#txtUsername').value = user.Name;
            page.querySelector('#txtConnectUserName').value = user.ConnectUserName;

            Dashboard.hideLoadingMsg();
        });

    });

})(jQuery, document, window);
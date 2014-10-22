(function () {

    function onLoggedIn() {

        ConnectionManager.getServers().done(function (result) {

            if (result.length) {

                connectToServerInstance(result[0]);

            } else {
                Dashboard.alert('Coming soon');
            }
        });
    }

    function connectToServerInstance(server) {

        var url = server.Url;
        var exchangeToken = server.AccessKey;

        url += "/mediabrowser/Connect/Exchange?format=json&ConnectUserId=" + ConnectionManager.connectUserId();

        $.ajax({

            type: "GET",
            url: url,
            dataType: "json",

            error: function () {
                // Don't show normal dashboard errors
            },
            headers: {
                "X-MediaBrowser-Token": exchangeToken
            }


        }).done(function (result) {

            Dashboard.serverAddress(server.Url);
            Dashboard.setCurrentUser(result.LocalUserId, result.AccessToken);

            window.location = 'index.html';

        }).fail(function (result) {

            alert('Error talking to MBS');

        });
    }

    function login(page, username, password) {

        var md5 = CryptoJS.MD5(password).toString();

        $.ajax({
            type: "POST",
            url: "https://connect.mediabrowser.tv/service/user/authenticate",
            data: {
                userName: username,
                password: md5
            },
            dataType: "json",
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8',

            error: function () {
                // Don't show normal dashboard errors
            }


        }).done(function (result) {

            ConnectionManager.onConnectAuthenticated(result);

            onLoggedIn();

        }).fail(function (result) {

            Dashboard.alert({
                message: Globalize.translate('MessageInvalidUser'),
                title: Globalize.translate('HeaderLoginFailure')
            });

            $('#txtManualPassword', page).val('');

        });

    }

    function submit(page) {

        var user = $('#txtManualName', page).val();
        var password = $('#txtManualPassword', page).val();

        login(page, user, password);

    }

    window.ConnectLoginPage = {

        onSubmit: function () {

            var page = $(this).parents('.page');

            submit(page);

            return false;
        }
    };

})();
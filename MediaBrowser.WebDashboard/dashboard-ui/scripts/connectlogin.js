(function () {

    function onLoggedIn() {

        ConnectionManager.connect().done(function (result) {

            Dashboard.hideLoadingMsg();

            switch (result.State) {

                case MediaBrowser.ConnectionState.Unavilable:
                    // Login succeeded so this should never happen
                    break;
                case MediaBrowser.ConnectionState.ServerSelection:
                    window.location = 'selectserver.html';
                    break;
                case MediaBrowser.ConnectionState.ServerSignIn:
                    // This should never happen in connect mode
                    break;
                case MediaBrowser.ConnectionState.SignedIn:
                    window.location = 'selectserver.html';
                    break;
                default:
                    break;
            }
        });
    }

    function login(page, username, password) {

        Dashboard.showLoadingMsg();

        ConnectionManager.loginToConnect(username, password).done(function () {

            onLoggedIn();

        }).fail(function () {

            Dashboard.hideLoadingMsg();

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
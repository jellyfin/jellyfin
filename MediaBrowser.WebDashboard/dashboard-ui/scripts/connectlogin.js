(function () {

    function onLoggedIn() {

        Dashboard.navigate('selectserver.html');
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
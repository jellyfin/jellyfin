(function () {

    function login(page, username, password) {

        Dashboard.showModalLoadingMsg();

        ConnectionManager.loginToConnect(username, password).done(function () {

            Dashboard.hideModalLoadingMsg();
            Dashboard.navigate('selectserver.html');

        }).fail(function () {

            Dashboard.hideModalLoadingMsg();

            Dashboard.alert({
                message: Globalize.translate('MessageInvalidUser'),
                title: Globalize.translate('HeaderLoginFailure')
            });

            $('#txtManualPassword', page).val('');

        });

    }

    function handleConnectionResult(page, result) {

        Dashboard.hideModalLoadingMsg();

        switch (result.State) {

            case MediaBrowser.ConnectionState.SignedIn:
                {
                    var apiClient = result.ApiClient;

                    Dashboard.onServerChanged(apiClient.getCurrentUserId(), apiClient.accessToken(), apiClient);
                    Dashboard.navigate('index.html');
                }
                break;
            case MediaBrowser.ConnectionState.ServerSignIn:
                {
                    Dashboard.navigate('login.html?serverid=' + result.Servers[0].Id, false, 'none');
                }
                break;
            case MediaBrowser.ConnectionState.ServerSelection:
                {
                    Dashboard.navigate('selectserver.html', false, 'none');
                }
                break;
            case MediaBrowser.ConnectionState.ConnectSignIn:
                {
                    loadMode(page, 'welcome');
                }
                break;
            case MediaBrowser.ConnectionState.Unavailable:
                {
                    Dashboard.alert({
                        message: Globalize.translate("MessageUnableToConnectToServer"),
                        title: Globalize.translate("HeaderConnectionFailure")
                    });
                }
                break;
            default:
                break;
        }
    }

    function loadAppConnection(page) {

        Dashboard.showModalLoadingMsg();

        ConnectionManager.connect().done(function (result) {

            handleConnectionResult(page, result);

        });
    }

    function loadPage(page) {

        var mode = getParameterByName('mode') || 'auto';

        if (mode == 'auto') {

            if (AppInfo.isNativeApp) {
                loadAppConnection(page);
                return;
            }
            mode = 'connect';
        }

        loadMode(page, mode);
    }
    function loadMode(page, mode) {

        Backdrops.setDefault(page);

        if (mode == 'welcome') {
            $('.connectLoginForm', page).hide();
            $('.welcomeContainer', page).show();
            $('.manualServerForm', page).hide();
            $('.signupForm', page).hide();
        }
        else if (mode == 'connect') {
            $('.connectLoginForm', page).show();
            $('.welcomeContainer', page).hide();
            $('.manualServerForm', page).hide();
            $('.signupForm', page).hide();
        }
        else if (mode == 'manualserver') {
            $('.manualServerForm', page).show();
            $('.connectLoginForm', page).hide();
            $('.welcomeContainer', page).hide();
            $('.signupForm', page).hide();
        }
        else if (mode == 'signup') {
            $('.manualServerForm', page).hide();
            $('.connectLoginForm', page).hide();
            $('.welcomeContainer', page).hide();
            $('.signupForm', page).show();
        }
    }

    function skip() {

        Dashboard.navigate('selectserver.html');
    }

    function onSubmit() {
        var page = $(this).parents('.page');

        submit(page);

        return false;
    }

    function onManualServerSubmit() {
        var page = $(this).parents('.page');

        submitManualServer(page);

        return false;
    }

    function onSignupFormSubmit() {

        var page = $(this).parents('.page');

        ConnectionManager.signupForConnect($('#txtSignupEmail', page).val(), $('#txtSignupUsername', page).val(), $('#txtSignupPassword', page).val(), $('#txtSignupPasswordConfirm', page).val()).done(function () {

            Dashboard.alert({
                message: Globalize.translate('MessageThankYouForConnectSignUp'),
                callback: function () {
                    Dashboard.navigate('connectlogin.html?mode=welcome');
                }
            });

        }).fail(function (result) {

            if (result.errorCode == 'passwordmatch') {
                Dashboard.alert({
                    message: Globalize.translate('ErrorMessagePasswordNotMatchConfirm')
                });
            }
            else if (result.errorCode == 'USERNAME_IN_USE') {
                Dashboard.alert({
                    message: Globalize.translate('ErrorMessageUsernameInUse')
                });
            }
            else if (result.errorCode == 'EMAIL_IN_USE') {
                Dashboard.alert({
                    message: Globalize.translate('ErrorMessageEmailInUse')
                });
            } else {
                Dashboard.alert({
                    message: Globalize.translate('DefaultErrorMessage')
                });
            }

        });

        return false;
    }

    $(document).on('pageinitdepends', "#connectLoginPage", function () {

        var page = this;

        $('.btnSkipConnect', page).on('click', function () {
            skip();
        });

        $('.connectLoginForm').off('submit', onSubmit).on('submit', onSubmit);
        $('.manualServerForm').off('submit', onManualServerSubmit).on('submit', onManualServerSubmit);
        $('.signupForm').off('submit', onSignupFormSubmit).on('submit', onSignupFormSubmit);

    }).on('pagebeforeshowready', "#connectLoginPage", function () {

        var page = this;

        $('#txtSignupEmail', page).val('');
        $('#txtSignupUsername', page).val('');
        $('#txtSignupPassword', page).val('');
        $('#txtSignupPasswordConfirm', page).val('');

        if (AppInfo.isNativeApp) {
            $('.skip', page).show();
        } else {
            $('.skip', page).hide();
        }

        var link = '<a href="http://emby.media" target="_blank">http://emby.media</a>';
        $('.embyIntroDownloadMessage', page).html(Globalize.translate('EmbyIntroDownloadMessage', link));

    }).on('pageshowready', "#connectLoginPage", function () {

        var page = this;

        loadPage(page);
    });

    function submitManualServer(page) {

        var host = $('#txtServerHost', page).val();
        var port = $('#txtServerPort', page).val();

        if (port) {
            host += ':' + port;
        }

        Dashboard.showModalLoadingMsg();

        ConnectionManager.connectToAddress(host).done(function (result) {

            handleConnectionResult(page, result);

        }).fail(function () {

            handleConnectionResult(page, {
                State: MediaBrowser.ConnectionState.Unavailable
            });

        });
    }

    function submit(page) {

        var user = $('#txtManualName', page).val();
        var password = $('#txtManualPassword', page).val();

        login(page, user, password);
    }

})();
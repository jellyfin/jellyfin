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

    function handleConnectionResult(page, result) {

        switch (result.State) {

            case MediaBrowser.ConnectionState.SignedIn:
                {
                    var apiClient = result.ApiClient;

                    Dashboard.serverAddress(apiClient.serverAddress());
                    Dashboard.setCurrentUser(apiClient.getCurrentUserId(), apiClient.accessToken());
                    window.location = 'index.html';
                }
                break;
            case MediaBrowser.ConnectionState.ServerSignIn:
                {
                    window.location = 'connectlogin.html?mode=serversignin&serverid=' + result.Servers[0].Id;
                }
                break;
            case MediaBrowser.ConnectionState.ServerSelection:
                {
                    onLoggedIn();
                }
                break;
            case MediaBrowser.ConnectionState.ConnectSignIn:
                {
                    loadMode(page, 'welcome');
                }
                break;
            default:
                break;
        }
    }

    function loadAppConnection(page) {

        Dashboard.showLoadingMsg();

        ConnectionManager.connect().done(function (result) {

            Dashboard.hideLoadingMsg();

            handleConnectionResult(page, result);

        });
    }

    function loadPage(page) {

        var mode = getParameterByName('mode');

        if (!mode) {

            if (Dashboard.isRunningInCordova()) {
                loadAppConnection(page);
                return;
            }
            mode = 'connect';
        }

        loadMode(page, mode);
    }
    function loadMode(page, mode) {

        if (mode == 'welcome') {
            $('.connectLoginForm', page).hide();
            $('.welcomeContainer', page).show();
            $('.manualServerForm', page).hide();
        }
        else if (mode == 'connect') {
            $('.connectLoginForm', page).show();
            $('.welcomeContainer', page).hide();
            $('.manualServerForm', page).hide();
        }
        else if (mode == 'manualserver') {
            $('.manualServerForm', page).show();
            $('.connectLoginForm', page).hide();
            $('.welcomeContainer', page).hide();
        }
    }

    $(document).on('pageshow', "#connectLoginPage", function () {

        var page = this;

        loadPage(page);

        var link = '<a href="http://emby.media" target="_blank">http://emby.media</a>';
        $('.embyIntroDownloadMessage', page).html(Globalize.translate('EmbyIntroDownloadMessage', link));

        if (Dashboard.isRunningInCordova()) {
            $('.newUsers', page).hide();
            $('.forgotPassword', page).hide();
            $('.skip', page).show();
        } else {
            $('.skip', page).hide();
            $('.newUsers', page).show();
            $('.forgotPassword', page).show();
        }
    });

    function submitManualServer(page) {

        var host = $('#txtServerHost', page).val();
        var port = $('#txtServerPort', page).val();

        if (port) {
            host += ':' + port;
        }

        Dashboard.showLoadingMsg();

        ConnectionManager.connectToAddress(host).done(function (result) {

            Dashboard.hideLoadingMsg();

            handleConnectionResult(page, result);

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
        },

        onManualServerSubmit: function () {
            var page = $(this).parents('.page');

            submitManualServer(page);

            return false;

        }
    };

})();
define(['jQuery'], function ($) {

    function login(page, username, password) {

        Dashboard.showModalLoadingMsg();

        ConnectionManager.loginToConnect(username, password).then(function () {

            Dashboard.hideModalLoadingMsg();
            Dashboard.navigate('selectserver.html');

        }, function () {

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
                    Dashboard.navigate('home.html');
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
            case MediaBrowser.ConnectionState.ServerUpdateNeeded:
                {
                    Dashboard.alert({
                        message: Globalize.translate('ServerUpdateNeeded', '<a href="https://emby.media">https://emby.media</a>')
                    });
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

        ConnectionManager.connect().then(function (result) {

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
            initSignup(page);
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

        if (!supportInAppSignup()) {
            return false;
        }

        var page = $(this).parents('.page');

        ConnectionManager.signupForConnect($('#txtSignupEmail', page).val(), $('#txtSignupUsername', page).val(), $('#txtSignupPassword', page).val(), $('#txtSignupPasswordConfirm', page).val()).then(function () {

            Dashboard.alert({
                message: Globalize.translate('MessageThankYouForConnectSignUp'),
                callback: function () {
                    Dashboard.navigate('connectlogin.html?mode=welcome');
                }
            });

        }, function (result) {

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

    function requireCaptcha() {
        return !AppInfo.isNativeApp && window.location.href.toLowerCase().indexOf('https') == 0;
    }

    function supportInAppSignup() {
        return AppInfo.isNativeApp;
        return AppInfo.isNativeApp || window.location.href.toLowerCase().indexOf('https') == 0;
    }

    function initSignup(page) {

        if (!supportInAppSignup()) {
            return;
        }

        if (!requireCaptcha()) {
            return;
        }

        require(['https://www.google.com/recaptcha/api.js?onload=onloadCallback&render=explicit'], function () {

        });
    }

    $(document).on('pageinit', "#connectLoginPage", function () {

        var page = this;

        $('.btnSkipConnect', page).on('click', function () {
            skip();
        });

        $('.connectLoginForm').off('submit', onSubmit).on('submit', onSubmit);
        $('.manualServerForm').off('submit', onManualServerSubmit).on('submit', onManualServerSubmit);
        $('.signupForm').off('submit', onSignupFormSubmit).on('submit', onSignupFormSubmit);

        $('.btnSignupForConnect', page).on('click', function () {

            if (supportInAppSignup()) {
                Dashboard.navigate('connectlogin.html?mode=signup');
                return false;
            }
        });

        $('.btnCancelSignup', page).on('click', function () {

            history.back();
        });

        $('.btnCancelManualServer', page).on('click', function () {

            history.back();
        });

        $('.btnWelcomeNext', page).on('click', function () {
            Dashboard.navigate('connectlogin.html?mode=connect');
        });

        var terms = page.querySelector('.terms');
        terms.innerHTML = Globalize.translate('LoginDisclaimer') + "<div style='margin-top:5px;'><a href='http://emby.media/terms' target='_blank'>" + Globalize.translate('TermsOfUse') + "</a></div>";

        if (AppInfo.isNativeApp) {
            terms.classList.add('hide');
            page.querySelector('.tvAppInfo').classList.add('hide');
        } else {
            terms.classList.remove('hide');
            page.querySelector('.tvAppInfo').classList.remove('hide');
        }

    }).on('pagebeforeshow', "#connectLoginPage", function () {

        var page = this;

        $('#txtSignupEmail', page).val('');
        $('#txtSignupUsername', page).val('');
        $('#txtSignupPassword', page).val('');
        $('#txtSignupPasswordConfirm', page).val('');

        if (browserInfo.safari && AppInfo.isNativeApp) {
            // With apple we can't even have a link to the site
            $('.embyIntroDownloadMessage', page).html(Globalize.translate('EmbyIntroDownloadMessageWithoutLink'));
        } else {
            var link = '<a href="http://emby.media" target="_blank">http://emby.media</a>';
            $('.embyIntroDownloadMessage', page).html(Globalize.translate('EmbyIntroDownloadMessage', link));
        }

    }).on('pageshow', "#connectLoginPage", function () {

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

        ConnectionManager.connectToAddress(host).then(function (result) {

            handleConnectionResult(page, result);

        }, function () {
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

});
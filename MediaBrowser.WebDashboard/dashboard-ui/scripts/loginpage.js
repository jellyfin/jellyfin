define(['jQuery'], function ($) {

    var LoginPage = {

        showVisualForm: function (page) {
            $('.visualLoginForm', page).show();
            $('.manualLoginForm', page).hide();
        },

        getLastSeenText: function (lastActivityDate) {

            if (!lastActivityDate) {
                return "";
            }

            return "Last seen " + humane_date(lastActivityDate);
        },

        authenticateUserByName: function (page, apiClient, username, password) {

            Dashboard.showLoadingMsg();

            apiClient.authenticateUserByName(username, password).then(function (result) {

                var user = result.User;

                var serverId = getParameterByName('serverid');

                var newUrl;

                if (user.Policy.IsAdministrator && !serverId) {
                    newUrl = "dashboard.html";
                } else {
                    newUrl = "home.html";
                }

                Dashboard.hideLoadingMsg();

                Dashboard.onServerChanged(user.Id, result.AccessToken, apiClient);
                Dashboard.navigate(newUrl);

            }, function () {

                $('#pw', page).val('');
                $('#txtManualName', page).val('');
                $('#txtManualPassword', page).val('');

                Dashboard.hideLoadingMsg();

                setTimeout(function () {
                    require(['toast'], function (toast) {
                        toast(Globalize.translate('MessageInvalidUser'));
                    });
                }, 300);
            });

        }

    };

    function getApiClient() {

        var serverId = getParameterByName('serverid');

        if (serverId) {
            return Promise.resolve(ConnectionManager.getOrCreateApiClient(serverId));

        } else {
            return Promise.resolve(ApiClient);
        }
    }

    function onManualSubmit() {
        var page = $(this).parents('.page');

        getApiClient().then(function (apiClient) {
            LoginPage.authenticateUserByName(page, apiClient, $('#txtManualName', page).val(), $('#txtManualPassword', page).val());
        });

        // Disable default form submission
        return false;
    }

    function showManualForm(context, showCancel, focusPassword) {
        $('.visualLoginForm', context).hide();
        $('.manualLoginForm', context).show();

        if (focusPassword) {
            $('#txtManualPassword input', context).focus();
        } else {
            $('#txtManualName input', context).focus();
        }

        if (showCancel) {
            $('.btnCancel', context).show();
        } else {
            $('.btnCancel', context).hide();
        }
    }

    function loadUserList(context, apiClient, users) {
        var html = "";

        for (var i = 0, length = users.length; i < length; i++) {
            var user = users[i];

            html += '<div class="card squareCard bottomPaddedCard"><div class="cardBox visualCardBox">';

            html += '<div class="cardScalable">';

            html += '<div class="cardPadder"></div>';
            html += '<a class="cardContent" href="#" data-ajax="false" data-haspw="' + user.HasPassword + '" data-username="' + user.Name + '" data-userid="' + user.Id + '">';

            var imgUrl;

            if (user.PrimaryImageTag) {

                imgUrl = apiClient.getUserImageUrl(user.Id, {
                    width: 300,
                    tag: user.PrimaryImageTag,
                    type: "Primary"
                });

                html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');"></div>';
            }
            else {

                var background = LibraryBrowser.getMetroColor(user.Id);

                imgUrl = 'css/images/logindefault.png';

                html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');background-color:' + background + ';"></div>';
            }

            html += '</a>';
            html += '</div>';

            html += '<div class="cardFooter">';
            html += '<div class="cardText">' + user.Name + '</div>';

            html += '<div class="cardText">';
            var lastSeen = LoginPage.getLastSeenText(user.LastActivityDate);
            if (lastSeen != "") {
                html += lastSeen;
            }
            else {
                html += "&nbsp;";
            }
            html += '</div>';
            html += '</div>';
            html += '</div>';

            html += '</div>';
        }

        var elem = $('#divUsers', context).html(html);

        $('a', elem).on('click', function () {

            var id = this.getAttribute('data-userid');
            var name = this.getAttribute('data-username');
            var haspw = this.getAttribute('data-haspw');

            if (id == 'manual') {
                showManualForm(context, true);
            }
            else if (haspw == 'false') {
                LoginPage.authenticateUserByName(context, apiClient, name, '');
            } else {
                $('#txtManualName', context).val(name);
                $('#txtManualPassword', context).val('');
                showManualForm(context, true, true);
            }
        });
    }

    return function (view, params) {

        var self = this;

        $('.manualLoginForm', view).on('submit', onManualSubmit);

        view.querySelector('.btnForgotPassword').addEventListener('click', function () {
            Dashboard.navigate('forgotpassword.html');
        });

        view.querySelector('.btnCancel').addEventListener('click', function () {
            LoginPage.showVisualForm(view);
        });

        view.querySelector('.btnManual').addEventListener('click', function () {
            showManualForm(view, true);
        });

        view.addEventListener('viewshow', function (e) {
            Dashboard.showLoadingMsg();

            getApiClient().then(function (apiClient) {

                apiClient.getPublicUsers().then(function (users) {

                    var showManualForm = !users.length;

                    if (showManualForm) {

                        showManualForm(view, false, false);

                    } else {

                        LoginPage.showVisualForm(view);
                        loadUserList(view, apiClient, users);
                    }

                    Dashboard.hideLoadingMsg();
                });

                apiClient.getJSON(apiClient.getUrl('Branding/Configuration')).then(function (options) {

                    $('.disclaimer', view).html(options.LoginDisclaimer || '');
                });
            });

            if (Dashboard.isConnectMode()) {
                $('.connectButtons', view).show();
            } else {
                $('.connectButtons', view).hide();
            }
        });
    };
});
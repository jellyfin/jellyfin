var LoginPage = {

    getApiClient: function () {

        var serverId = getParameterByName('serverid');

        return new Promise(function (resolve, reject) {

            if (serverId) {
                resolve(ConnectionManager.getOrCreateApiClient(serverId));

            } else {
                resolve(ApiClient);
            }
        });
    },

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        LoginPage.getApiClient().then(function (apiClient) {

            apiClient.getPublicUsers().then(function (users) {

                var showManualForm = !users.length;

                if (showManualForm) {

                    LoginPage.showManualForm(page, false, false);

                } else {

                    LoginPage.showVisualForm(page);
                    LoginPage.loadUserList(page, apiClient, users);
                }

                Dashboard.hideLoadingMsg();
            });

            apiClient.getJSON(apiClient.getUrl('Branding/Configuration')).then(function (options) {

                $('.disclaimer', page).html(options.LoginDisclaimer || '');
            });
        });

        if (Dashboard.isConnectMode()) {
            $('.connectButtons', page).show();
        } else {
            $('.connectButtons', page).hide();
        }
    },

    cancelLogin: function () {

        LoginPage.showVisualForm($.mobile.activePage);
    },

    showManualForm: function (page, showCancel, focusPassword) {
        $('.visualLoginForm', page).hide();
        $('.manualLoginForm', page).show();

        if (focusPassword) {
            $('#txtManualPassword input', page).focus();
        } else {
            $('#txtManualName input', page).focus();
        }

        if (showCancel) {
            $('.btnCancel', page).show();
        } else {
            $('.btnCancel', page).hide();
        }
    },

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

    },

    loadUserList: function (page, apiClient, users) {
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

        var elem = $('#divUsers', page).html(html);

        $('a', elem).on('click', function () {

            var id = this.getAttribute('data-userid');
            var name = this.getAttribute('data-username');
            var haspw = this.getAttribute('data-haspw');

            if (id == 'manual') {
                LoginPage.showManualForm(page, true);
            }
            else if (haspw == 'false') {
                LoginPage.authenticateUserByName(page, apiClient, name, '');
            } else {
                $('#txtManualName', page).val(name);
                $('#txtManualPassword', page).val('');
                LoginPage.showManualForm(page, true, true);
            }
        });
    },

    onManualSubmit: function () {

    	var page = $(this).parents('.page');

        LoginPage.getApiClient().then(function (apiClient) {
            LoginPage.authenticateUserByName(page, apiClient, $('#txtManualName', page).val(), $('#txtManualPassword', page).val());
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageinit', "#loginPage", function () {

    var page = this;

    $('.manualLoginForm', page).off('submit', LoginPage.onManualSubmit).on('submit', LoginPage.onManualSubmit);

    $('.btnForgotPassword', page).on('click', function () {
        Dashboard.navigate('forgotpassword.html');
    });

}).on('pageshow', "#loginPage", LoginPage.onPageShow);

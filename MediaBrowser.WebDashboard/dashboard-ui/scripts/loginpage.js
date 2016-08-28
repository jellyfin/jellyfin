define(['appSettings', 'cardStyle', 'emby-checkbox'], function (appSettings) {

    function getApiClient() {

        var serverId = getParameterByName('serverid');

        if (serverId) {
            return ConnectionManager.getOrCreateApiClient(serverId);

        } else {
            return ApiClient;
        }
    }

    var LoginPage = {

        showVisualForm: function (page) {

            page.querySelector('.visualLoginForm').classList.remove('hide');
            page.querySelector('.manualLoginForm').classList.add('hide');
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

            }, function (response) {

                page.querySelector('#txtManualName').value = '';
                page.querySelector('#txtManualPassword').value = '';

                Dashboard.hideLoadingMsg();

                if (response.status == 401) {
                    require(['toast'], function (toast) {
                        toast(Globalize.translate('MessageInvalidUser'));
                    });
                } else {
                    showServerConnectionFailure();
                }
            });

        }

    };

    function showServerConnectionFailure() {

        Dashboard.alert({
            message: Globalize.translate("MessageUnableToConnectToServer"),
            title: Globalize.translate("HeaderConnectionFailure")
        });
    }

    function showManualForm(context, showCancel, focusPassword) {

        context.querySelector('.chkRememberLogin').checked = appSettings.enableAutoLogin();

        context.querySelector('.manualLoginForm').classList.remove('hide');
        context.querySelector('.visualLoginForm').classList.add('hide');

        if (focusPassword) {
            context.querySelector('#txtManualPassword').focus();
        } else {
            context.querySelector('#txtManualName').focus();
        }

        if (showCancel) {
            context.querySelector('.btnCancel').classList.remove('hide');
        } else {
            context.querySelector('.btnCancel').classList.add('hide');
        }
    }

    var metroColors = ["#6FBD45", "#4BB3DD", "#4164A5", "#E12026", "#800080", "#E1B222", "#008040", "#0094FF", "#FF00C7", "#FF870F", "#7F0037"];

    function getRandomMetroColor() {

        var index = Math.floor(Math.random() * (metroColors.length - 1));

        return metroColors[index];
    }

    function getMetroColor(str) {

        if (str) {
            var character = String(str.substr(0, 1).charCodeAt());
            var sum = 0;
            for (var i = 0; i < character.length; i++) {
                sum += parseInt(character.charAt(i));
            }
            var index = String(sum).substr(-1);

            return metroColors[index];
        } else {
            return getRandomMetroColor();
        }
    }

    function loadUserList(context, apiClient, users) {
        var html = "";

        for (var i = 0, length = users.length; i < length; i++) {
            var user = users[i];

            html += '<div class="card squareCard scalableCard squareCard-scalable"><div class="cardBox cardBox-bottompadded visualCardBox">';

            html += '<div class="cardScalable visualCardBox-cardScalable">';

            html += '<div class="cardPadder cardPadder-square"></div>';
            html += '<a class="cardContent" href="#" data-ajax="false" data-haspw="' + user.HasPassword + '" data-username="' + user.Name + '" data-userid="' + user.Id + '">';

            var imgUrl;

            if (user.PrimaryImageTag) {

                imgUrl = apiClient.getUserImageUrl(user.Id, {
                    width: 300,
                    tag: user.PrimaryImageTag,
                    type: "Primary"
                });

                html += '<div class="cardImageContainer coveredImage coveredImage-noScale" style="background-image:url(\'' + imgUrl + '\');"></div>';
            }
            else {

                var background = getMetroColor(user.Id);

                imgUrl = 'css/images/logindefault.png';

                html += '<div class="cardImageContainer coveredImage coveredImage-noScale" style="background-image:url(\'' + imgUrl + '\');background-color:' + background + ';"></div>';
            }

            html += '</a>';
            html += '</div>';

            html += '<div class="cardFooter visualCardBox-cardFooter">';
            html += '<div class="cardText">' + user.Name + '</div>';

            html += '<div class="cardText cardText-secondary">';
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

        context.querySelector('#divUsers').innerHTML = html;
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    return function (view, params) {

        var self = this;

        view.querySelector('#divUsers').addEventListener('click', function (e) {
            var cardContent = parentWithClass(e.target, 'cardContent');

            if (cardContent) {

                var context = view;
                var id = cardContent.getAttribute('data-userid');
                var name = cardContent.getAttribute('data-username');
                var haspw = cardContent.getAttribute('data-haspw');

                if (id == 'manual') {
                    context.querySelector('#txtManualName').value = '';
                    showManualForm(context, true);
                }
                else if (haspw == 'false') {
                    LoginPage.authenticateUserByName(context, getApiClient(), name, '');
                } else {

                    context.querySelector('#txtManualName').value = name;
                    context.querySelector('#txtManualPassword').value = '';
                    showManualForm(context, true, true);
                }
            }
        });

        view.querySelector('.manualLoginForm').addEventListener('submit', function (e) {

            appSettings.enableAutoLogin(view.querySelector('.chkRememberLogin').checked);

            var apiClient = getApiClient();
            LoginPage.authenticateUserByName(view, apiClient, view.querySelector('#txtManualName').value, view.querySelector('#txtManualPassword').value);

            e.preventDefault();
            // Disable default form submission
            return false;
        });

        view.querySelector('.btnForgotPassword').addEventListener('click', function () {
            Dashboard.navigate('forgotpassword.html');
        });

        view.querySelector('.btnCancel').addEventListener('click', function () {
            LoginPage.showVisualForm(view);
        });

        view.querySelector('.btnManual').addEventListener('click', function () {
            view.querySelector('#txtManualName').value = '';
            showManualForm(view, true);
        });

        view.addEventListener('viewshow', function (e) {
            Dashboard.showLoadingMsg();

            var apiClient = getApiClient();
            apiClient.getPublicUsers().then(function (users) {

                if (!users.length) {

                    view.querySelector('#txtManualName').value = '';
                    showManualForm(view, false, false);

                } else {

                    LoginPage.showVisualForm(view);
                    loadUserList(view, apiClient, users);
                }

                Dashboard.hideLoadingMsg();
            });

            apiClient.getJSON(apiClient.getUrl('Branding/Configuration')).then(function (options) {

                view.querySelector('.disclaimer').innerHTML = options.LoginDisclaimer || '';
            });

            if (Dashboard.isConnectMode()) {
                view.querySelector('.connectButtons').classList.remove('hide');
            } else {
                view.querySelector('.connectButtons').classList.add('hide');
            }
        });
    };
});
var LoginPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        if (LoginPage.isLocalhost()) {
            $('.localhostMessage', page).show();
        } else {
            $('.localhostMessage', page).hide();
        }

        // Show all users on localhost
        var promise1 = ApiClient.getPublicUsers();

        promise1.done(function (users) {

            var showManualForm = !users.length;

            if (showManualForm) {

                LoginPage.showManualForm(page, false);

            } else {
                LoginPage.showVisualForm(page);
                LoginPage.loadUserList(users);
            }

            Dashboard.hideLoadingMsg();
        });

        ApiClient.getJSON(ApiClient.getUrl('Branding/Configuration')).done(function (options) {

            $('.disclaimer', page).html(options.LoginDisclaimer || '');
        });
    },

    isLocalhost: function () {

        var location = window.location.toString().toLowerCase();
        return location.indexOf('localhost') != -1 || location.indexOf('127.0.0.1') != -1;
    },
    
    cancelLogin: function() {

        LoginPage.showVisualForm($.mobile.activePage);
    },

    showManualForm: function (page, showCancel) {
        $('.visualLoginForm', page).hide();
        $('#manualLoginForm', page).show();
        $('#txtManualName', page).focus();
        
        if (showCancel) {
            $('.btnCancel', page).show();
        } else {
            $('.btnCancel', page).hide();
        }
    },

    showVisualForm: function (page) {
        $('.visualLoginForm', page).show();
        $('#manualLoginForm', page).hide();
    },

    getLastSeenText: function (lastActivityDate) {

        if (!lastActivityDate) {
            return "";
        }

        return "Last seen " + humane_date(lastActivityDate);
    },

    getImagePath: function (user) {

        if (!user.PrimaryImageTag) {
            return "css/images/logindefault.png";
        }

        return ApiClient.getUserImageUrl(user.Id, {
            width: 240,
            tag: user.PrimaryImageTag,
            type: "Primary"
        });
    },

    authenticateUserByName: function (username, password) {

        Dashboard.showLoadingMsg();

        ApiClient.authenticateUserByName(username, password).done(function (result) {

            var user = result.User;

            Dashboard.setCurrentUser(user.Id, result.AccessToken);

            if (user.Configuration.IsAdministrator) {
                window.location = "dashboard.html?u=" + user.Id + '&t=' + result.AccessToken;
            } else {
                window.location = "index.html?u=" + user.Id + '&t=' + result.AccessToken;
            }

        }).fail(function () {

            $('#pw', '#loginPage').val('');
            $('#txtManualName', '#loginPage').val('');
            $('#txtManualPassword', '#loginPage').val('');

            Dashboard.hideLoadingMsg();

            setTimeout(function () {
                Dashboard.showError(Globalize.translate('MessageInvalidUser'));
            }, 300);
        });

    },

    loadUserList: function (users) {
        var html = "";

        var page = $.mobile.activePage;

        for (var i = 0, length = users.length; i < length; i++) {
            var user = users[i];

            html += '<div class="card card-1-1"><div class="cardBox visualCardBox">';

            html += '<div class="cardScalable">';

            html += '<div class="cardPadder"></div>';
            html += '<a class="cardContent" href="#" data-ajax="false" data-haspw="' + user.HasPassword + '" data-username="' + user.Name + '" data-userid="' + user.Id + '">';

            var imgUrl;

            if (user.PrimaryImageTag) {

                 imgUrl = ApiClient.getUserImageUrl(user.Id, {
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

        var elem = $('#divUsers', '#loginPage').html(html);

        $('a', elem).on('click', function () {

            var name = this.getAttribute('data-username');
            var haspw = this.getAttribute('data-haspw');

            if (LoginPage.isLocalhost() || haspw == 'false') {
                LoginPage.authenticateUserByName(name, '');
            } else {
                $('#txtManualName', page).val(name);
                $('#txtManualPassword', '#loginPage').val('');
                LoginPage.showManualForm(page, true);
            }
        });
    },

    onManualSubmit: function () {

        LoginPage.authenticateUserByName($('#txtManualName', '#loginPage').val(), $('#txtManualPassword', '#loginPage').val());

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#loginPage", LoginPage.onPageShow);

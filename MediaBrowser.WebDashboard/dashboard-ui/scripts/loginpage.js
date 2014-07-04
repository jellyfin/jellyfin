var LoginPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var location = window.location.toString().toLowerCase();
        var isLocalhost = location.indexOf('localhost') != -1 || location.indexOf('127.0.0.1') != -1;

        if (isLocalhost) {
            $('.localhostMessage', page).show();
        } else {
            $('.localhostMessage', page).hide();
        }

        // Show all users on localhost
        var promise1 = !isLocalhost ? ApiClient.getPublicUsers() : ApiClient.getUsers({ IsDisabled: false });

        promise1.done(function (users) {

            var showManualForm = !users.length || !isLocalhost;

            if (showManualForm) {

                LoginPage.showManualForm(page);

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

    showManualForm: function (page) {
        $('.visualLoginForm', page).hide();
        $('#manualLoginForm', page).show();
        $('#txtManualName', page).focus();
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

    authenticateUserLink: function (link) {

        LoginPage.authenticateUser(link.getAttribute('data-userid'));
    },

    authenticateUser: function (userId, password) {

        Dashboard.showLoadingMsg();

        ApiClient.getUser(userId).done(function (user) {

            LoginPage.authenticateUserByName(user.Name, password);
        });

    },

    authenticateUserByName: function (username, password) {

        Dashboard.showLoadingMsg();

        ApiClient.authenticateUserByName(username, password).done(function (result) {

            var user = result.User;

            Dashboard.setCurrentUser(user.Id);

            if (user.Configuration.IsAdministrator) {
                window.location = "dashboard.html?u=" + user.Id;
            } else {
                window.location = "index.html?u=" + user.Id;
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

        for (var i = 0, length = users.length; i < length; i++) {
            var user = users[i];

            var linkId = "lnkUser" + i;

            html += "<a class='posterItem squarePosterItem' id='" + linkId + "' data-userid='" + user.Id + "' href='index.html?u=" + user.Id + "' data-ajax='false' \">";

            if (user.PrimaryImageTag) {

                var imgUrl = ApiClient.getUserImageUrl(user.Id, {
                    width: 500,
                    tag: user.PrimaryImageTag,
                    type: "Primary"
                });

                html += '<div class="posterItemImage" style="background-image:url(\'' + imgUrl + '\');"></div>';
            }
            else {

                var background = LibraryBrowser.getMetroColor(user.Id);

                html += '<div class="posterItemImage" style="background-color:' + background + ';"></div>';
            }

            html += '<div class="posterItemText" style="color:#000;">' + user.Name + '</div>';

            html += '<div class="posterItemText" style="color:#000;">';
            var lastSeen = LoginPage.getLastSeenText(user.LastActivityDate);
            if (lastSeen != "") {
                html += lastSeen;
            }
            else {
                html += "&nbsp;";
            }
            html += '</div>';

            html += '</a>';
        }

        $('#divUsers', '#loginPage').html(html);

    },

    onManualSubmit: function () {

        LoginPage.authenticateUserByName($('#txtManualName', '#loginPage').val(), $('#txtManualPassword', '#loginPage').val());

        // Disable default form submission
        return false;
    }
};

$(document).on('pagebeforeshow', "#loginPage", LoginPage.onPageShow);

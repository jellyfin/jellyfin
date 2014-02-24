var LoginPage = {

    onPageInit: function () {

        var page = this;

        $("#popupLogin", page).popup({
            afteropen: function (event, ui) {
                $('#pw').focus();
            }
        });
    },

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        var isLocalhost = window.location.toString().toLowerCase().indexOf('localhost') != -1;

        if (isLocalhost) {
            $('.localhostMessage', page).show();
        } else {
            $('.localhostMessage', page).hide();
        }

        // Show all users on localhost
        var promise1 = !isLocalhost ? ApiClient.getPublicUsers() : ApiClient.getUsers({ IsDisabled: false });
        var promise2 = ApiClient.getServerConfiguration();

        $.when(promise1, promise2).done(function (response1, response2) {

            var users = response1[0];
            var config = response2[0];

            var showManualForm = config.ManualLoginClients.filter(function (i) {

                return i == "Mobile";

            }).length || !users.length;

            if (showManualForm) {

                $('.visualLoginForm', page).hide();
                $('#manualLoginForm', page).show();
                $('#txtManualName', page).focus();

            } else {

                $('.visualLoginForm', page).show();
                $('#manualLoginForm', page).hide();

                LoginPage.loadUserList(users);
            }

            Dashboard.hideLoadingMsg();
        });
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
                Dashboard.showError("Invalid user or password.");
            }, 300);
        });

    },

    loadUserList: function (users) {
        var html = "";

        var isLocalhost = window.location.toString().toLowerCase().indexOf('localhost') != -1;

        for (var i = 0, length = users.length; i < length; i++) {
            var user = users[i];

            var linkId = "lnkUser" + i;

            if (isLocalhost) {
                html += "<a class='posterItem squarePosterItem' id='" + linkId + "' data-userid='" + user.Id + "' href='index.html?u=" + user.Id + "' data-ajax='false' \">";
            }
            else if (user.HasPassword) {
                html += "<a class='posterItem squarePosterItem' id='" + linkId + "' data-userid='" + user.Id + "' href='#popupLogin' data-rel='popup' onclick='LoginPage.authenticatingLinkId=this.id;' \">";
            } else {
                html += "<a class='posterItem squarePosterItem' id='" + linkId + "' data-userid='" + user.Id + "' href='#' onclick='LoginPage.authenticateUserLink(this);' \">";
            }

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

    onSubmit: function () {

        $('#popupLogin', '#loginPage').popup('close');

        var link = $('#' + LoginPage.authenticatingLinkId)[0];

        LoginPage.authenticateUser(link.getAttribute('data-userid'), $('#pw', '#loginPage').val());

        // Disable default form submission
        return false;
    },

    onManualSubmit: function () {

        LoginPage.authenticateUserByName($('#txtManualName', '#loginPage').val(), $('#txtManualPassword', '#loginPage').val());

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#loginPage", LoginPage.onPageShow).on('pageinit', "#loginPage", LoginPage.onPageInit);

var LoginPage = {

    onPageShow: function () {
        Dashboard.showLoadingMsg();

        ApiClient.getUsers().done(LoginPage.loadUserList);
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

        LoginPage.authenticateUser(link.getAttribute('data-username'), link.getAttribute('data-userid'));
    },

    authenticateUser: function (username, userId, password) {

        Dashboard.showLoadingMsg();

        ApiClient.authenticateUser(userId, password).done(function () {

            Dashboard.setCurrentUser(userId);

            window.location = "index.html?u=" + userId;

        }).fail(function () {
            Dashboard.hideLoadingMsg();

            setTimeout(function () {
                Dashboard.showError("Invalid user or password.");
            }, 300);
        });
    },

    loadUserList: function (users) {
        var html = "";

        for (var i = 0, length = users.length; i < length; i++) {
            var user = users[i];

            var linkId = "lnkUser" + i;

            if (user.HasPassword) {
                html += "<a class='posterItem squarePosterItem' id='" + linkId + "' data-userid='" + user.Id + "' data-username='" + user.Name + "' href='#popupLogin' data-rel='popup' onclick='LoginPage.authenticatingLinkId=this.id;' \">";
            } else {
                html += "<a class='posterItem squarePosterItem' id='" + linkId + "' data-userid='" + user.Id + "' data-username='" + user.Name + "' href='#' onclick='LoginPage.authenticateUserLink(this);' \">";
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

            html += '<div class="posterItemText">' + user.Name + '</div>';

            html += '<div class="posterItemText">';
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

        Dashboard.hideLoadingMsg();
    },

    onSubmit: function () {
        $('#popupLogin', '#loginPage').popup('close');

        var link = $('#' + LoginPage.authenticatingLinkId)[0];

        LoginPage.authenticateUser(link.getAttribute('data-username'), link.getAttribute('data-userid'), $('#pw', '#loginPage').val());

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#loginPage", LoginPage.onPageShow);

var LoginPage = {

    onPageShow: function () {
        Dashboard.showLoadingMsg();

        ApiClient.getAllUsers().done(LoginPage.loadUserList);
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
            tag: user.PrimaryImageTag
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

            var background = Dashboard.getRandomMetroColor();

            html += '<div class="posterViewItem">';

            if (user.HasPassword) {
                html += "<a id='" + linkId + "' data-userid='" + user.Id + "' data-username='" + user.Name + "' href='#popupLogin' data-rel='popup' onclick='LoginPage.authenticatingLinkId=this.id;' \">";
            } else {
                html += "<a id='" + linkId + "' data-userid='" + user.Id + "' data-username='" + user.Name + "' href='#' onclick='LoginPage.authenticateUserLink(this);' \">";
            }

            if (user.PrimaryImageTag) {

                var imgUrl = ApiClient.getUserImageUrl(user.Id, {
                    width: 500,
                    tag: user.PrimaryImageTag
                });

                html += '<img src="' + imgUrl + '" />';
            } else {
                html += '<img style="background:' + background + ';" src="css/images/logindefault.png"/>';
            }

            html += '<div class="posterViewItemText">';

            html += '<div>' + user.Name + '</div>';
            html += '<div>';
            var lastSeen = LoginPage.getLastSeenText(user.LastActivityDate);
            if (lastSeen != "") {
                html += lastSeen;
            }
            else {
                html += "&nbsp;";
            }
            html += '</div>';

            html += '</div>';
            html += '</a>';

            html += '</div>';
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

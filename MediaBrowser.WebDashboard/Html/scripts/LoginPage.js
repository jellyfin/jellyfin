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

            var background = Dashboard.getRandomMetroColor();

            if (user.HasPassword) {
                html += "<a id='" + linkId + "' data-userid='" + user.Id + "' data-username='" + user.Name + "' href='#popupLogin' data-rel='popup' onclick='LoginPage.authenticatingLinkId=this.id;' class='userItem'>";
            } else {
                html += "<a id='" + linkId + "' data-userid='" + user.Id + "' data-username='" + user.Name + "' href='#' onclick='LoginPage.authenticateUserLink(this);' class='userItem'>";
            }

            if (user.PrimaryImageTag) {

                var imgUrl = ApiClient.getUserImageUrl(user.Id, {
                    width: 500,
                    tag: user.PrimaryImageTag,
                    type: "Primary"
                });

                html += '<img class="userItemImage" src="' + imgUrl + '" />';
            } else {
                html += '<img class="userItemImage" src="css/images/logindefault.png" style="background:' + background + ';" />';
            }

            html += '<div class="userItemContent" style="background:' + background + ';">';

            html += '<div class="userItemContentInner">';
            html += '<p class="userItemHeader">' + user.Name + '</p>';
            html += '<p>' + LoginPage.getLastSeenText(user.LastActivityDate) + '</p>';
            html += '</div>';

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

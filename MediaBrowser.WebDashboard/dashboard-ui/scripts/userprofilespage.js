var UserProfilesPage = {
    onPageShow: function () {

        UserProfilesPage.loadPageData();
    },

    loadPageData: function () {

        Dashboard.showLoadingMsg();
        ApiClient.getUsers().done(UserProfilesPage.renderUsers);
    },

    renderUsers: function (users) {

        var html = "";

        html += '<li data-role="list-divider"><h3>' + Globalize.translate("Users") + '</h3></li>';

        for (var i = 0, length = users.length; i < length; i++) {

            var user = users[i];

            html += "<li>";

            html += "<a href='useredit.html?userId=" + user.Id + "'>";

            if (user.PrimaryImageTag) {

                var url = ApiClient.getUserImageUrl(user.Id, {
                    width: 225,
                    tag: user.PrimaryImageTag,
                    type: "Primary"
                });
                html += "<img src='" + url + "' />";
            } else {
                html += "<img src='css/images/userflyoutdefault.png' />";
            }

            html += "<h3>" + user.Name;

            html += "</h3>";

            html += "<p class='ui-li-aside'>";
            if (user.Configuration.HasPassword) html += '<img src="css/images/userdata/password.png" alt="' + Globalize.translate("Password") + '" title="' + Globalize.translate("Password") + '" class="userProfileIcon" />';
            if (user.Configuration.IsAdministrator) html += '<img src="css/images/userdata/administrator.png" alt="' + Globalize.translate("Administrator") + '" title="' + Globalize.translate("Administrator") + '" class="userProfileIcon" />';

            html += "</p>";

            html += "</a>";


            html += "<a onclick='UserProfilesPage.deleteUser(this);' data-userid='" + user.Id + "' data-username='" + user.Name + "' href='#'>" + Globalize.translate("Delete") + "</a>";

            html += "</li>";
        }

        $('#ulUserProfiles', $('#userProfilesPage')).html(html).listview('refresh');

        Dashboard.hideLoadingMsg();
    },

    deleteUser: function (link) {

        var page = $.mobile.activePage;
        var name = link.getAttribute('data-username');

        var msg = Globalize.translate("DeleteUserConfirmation").replace('{0}', name);

        Dashboard.confirm(msg, Globalize.translate("DeleteUser"), function (result) {

            if (result) {
                Dashboard.showLoadingMsg();

                var id = link.getAttribute('data-userid');

                ApiClient.deleteUser(id).done(function () {

                    Dashboard.validateCurrentUser(page);
                    UserProfilesPage.loadPageData();
                });
            }
        });
    }
};

$(document).on('pageshow', "#userProfilesPage", UserProfilesPage.onPageShow);

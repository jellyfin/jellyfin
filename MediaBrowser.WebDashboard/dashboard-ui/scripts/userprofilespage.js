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

        html += '<li data-role="list-divider"><h3>Users</h3></li>';

        for (var i = 0, length = users.length; i < length; i++) {

            var user = users[i];

            html += "<li>";

            html += "<a onclick='Dashboard.navigate(\"edituser.html?userId=" + user.Id + "\");' href='#'>";

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

            html += "<h3>" + user.Name + "</h3>";

            html += "</a>";

            html += "<a onclick='UserProfilesPage.deleteUser(this);' data-userid='" + user.Id + "' data-username='" + user.Name + "' href='#'>Delete</a>";

            html += "</li>";
        }

        $('#ulUserProfiles', $('#userProfilesPage')).html(html).listview('refresh');

        Dashboard.hideLoadingMsg();
    },

    deleteUser: function (link) {

        var name = link.getAttribute('data-username');

        var msg = "Are you sure you wish to delete " + name + "?";

        Dashboard.confirm(msg, "Delete User", function (result) {

            if (result) {
                Dashboard.showLoadingMsg();

                var id = link.getAttribute('data-userid');

                ApiClient.deleteUser(id).done(function () {

                    Dashboard.validateCurrentUser();
                    UserProfilesPage.loadPageData();
                });
            }
        });
    }
};

$(document).on('pageshow', "#userProfilesPage", UserProfilesPage.onPageShow);

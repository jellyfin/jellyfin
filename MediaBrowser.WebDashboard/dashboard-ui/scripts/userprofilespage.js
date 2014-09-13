(function (document, window, $) {

    function deleteUser(page, id, name) {

        var msg = Globalize.translate('DeleteUserConfirmation').replace('{0}', name);

        Dashboard.confirm(msg, Globalize.translate('DeleteUser'), function (result) {

            if (result) {
                Dashboard.showLoadingMsg();

                ApiClient.deleteUser(id).done(function () {

                    loadUsers(page);
                });
            }
        });
    }

    function getUserHtml(user) {

        var html = '';

        html += "<div class='card homePageSquareCard alternateHover bottomPaddedCard'>";

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        var href = "useredit.html?userId=" + user.Id + "";
        html += '<a class="cardContent" href="' + href + '">';

        var imgUrl;

        if (user.PrimaryImageTag) {

            imgUrl = ApiClient.getUserImageUrl(user.Id, {
                width: 200,
                tag: user.PrimaryImageTag,
                type: "Primary"
            });

        } else {
            imgUrl = 'css/images/userflyoutdefault.png';
        }

        html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');">';

        //if (plugin.isPremium) {
        //    if (plugin.price > 0) {
        //        html += "<div class='premiumBanner'><img src='css/images/supporter/premiumflag.png' /></div>";
        //    } else {
        //        html += "<div class='premiumBanner'><img src='css/images/supporter/supporterflag.png' /></div>";
        //    }
        //}
        html += "</div>";

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter">';

        html += '<div class="cardText" style="text-align:right; float:right;">';

        html += '<button type="button" data-inline="true" data-iconpos="notext" data-icon="ellipsis-v" style="margin: 2px 0 0;"></button>';
        html += "</div>";

        html += '<div class="cardText" style="margin-right: 30px; padding: 11px 0 10px;">';
        html += user.Name;
        html += "</div>";

        // cardFooter
        html += "</div>";

        // cardBox
        html += "</div>";

        // card
        html += "</div>";



        //html += "<li>";

        //html += "<a href='useredit.html?userId=" + user.Id + "'>";

        //if (user.PrimaryImageTag) {

        //    var url = ApiClient.getUserImageUrl(user.Id, {
        //        width: 80,
        //        tag: user.PrimaryImageTag,
        //        type: "Primary"
        //    });
        //    html += "<img src='" + url + "' />";
        //} else {
        //    html += "<img src='css/images/userflyoutdefault.png' />";
        //}

        //html += "<h3>" + user.Name;

        //html += "</h3>";

        //html += "<p class='ui-li-aside'>";
        //if (user.HasConfiguredPassword) html += '<img src="css/images/userdata/password.png" alt="' + Globalize.translate('Password') + '" title="' + Globalize.translate('Password') + '" class="userProfileIcon" />';
        //if (user.Configuration.IsAdministrator) html += '<img src="css/images/userdata/administrator.png" alt="' + Globalize.translate('Administrator') + '" title="' + Globalize.translate('Administrator') + '" class="userProfileIcon" />';

        //html += "</p>";

        //html += "</a>";


        //html += "<a onclick='UserProfilesPage.deleteUser(this);' data-userid='" + user.Id + "' data-username='" + user.Name + "' href='#'>" + Globalize.translate('Delete') + "</a>";

        //html += "</li>";

        return html;
    }

    function loadUsers(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getUsers().done(function (users) {

            var html = users.map(getUserHtml).join('');

            $('.users', page).html(html).trigger('create');

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pagebeforeshow', "#userProfilesPage", function () {

        var page = this;

        loadUsers(page);
    });


})(document, window, jQuery);
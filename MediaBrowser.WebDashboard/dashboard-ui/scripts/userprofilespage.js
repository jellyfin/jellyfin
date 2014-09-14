(function (document, window, $) {

    function deleteUser(page, id) {

        $('.userMenu', page).on("popupafterclose.deleteuser", function() {

            $(this).off('popupafterclose.deleteuser');

            var msg = Globalize.translate('DeleteUserConfirmation');

            Dashboard.confirm(msg, Globalize.translate('DeleteUser'), function (result) {

                if (result) {
                    Dashboard.showLoadingMsg();

                    ApiClient.deleteUser(id).done(function () {

                        loadUsers(page);
                    });
                }
            });

        }).popup('close');
    }

    function closeUserMenu(page) {
        $('.userMenu', page).popup('close').remove();
    }

    function showUserMenu(elem) {

        var card = $(elem).parents('.card');
        var page = $(elem).parents('.page');
        var userId = card.attr('data-userid');

        $('.userMenu', page).popup("close").remove();

        var html = '<div data-role="popup" class="userMenu" data-history="false" data-theme="a">';

        html += '<ul data-role="listview" style="min-width: 180px;">';
        html += '<li data-role="list-divider">Menu</li>';

        html += '<li><a href="#" class="btnDeleteUser" data-userid="' + userId + '">' + Globalize.translate('ButtonDelete') + '</a></li>';

        html += '<li><a href="useredit.html?userid=' + userId + '">' + Globalize.translate('ButtonOpen') + '</a></li>';

        html += '</ul>';

        html += '</div>';

        page.append(html);

        var flyout = $('.userMenu', page).popup({ positionTo: elem || "window" }).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();

        });

        $('.btnDeleteUser', flyout).on('click', function () {
            deleteUser(page, this);
        });
    }

    function getUserHtml(user) {

        var html = '';

        var cssClass = "card homePageSquareCard alternateHover bottomPaddedCard";

        if (user.Configuration.IsDisabled) {
            cssClass += ' grayscale';
        }

        html += "<div data-userid='" + user.Id + "' class='" + cssClass + "'>";

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        var href = "useredit.html?userId=" + user.Id + "";
        html += '<a class="cardContent" href="' + href + '">';

        var imgUrl;

        if (user.PrimaryImageTag) {

            imgUrl = ApiClient.getUserImageUrl(user.Id, {
                width: 300,
                tag: user.PrimaryImageTag,
                type: "Primary"
            });

        } else {
            imgUrl = 'css/images/userflyoutdefault.png';
        }

        html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');">';

        if (user.ConnectUserId) {
            html += '<div class="playedIndicator"><div class="ui-icon-cloud ui-btn-icon-notext"></div></div>';
        }

        html += "</div>";

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter">';

        html += '<div class="cardText" style="text-align:right; float:right;">';

        html += '<button class="btnUserMenu" type="button" data-inline="true" data-iconpos="notext" data-icon="ellipsis-v" style="margin: 2px 0 0;"></button>';
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

        return html;
    }

    function getUserSectionHtml(users) {

        var html = '';

        html += users.map(getUserHtml).join('');

        return html;
    }

    function renderUsers(page, users) {

        var html = '';

        html += getUserSectionHtml(users);

        var elem = $('.users', page).html(html).trigger('create');

        $('.btnUserMenu', elem).on('click', function () {
            showUserMenu(this);
        });
    }

    function loadUsers(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getUsers().done(function (users) {
            renderUsers(page, users);
            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pagebeforeshow', "#userProfilesPage", function () {

        var page = this;

        loadUsers(page);
    });


})(document, window, jQuery);
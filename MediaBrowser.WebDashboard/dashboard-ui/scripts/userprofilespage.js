define(['jQuery', 'paper-icon-button-light', 'cardStyle'], function ($) {

    function deleteUser(page, id) {

        var msg = Globalize.translate('DeleteUserConfirmation');

        require(['confirm'], function (confirm) {

            confirm(msg, Globalize.translate('DeleteUser')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.deleteUser(id).then(function () {

                    loadData(page);
                });
            });

        });
    }

    function showUserMenu(elem) {

        var card = $(elem).parents('.card')[0];
        var page = $(card).parents('.page')[0];
        var userId = card.getAttribute('data-userid');

        var menuItems = [];

        menuItems.push({
            name: Globalize.translate('ButtonOpen'),
            id: 'open',
            ironIcon: 'mode-edit'
        });

        menuItems.push({
            name: Globalize.translate('ButtonLibraryAccess'),
            id: 'access',
            ironIcon: 'lock'
        });

        menuItems.push({
            name: Globalize.translate('ButtonParentalControl'),
            id: 'parentalcontrol',
            ironIcon: 'person'
        });

        menuItems.push({
            name: Globalize.translate('ButtonDelete'),
            id: 'delete',
            ironIcon: 'delete'
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                positionTo: card,
                callback: function (id) {

                    switch (id) {

                        case 'open':
                            Dashboard.navigate('useredit.html?userid=' + userId);
                            break;
                        case 'access':
                            Dashboard.navigate('userlibraryaccess.html?userid=' + userId);
                            break;
                        case 'parentalcontrol':
                            Dashboard.navigate('userparentalcontrol.html?userid=' + userId);
                            break;
                        case 'delete':
                            deleteUser(page, userId);
                            break;
                        default:
                            break;
                    }
                }
            });

        });
    }

    function getUserHtml(user, addConnectIndicator) {

        var html = '';

        var cssClass = "card squareCard scalableCard squareCard-scalable";

        if (user.Policy.IsDisabled) {
            cssClass += ' grayscale';
        }

        html += "<div data-userid='" + user.Id + "' class='" + cssClass + "'>";

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable visualCardBox-cardScalable">';

        html += '<div class="cardPadder cardPadder-square"></div>';

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

        var imageClass = 'cardImage';
        if (user.Policy.IsDisabled) {
            imageClass += ' disabledUser';
        }
        html += '<div class="' + imageClass + '" style="background-image:url(\'' + imgUrl + '\');">';

        if (user.ConnectUserId && addConnectIndicator) {
            html += '<div class="cardIndicators">';
            html += '<div class="playedIndicator" title="' + Globalize.translate('TooltipLinkedToEmbyConnect') + '"><i class="md-icon playedIndicatorIcon indicatorIcon">cloud</i></div>';
            html += "</div>";
        }

        html += "</div>";

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter visualCardBox-cardFooter">';

        html += '<div style="text-align:right; float:right;padding:0;">';
        html += '<button type="button" is="paper-icon-button-light" class="btnUserMenu autoSize"><i class="md-icon">more_vert</i></button>';
        html += "</div>";

        html += '<div class="cardText" style="padding-top:10px;padding-bottom:10px;">';
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

    function getUserSectionHtml(users, addConnectIndicator) {

        var html = '';

        html += users.map(function (u) {

            return getUserHtml(u, addConnectIndicator);

        }).join('');

        return html;
    }

    function renderUsersIntoElement(elem, users, addConnectIndicator) {

        var html = getUserSectionHtml(users, addConnectIndicator);

        elem.html(html);

        $('.btnUserMenu', elem).on('click', function () {
            showUserMenu(this);
        });
    }

    function renderUsers(page, users) {

        renderUsersIntoElement($('.localUsers', page), users.filter(function (u) {
            return u.ConnectLinkType != 'Guest';
        }), true);

        renderUsersIntoElement($('.connectUsers', page), users.filter(function (u) {
            return u.ConnectLinkType == 'Guest';
        }));
    }

    function showPendingUserMenu(elem) {

        var menuItems = [];

        menuItems.push({
            name: Globalize.translate('ButtonCancel'),
            id: 'delete',
            ironIcon: 'delete'
        });

        require(['actionsheet'], function (actionsheet) {

            var card = $(elem).parents('.card');
            var page = $(elem).parents('.page');
            var id = card.attr('data-id');

            actionsheet.show({
                items: menuItems,
                positionTo: card,
                callback: function (menuItemId) {

                    switch (menuItemId) {

                        case 'delete':
                            cancelAuthorization(page, id);
                            break;
                        default:
                            break;
                    }
                }
            });
        });
    }

    function getPendingUserHtml(user) {

        var html = '';

        var cssClass = "card squareCard";

        html += "<div data-id='" + user.Id + "' class='" + cssClass + "'>";

        html += '<div class="cardBox cardBox-bottompadded visualCardBox">';
        html += '<div class="cardScalable visualCardBox-cardScalable">';

        html += '<div class="cardPadder cardPadder-square"></div>';

        var href = "#";
        html += '<a class="cardContent" href="' + href + '">';

        var imgUrl = user.ImageUrl || 'css/images/userflyoutdefault.png';

        html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');">';

        html += "</div>";

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter visualCardBox-cardFooter">';

        html += '<div class="cardText" style="text-align:right; float:right;padding:0;">';
        html += '<button type="button" is="paper-icon-button-light" class="btnUserMenu"><i class="md-icon">more_vert</i></button>';
        html += "</div>";

        html += '<div class="cardText" style="padding-top:10px;padding-bottom:10px;">';
        html += user.UserName;
        html += "</div>";

        // cardFooter
        html += "</div>";

        // cardBox
        html += "</div>";

        // card
        html += "</div>";

        return html;
    }

    function renderPendingGuests(page, users) {

        if (users.length) {
            $('.sectionPendingGuests', page).show();
        } else {
            $('.sectionPendingGuests', page).hide();
        }

        var html = users.map(getPendingUserHtml).join('');

        var elem = $('.pending', page).html(html);

        $('.btnUserMenu', elem).on('click', function () {
            showPendingUserMenu(this);
        });
    }

    function cancelAuthorization(page, id) {

        Dashboard.showLoadingMsg();

        // Add/Update connect info
        ApiClient.ajax({

            type: "DELETE",
            url: ApiClient.getUrl('Connect/Pending', {

                Id: id

            })

        }).then(function () {

            loadData(page);

        });
    }

    function loadData(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getUsers().then(function (users) {
            renderUsers(page, users);
            Dashboard.hideLoadingMsg();
        });

        ApiClient.getJSON(ApiClient.getUrl('Connect/Pending')).then(function (pending) {

            renderPendingGuests(page, pending);
        });
    }

    function showLinkUser(page, userId) {
        
        require(['components/guestinviter/connectlink'], function (connectlink) {

            connectlink.show().then(function () {
                loadData(page);
            });
        });
    }

    function showInvitePopup(page) {

        Dashboard.getCurrentUser().then(function (user) {

            if (!user.ConnectUserId) {

                showLinkUser(page, user.Id);
                return;
            }

            require(['components/guestinviter/guestinviter'], function (guestinviter) {

                guestinviter.show().then(function () {
                    loadData(page);
                });
            });
        });
    }

    $(document).on('pageinit', "#userProfilesPage", function () {

        var page = this;

        $('.btnInvite', page).on('click', function () {

            showInvitePopup(page);
        });

        $('.btnAddUser', page).on('click', function () {

            Dashboard.navigate('usernew.html');
        });

    }).on('pagebeforeshow', "#userProfilesPage", function () {

        var page = this;

        loadData(page);
    });

});
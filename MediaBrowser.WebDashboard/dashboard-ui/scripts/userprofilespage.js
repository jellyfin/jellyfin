(function (document, window, $) {

    function renderLibrarySharingList(page, result) {

        var folderHtml = '';

        folderHtml += '<div data-role="controlgroup">';

        folderHtml += result.Items.map(function (i) {

            var currentHtml = '';

            var id = 'chkShareFolder' + i.Id;

            currentHtml += '<label for="' + id + '">' + i.Name + '</label>';

            var isChecked = true;
            var checkedHtml = isChecked ? ' checked="checked"' : '';

            currentHtml += '<input data-mini="true" class="chkShareFolder" data-folderid="' + i.Id + '" type="checkbox" id="' + id + '"' + checkedHtml + ' />';

            return currentHtml;

        }).join('');

        folderHtml += '</div>';

        $('.librarySharingList', page).html(folderHtml).trigger('create');
    }

    function deleteUser(page, id) {

        var msg = Globalize.translate('DeleteUserConfirmation');

        Dashboard.confirm(msg, Globalize.translate('DeleteUser'), function (result) {

            if (result) {
                Dashboard.showLoadingMsg();

                ApiClient.deleteUser(id).then(function () {

                    loadData(page);
                });
            }
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

        require(['actionsheet'], function () {

            ActionSheetElement.show({
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

        var cssClass = "card squareCard bottomPaddedCard";

        if (user.Policy.IsDisabled) {
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

        var imageClass = 'cardImage';
        if (user.Policy.IsDisabled) {
            imageClass += ' disabledUser';
        }
        html += '<div class="' + imageClass + '" style="background-image:url(\'' + imgUrl + '\');">';

        if (user.ConnectUserId && addConnectIndicator) {
            html += '<div class="playedIndicator" title="' + Globalize.translate('TooltipLinkedToEmbyConnect') + '"><iron-icon icon="cloud"></iron-icon></div>';
        }

        html += "</div>";

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter">';

        html += '<div class="cardText" style="text-align:right; float:right;padding:0;">';
        html += '<paper-icon-button icon="' + AppInfo.moreIcon + '" class="btnUserMenu"></paper-icon-button>';
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

        elem.html(html).trigger('create');

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

        require(['jqmpopup'], function () {
            var card = $(elem).parents('.card');
            var page = $(elem).parents('.page');
            var id = card.attr('data-id');

            $('.userMenu', page).popup("close").remove();

            var html = '<div data-role="popup" class="userMenu tapHoldMenu" data-theme="a">';

            html += '<ul data-role="listview" style="min-width: 180px;">';
            html += '<li data-role="list-divider">' + Globalize.translate('HeaderMenu') + '</li>';

            html += '<li><a href="#" class="btnDelete" data-id="' + id + '">' + Globalize.translate('ButtonCancel') + '</a></li>';

            html += '</ul>';

            html += '</div>';

            page.append(html);

            var flyout = $('.userMenu', page).popup({ positionTo: elem || "window" }).trigger('create').popup("open").on("popupafterclose", function () {

                $(this).off("popupafterclose").remove();

            });

            $('.btnDelete', flyout).on('click', function () {
                cancelAuthorization(page, this.getAttribute('data-id'));
                $('.userMenu', page).popup("close").remove();
            });
        });
    }

    function getPendingUserHtml(user) {

        var html = '';

        var cssClass = "card squareCard bottomPaddedCard";

        html += "<div data-id='" + user.Id + "' class='" + cssClass + "'>";

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        var href = "#";
        html += '<a class="cardContent" href="' + href + '">';

        var imgUrl = user.ImageUrl || 'css/images/userflyoutdefault.png';

        html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');">';

        html += "</div>";

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter">';

        html += '<div class="cardText" style="text-align:right; float:right;padding:0;">';
        html += '<paper-icon-button icon="' + AppInfo.moreIcon + '" class="btnUserMenu"></paper-icon-button>';
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

        var elem = $('.pending', page).html(html).trigger('create');

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

        ApiClient.getJSON(ApiClient.getUrl("Library/MediaFolders", { IsHidden: false })).then(function (result) {

            renderLibrarySharingList(page, result);
        });
    }

    function inviteUser(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getJSON(ApiClient.getUrl("Channels", {})).then(function (channelsResult) {

            var shareExcludes = $(".chkShareFolder:checked", page).get().map(function (i) {

                return i.getAttribute('data-folderid');
            });

            // Add/Update connect info
            ApiClient.ajax({

                type: "POST",
                url: ApiClient.getUrl('Connect/Invite'),
                dataType: 'json',
                data: {

                    ConnectUsername: $('#txtConnectUsername', page).val(),
                    EnabledLibraries: shareExcludes.join(','),
                    SendingUserId: Dashboard.getCurrentUserId(),
                    EnableLiveTv: false
                }

            }).then(function (result) {

                $('#popupInvite').popup('close');

                Dashboard.hideLoadingMsg();

                showNewUserInviteMessage(page, result);

            });
        });
    }

    function showNewUserInviteMessage(page, result) {

        if (!result.IsNewUserInvitation && !result.IsPending) {

            // It was immediately approved
            loadData(page);
            return;
        }

        var message = result.IsNewUserInvitation ?
            Globalize.translate('MessageInvitationSentToNewUser', result.GuestDisplayName) :
            Globalize.translate('MessageInvitationSentToUser', result.GuestDisplayName);

        // Need a timeout because jquery mobile will not show a popup while a previous one is in the act of closing
        setTimeout(function () {

            Dashboard.alert({
                message: message,
                title: Globalize.translate('HeaderInvitationSent'),
                callback: function () {
                    loadData(page);
                }
            });

        }, 300);
    }

    function showInvitePopup(page) {

        Dashboard.getCurrentUser().then(function (user) {

            if (user.ConnectUserId) {

                $('#popupInvite', page).popup('open');
                $('#txtConnectUsername', page).val('');
            } else {

                var msg = Globalize.translate('MessageConnectAccountRequiredToInviteGuest');

                msg += '<br/>';
                msg += '<br/>';
                msg += '<a href="useredit.html?userId=' + user.Id + '">' + Globalize.translate('ButtonLinkMyEmbyAccount') + '</a>';
                msg += '<br/>';

                Dashboard.alert({
                    message: msg,
                    title: Globalize.translate('HeaderInviteGuest')
                });

            }

        });
    }

    function onSubmit() {
        var form = this;

        var page = $(form).parents('.page');

        inviteUser(page);

        return false;
    }

    $(document).on('pageinit', "#userProfilesPage", function () {

        var page = this;

        $('.btnInvite', page).on('click', function () {

            showInvitePopup(page);
        });

        $('.btnAddUser', page).on('click', function () {

            Dashboard.navigate('usernew.html');
        });

        $('.addUserForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pagebeforeshow', "#userProfilesPage", function () {

        var page = this;

        loadData(page);
    });

})(document, window, jQuery);
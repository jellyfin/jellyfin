(function () {

    function connectToServer(page, server) {

        Dashboard.showLoadingMsg();

        ConnectionManager.connectToServer(server).done(function (result) {

            Dashboard.hideLoadingMsg();

            switch (result.State) {

                case MediaBrowser.ConnectionState.Unavilable:
                    showServerConnectionFailure();
                    break;
                case MediaBrowser.ConnectionState.SignedIn:
                    {
                        var apiClient = result.ApiClient;

                        Dashboard.serverAddress(apiClient.serverAddress());
                        Dashboard.setCurrentUser(apiClient.getCurrentUserId(), apiClient.accessToken());
                        window.location = 'index.html';
                    }
                    break;
                default:
                    break;
            }

        });
    }

    function showServerConnectionFailure() {

        // Need the timeout because jquery mobile will not show a popup while another is in process of closing
        setTimeout(function () {
            Dashboard.alert({
                message: Globalize.translate("MessageUnableToConnectToServer"),
                title: Globalize.translate("HeaderConnectionFailure")
            });

        }, 300);
    }

    function getServerHtml(server) {

        var html = '';

        var cssClass = "card homePageSquareCard bottomPaddedCard";

        html += "<div data-id='" + server.Id + "' data-connectserverid='" + (server.ConnectServerId || '') + "' class='" + cssClass + "'>";

        html += '<div class="cardBox visualCardBox visualCardBox-b">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        var href = "#";
        html += '<a class="cardContent lnkServer" data-serverid="' + server.Id + '" href="' + href + '">';

        var imgUrl = 'css/images/server.png';

        html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');">';

        html += "</div>";

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter">';

        html += '<div class="cardText" style="text-align:right; float:right;">';

        html += '<button class="btnServerMenu" type="button" data-inline="true" data-iconpos="notext" data-icon="ellipsis-v" style="margin: 2px 0 0;"></button>';

        html += "</div>";

        html += '<div class="cardText" style="margin-right: 30px; padding: 11px 0 10px;">';
        html += server.Name;
        html += "</div>";

        // cardFooter
        html += "</div>";

        // cardBox
        html += "</div>";

        // card
        html += "</div>";

        return html;
    }

    function renderServers(page, servers) {

        if (servers.length) {
            $('.noServersMessage', page).hide();
        } else {
            $('.noServersMessage', page).show();
        }

        var html = '';

        html += servers.map(getServerHtml).join('');

        var elem = $('.serverList', page).html(html).trigger('create');

        $('.lnkServer', elem).on('click', function () {

            var id = this.getAttribute('data-serverid');
            var server = servers.filter(function (s) {
                return s.Id == id;
            })[0];

            connectToServer(page, server);
        });

        $('.btnServerMenu', elem).on('click', function () {
            showServerMenu(this);
        });
    }

    function showGeneralError() {

        // Need the timeout because jquery mobile will not show a popup if there's currently already one in the process of closing
        setTimeout(function () {

            Dashboard.hideLoadingMsg();
            Dashboard.alert({
                message: Globalize.translate('DefaultErrorMessage')
            });
        }, 300);

    }

    function acceptInvitation(page, id) {

        Dashboard.showLoadingMsg();

        // Add/Update connect info
        ConnectionManager.acceptServer(id).done(function () {

            Dashboard.hideLoadingMsg();
            loadPage(page);

        }).fail(function () {

            showGeneralError();
        });
    }

    function deleteServer(page, id) {

        Dashboard.showLoadingMsg();

        // Add/Update connect info
        ConnectionManager.deleteServer(id).done(function () {

            Dashboard.hideLoadingMsg();
            loadPage(page);

        }).fail(function () {

            showGeneralError();

        });
    }

    function rejectInvitation(page, id) {

        Dashboard.showLoadingMsg();

        // Add/Update connect info
        ConnectionManager.rejectServer(id).done(function () {

            Dashboard.hideLoadingMsg();
            loadPage(page);

        }).fail(function () {

            showGeneralError();

        });
    }

    function showServerMenu(elem) {

        var card = $(elem).parents('.card');
        var page = $(elem).parents('.page');
        var id = card.attr('data-id');
        var connectserverid = card.attr('data-connectserverid');

        $('.serverMenu', page).popup("close").remove();

        var html = '<div data-role="popup" class="serverMenu" data-theme="a">';

        html += '<ul data-role="listview" style="min-width: 180px;">';
        html += '<li data-role="list-divider">' + Globalize.translate('HeaderMenu') + '</li>';

        html += '<li><a href="#" class="btnDelete" data-connectserverid="' + connectserverid + '">' + Globalize.translate('ButtonDelete') + '</a></li>';

        html += '</ul>';

        html += '</div>';

        page.append(html);

        var flyout = $('.serverMenu', page).popup({ positionTo: elem || "window" }).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();

        });

        $('.btnDelete', flyout).on('click', function () {
            deleteServer(page, this.getAttribute('data-connectserverid'));
            $('.serverMenu', page).popup("close").remove();
        });
    }

    function showPendingInviteMenu(elem) {

        var card = $(elem).parents('.card');
        var page = $(elem).parents('.page');
        var id = card.attr('data-id');

        $('.inviteMenu', page).popup("close").remove();

        var html = '<div data-role="popup" class="inviteMenu" data-theme="a">';

        html += '<ul data-role="listview" style="min-width: 180px;">';
        html += '<li data-role="list-divider">' + Globalize.translate('HeaderMenu') + '</li>';

        html += '<li><a href="#" class="btnAccept" data-id="' + id + '">' + Globalize.translate('ButtonAccept') + '</a></li>';
        html += '<li><a href="#" class="btnReject" data-id="' + id + '">' + Globalize.translate('ButtonReject') + '</a></li>';

        html += '</ul>';

        html += '</div>';

        page.append(html);

        var flyout = $('.inviteMenu', page).popup({ positionTo: elem || "window" }).trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();

        });

        $('.btnAccept', flyout).on('click', function () {
            acceptInvitation(page, this.getAttribute('data-id'));
            $('.inviteMenu', page).popup("close").remove();
        });

        $('.btnReject', flyout).on('click', function () {
            rejectInvitation(page, this.getAttribute('data-id'));
            $('.inviteMenu', page).popup("close").remove();
        });
    }

    function getPendingInviteHtml(invite) {

        var html = '';

        var cssClass = "card homePageSquareCard alternateHover bottomPaddedCard";

        html += "<div data-id='" + invite.Id + "' class='" + cssClass + "'>";

        html += '<div class="cardBox visualCardBox visualCardBox-b">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        var href = "#";
        html += '<a class="cardContent" href="' + href + '">';

        var imgUrl = 'css/images/server.png';

        html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');">';

        html += "</div>";

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter">';

        html += '<div class="cardText" style="text-align:right; float:right;">';

        html += '<button class="btnInviteMenu" type="button" data-inline="true" data-iconpos="notext" data-icon="ellipsis-v" style="margin: 2px 0 0;"></button>';
        html += "</div>";

        html += '<div class="cardText" style="margin-right: 30px; padding: 11px 0 10px;">';
        html += invite.Name;
        html += "</div>";

        // cardFooter
        html += "</div>";

        // cardBox
        html += "</div>";

        // card
        html += "</div>";

        return html;
    }

    function renderInvitations(page, list) {

        if (list.length) {
            $('.invitationSection', page).show();
        } else {
            $('.invitationSection', page).hide();
        }

        var html = list.map(getPendingInviteHtml).join('');

        var elem = $('.invitationList', page).html(html).trigger('create');

        $('.btnInviteMenu', elem).on('click', function () {
            showPendingInviteMenu(this);
        });
    }

    function loadInvitations(page) {

        ConnectionManager.getUserInvitations().done(function (list) {

            renderInvitations(page, list);

        });
    }

    function loadPage(page) {

        Dashboard.showLoadingMsg();

        ConnectionManager.getServers().done(function (servers) {

            renderServers(page, servers);

            Dashboard.hideLoadingMsg();
        });

        loadInvitations(page);
    }

    $(document).on('pageshow', "#selectServerPage", function () {

        var page = this;

        loadPage(page);

    });

    window.SelectServerPage = {

        onServerAddressEntrySubmit: function () {

            Dashboard.showLoadingMsg();

            var form = this;
            var page = $(form).parents('.page');


            // Disable default form submission
            return false;

        }

    };

})();
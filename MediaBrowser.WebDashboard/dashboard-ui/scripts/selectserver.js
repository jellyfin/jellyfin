(function () {

	var serverList = [];
	function connectToServer(page, server) {

        Dashboard.showLoadingMsg();

        ConnectionManager.connectToServer(server).done(function (result) {

            Dashboard.hideLoadingMsg();

            var apiClient = result.ApiClient;

            switch (result.State) {

                case MediaBrowser.ConnectionState.SignedIn:
                    {
                        Dashboard.onServerChanged(apiClient.getCurrentUserId(), apiClient.accessToken(), apiClient);
                        Dashboard.navigate('index.html');
                    }
                    break;
                case MediaBrowser.ConnectionState.ServerSignIn:
                    {
                        Dashboard.onServerChanged(null, null, apiClient);
                        Dashboard.navigate('login.html?serverid=' + result.Servers[0].Id);
                    }
                    break;
                default:
                    showServerConnectionFailure();
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

        var cssClass = "card squareCard bottomPaddedCard";

        html += "<div data-id='" + server.Id + "' data-connectserverid='" + (server.ConnectServerId || '') + "' class='" + cssClass + "'>";

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        var href = server.href || "#";
        html += '<a class="cardContent lnkServer" data-serverid="' + server.Id + '" href="' + href + '">';

        var imgUrl = server.Id == 'connect' ? 'css/images/logo536.png' : '';

        if (imgUrl) {
            html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');">';
        } else {
            html += '<div class="cardImage" style="text-align:center;">';

            var icon = server.Id == 'new' ? 'plus-circle' : 'server';
            html += '<i class="fa fa-' + icon + '" style="color:#fff;vertical-align:middle;font-size:100px;margin-top:40px;"></i>';
        }

        html += "</div>";

        // cardContent'
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter outerCardFooter">';

        if (server.showOptions !== false) {
            html += '<div class="cardText" style="text-align:right; float:right;padding:0;">';
            html += '<paper-icon-button icon="more-vert" class="btnServerMenu"></paper-icon-button>';
            html += "</div>";
        }

        html += '<div class="cardText">';
        html += server.Name;
        html += "</div>";

        html += '<div class="cardText">';
        html += '&nbsp;';
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

    	serverList = servers;

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

            if (id != 'new' && id != 'connect') {

                var server = servers.filter(function (s) {
                    return s.Id == id;
                })[0];

                connectToServer(page, server);
            }

        });

        $('.btnServerMenu', elem).on('click', function () {
            showServerMenu(this);
        });
    }

    function showGeneralError() {

        // Need the timeout because jquery mobile will not show a popup if there's currently already one in the process of closing
        setTimeout(function () {

            Dashboard.hideModalLoadingMsg();
            Dashboard.alert({
                message: Globalize.translate('DefaultErrorMessage')
            });
        }, 300);

    }

    function acceptInvitation(page, id) {

        Dashboard.showModalLoadingMsg();

        // Add/Update connect info
        ConnectionManager.acceptServer(id).done(function () {

            Dashboard.hideModalLoadingMsg();
            loadPage(page);

        }).fail(function () {

            showGeneralError();
        });
    }

    function deleteServer(page, id) {

        Dashboard.showModalLoadingMsg();

        // Add/Update connect info
        ConnectionManager.deleteServer(id).done(function () {

            Dashboard.hideModalLoadingMsg();

            // Just re-render the servers without discovering them again
            // If we re-discover then the one they deleted may just come back
            var newServerList = serverList.filter(function(s){
            	return s.Id != id;
            });
            renderServers(page, newServerList);

        }).fail(function () {

            showGeneralError();

        });
    }

    function rejectInvitation(page, id) {

        Dashboard.showModalLoadingMsg();

        // Add/Update connect info
        ConnectionManager.rejectServer(id).done(function () {

            Dashboard.hideModalLoadingMsg();

            loadPage(page);

        }).fail(function () {

            showGeneralError();

        });
    }

    function showServerMenu(elem) {

        var card = $(elem).parents('.card');
        var page = $(elem).parents('.page');
        var serverId = card.attr('data-id');
        var connectserverid = card.attr('data-connectserverid');

        var menuItems = [];

        menuItems.push({
            name: Globalize.translate('ButtonDelete'),
            id: 'delete',
            ironIcon: 'delete'
        });

        require(['actionsheet'], function () {

            ActionSheetElement.show({
                items: menuItems,
                positionTo: elem,
                callback: function (id) {

                    switch (id) {

                        case 'delete':
                            deleteServer(page, connectserverid);
                            break;
                        default:
                            break;
                    }
                }
            });

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

        var cssClass = "card squareCard alternateHover bottomPaddedCard";

        html += "<div data-id='" + invite.Id + "' class='" + cssClass + "'>";

        html += '<div class="cardBox visualCardBox">';
        html += '<div class="cardScalable">';

        html += '<div class="cardPadder"></div>';

        var href = "#";
        html += '<a class="cardContent" href="' + href + '">';

        html += '<div class="cardImage" style="text-align:center;">';
        html += '<i class="fa fa-globe" style="color:#fff;vertical-align:middle;font-size:100px;margin-top:40px;"></i>';
        html += "</div>";

        // cardContent
        html += "</a>";

        // cardScalable
        html += "</div>";

        html += '<div class="cardFooter outerCardFooter">';

        html += '<div class="cardText" style="text-align:right; float:right;padding:0;">';
        html += '<paper-icon-button icon="more-vert" class="btnInviteMenu"></paper-icon-button>';
        html += "</div>";

        html += '<div class="cardText">';
        html += invite.Name;
        html += "</div>";

        html += '<div class="cardText">';
        html += '&nbsp;';
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

        if (ConnectionManager.isLoggedIntoConnect()) {

            ConnectionManager.getUserInvitations().done(function (list) {

                renderInvitations(page, list);

            });

        } else {

            renderInvitations(page, []);
        }

    }

    function loadPage(page) {

        Dashboard.showLoadingMsg();

        Backdrops.setDefault(page);

        ConnectionManager.getAvailableServers().done(function (servers) {

            servers = servers.slice(0);

            renderServers(page, servers);

            Dashboard.hideLoadingMsg();
        });

        loadInvitations(page);

        if (ConnectionManager.isLoggedIntoConnect()) {
            $('.connectLogin', page).hide();
        } else {
            $('.connectLogin', page).show();
        }
    }

    function updatePageStyle(page) {

        if (ConnectionManager.isLoggedIntoConnect()) {
            $(page).addClass('libraryPage').addClass('noSecondaryNavPage').removeClass('standalonePage');
        } else {
            $(page).removeClass('libraryPage').removeClass('noSecondaryNavPage').addClass('standalonePage');
        }

        if (AppInfo.isNativeApp) {
            $('.addServer', page).show();
        } else {
            $('.addServer', page).hide();
        }
    }

    $(document).on('pageinitdepends pagebeforeshowready', "#selectServerPage", function () {

        var page = this;
        updatePageStyle(page);

    }).on('pageshowready', "#selectServerPage", function () {

        var page = this;

        loadPage(page);

    });

})();
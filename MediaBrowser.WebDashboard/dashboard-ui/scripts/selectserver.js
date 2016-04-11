define(['jQuery'], function ($) {

    function connectToServer(page, server) {

        Dashboard.showLoadingMsg();

        ConnectionManager.connectToServer(server).then(function (result) {

            Dashboard.hideLoadingMsg();

            var apiClient = result.ApiClient;

            switch (result.State) {

                case MediaBrowser.ConnectionState.SignedIn:
                    {
                        Dashboard.onServerChanged(apiClient.getCurrentUserId(), apiClient.accessToken(), apiClient);
                        Dashboard.navigate('home.html');
                    }
                    break;
                case MediaBrowser.ConnectionState.ServerSignIn:
                    {
                        Dashboard.onServerChanged(null, null, apiClient);
                        Dashboard.navigate('login.html?serverid=' + result.Servers[0].Id);
                    }
                    break;
                case MediaBrowser.ConnectionState.ServerUpdateNeeded:
                    {
                        Dashboard.alert(alert({

                            text: Globalize.translate('core#ServerUpdateNeeded', 'https://emby.media'),
                            html: Globalize.translate('core#ServerUpdateNeeded', '<a href="https://emby.media">https://emby.media</a>')

                        }));
                    }
                    break;
                default:
                    showServerConnectionFailure();
                    break;
            }

        });
    }

    function showServerConnectionFailure() {

        Dashboard.alert({
            message: Globalize.translate("MessageUnableToConnectToServer"),
            title: Globalize.translate("HeaderConnectionFailure")
        });
    }

    function getServerHtml(server) {

        var html = '';

        html += '<paper-icon-item class="serverItem" data-id="' + server.Id + '">';

        html += '<paper-fab mini class="blue lnkServer" icon="wifi" item-icon></paper-fab>';

        html += '<paper-item-body class="lnkServer" two-line>';
        html += '<a class="clearLink" href="#">';

        html += '<div>';
        html += server.Name;
        html += '</div>';

        //html += '<div secondary>';
        //html += MediaBrowser.ServerInfo.getServerAddress(server, server.LastConnectionMode);
        //html += '</div>';

        html += '</a>';
        html += '</paper-item-body>';

        if (server.Id) {
            html += '<paper-icon-button icon="' + AppInfo.moreIcon + '" class="btnServerMenu"></paper-icon-button>';
        }

        html += '</paper-icon-item>';

        return html;
    }

    function renderServers(page, servers) {

        if (servers.length) {
            $('.noServersMessage', page).hide();
            $('.serverList', page).show();
        } else {
            $('.noServersMessage', page).show();
            $('.serverList', page).hide();
        }

        var html = '';

        html += servers.map(getServerHtml).join('');

        var elem = $('.serverList', page).html(html);

        $('.lnkServer', elem).on('click', function () {

            var item = $(this).parents('.serverItem')[0];
            var id = item.getAttribute('data-id');

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

            Dashboard.hideModalLoadingMsg();
            Dashboard.alert({
                message: Globalize.translate('DefaultErrorMessage')
            });
        }, 300);

    }

    function acceptInvitation(page, id) {

        Dashboard.showModalLoadingMsg();

        // Add/Update connect info
        ConnectionManager.acceptServer(id).then(function () {

            Dashboard.hideModalLoadingMsg();
            loadPage(page);

        }, function () {

            showGeneralError();
        });
    }

    function deleteServer(page, serverId) {

        Dashboard.showModalLoadingMsg();

        // Add/Update connect info
        ConnectionManager.deleteServer(serverId).then(function () {

            Dashboard.hideModalLoadingMsg();

            loadPage(page);

        }, function () {

            showGeneralError();

        });
    }

    function rejectInvitation(page, id) {

        Dashboard.showModalLoadingMsg();

        // Add/Update connect info
        ConnectionManager.rejectServer(id).then(function () {

            Dashboard.hideModalLoadingMsg();

            loadPage(page);

        }, function () {

            showGeneralError();

        });
    }

    function showServerMenu(elem) {

        var card = $(elem).parents('.serverItem');
        var page = $(elem).parents('.page');
        var serverId = card.attr('data-id');

        var menuItems = [];

        menuItems.push({
            name: Globalize.translate('ButtonDelete'),
            id: 'delete',
            ironIcon: 'delete'
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                positionTo: elem,
                callback: function (id) {

                    switch (id) {

                        case 'delete':
                            deleteServer(page, serverId);
                            break;
                        default:
                            break;
                    }
                }
            });

        });
    }

    function showPendingInviteMenu(elem) {

        var card = $(elem).parents('.inviteItem');
        var page = $(elem).parents('.page');
        var invitationId = card.attr('data-id');

        var menuItems = [];

        menuItems.push({
            name: Globalize.translate('ButtonAccept'),
            id: 'accept',
            ironIcon: 'add'
        });

        menuItems.push({
            name: Globalize.translate('ButtonReject'),
            id: 'reject',
            ironIcon: 'cancel'
        });

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: menuItems,
                positionTo: elem,
                callback: function (id) {

                    switch (id) {

                        case 'accept':
                            acceptInvitation(page, invitationId);
                            break;
                        case 'reject':
                            rejectInvitation(page, invitationId);
                            break;
                        default:
                            break;
                    }
                }
            });

        });
    }

    function getPendingInviteHtml(invite) {

        var html = '';

        html += '<paper-icon-item class="inviteItem" data-id="' + invite.Id + '">';

        html += '<paper-fab mini class="blue lnkServer" icon="wifi" item-icon></paper-fab>';

        html += '<paper-item-body two-line>';

        html += '<div>';
        html += invite.Name;
        html += '</div>';

        html += '</paper-item-body>';

        html += '<paper-icon-button icon="' + AppInfo.moreIcon + '" class="btnInviteMenu"></paper-icon-button>';

        html += '</paper-icon-item>';

        return html;
    }

    function renderInvitations(page, list) {

        if (list.length) {
            $('.invitationSection', page).show();
        } else {
            $('.invitationSection', page).hide();
        }

        var html = list.map(getPendingInviteHtml).join('');

        var elem = $('.invitationList', page).html(html);

        $('.btnInviteMenu', elem).on('click', function () {
            showPendingInviteMenu(this);
        });
    }

    function loadInvitations(page) {

        if (ConnectionManager.isLoggedIntoConnect()) {

            ConnectionManager.getUserInvitations().then(function (list) {

                renderInvitations(page, list);

            });

        } else {

            renderInvitations(page, []);
        }

    }

    function loadPage(page) {

        Dashboard.showLoadingMsg();

        ConnectionManager.getAvailableServers().then(function (servers) {

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

        if (getParameterByName('showuser') == '1') {
            $(page).addClass('libraryPage').addClass('noSecondaryNavPage').removeClass('standalonePage');
        } else {
            $(page).removeClass('libraryPage').removeClass('noSecondaryNavPage').addClass('standalonePage');
        }
    }

    pageIdOn('pagebeforeshow', "selectServerPage", function () {

        var page = this;
        updatePageStyle(page);
    });

    pageIdOn('pageshow', "selectServerPage", function () {

        var page = this;

        loadPage(page);
    });

});
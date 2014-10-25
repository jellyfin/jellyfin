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

        Dashboard.alert({
            message: Globalize.translate("MessageUnableToConnectToServer"),
            title: Globalize.translate("HeaderConnectionFailure")
        });
    }

    function renderServers(page, servers) {

        if (servers.length) {
            $('.noServersMessage', page).hide();
        } else {
            $('.noServersMessage', page).show();
        }

        var html = '<ul data-role="listview" data-inset="true">';

        html += servers.map(function (s) {

            var serverHtml = '';

            serverHtml += '<li>';

            serverHtml += '<a class="lnkServer" data-serverid="' + s.Id + '" href="#">';
            serverHtml += '<h3>';
            serverHtml += s.Name;
            serverHtml += '</h3>';
            serverHtml += '</a>';

            serverHtml += '</li>';

            return serverHtml;

        }).join('');

        html += '</ul>';

        var elem = $('.serverList', page).html(html).trigger('create');

        $('.lnkServer', elem).on('click', function () {

            var id = this.getAttribute('data-serverid');
            var server = servers.filter(function (s) {
                return s.Id == id;
            })[0];

            connectToServer(page, server);
        });
    }

    function loadPage(page) {

        Dashboard.showLoadingMsg();

        ConnectionManager.getServers().done(function (servers) {

            renderServers(page, servers);

            Dashboard.hideLoadingMsg();
        });
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
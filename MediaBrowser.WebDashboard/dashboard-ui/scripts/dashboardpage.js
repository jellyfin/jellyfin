var DashboardPage = {

    newsStartIndex: 0,

    onPageShow: function () {

        var page = this;

        DashboardPage.newsStartIndex = 0;
        
        Dashboard.showLoadingMsg();
        DashboardPage.pollForInfo(page);
        DashboardPage.startInterval();
        $(ApiClient).on("websocketmessage", DashboardPage.onWebSocketMessage).on("websocketopen", DashboardPage.onWebSocketConnectionChange).on("websocketerror", DashboardPage.onWebSocketConnectionChange).on("websocketclose", DashboardPage.onWebSocketConnectionChange);

        DashboardPage.lastAppUpdateCheck = null;
        DashboardPage.lastPluginUpdateCheck = null;

        Dashboard.getPluginSecurityInfo().done(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter) {
                $('#contribute', page).hide();
            } else {
                $('#contribute', page).show();
            }

        });

        DashboardPage.reloadNews(page);
    },

    reloadNews: function (page) {

        var query = {
            StartIndex: DashboardPage.newsStartIndex,
            Limit: 5
        };

        ApiClient.getProductNews(query).done(function (result) {

            var html = result.Items.map(function (item) {

                var itemHtml = '';

                itemHtml += '<div class="newsItem">';
                itemHtml += '<a class="newsItemHeader" href="' + item.Link + '" target="_blank">' + item.Title + '</a>';

                var date = parseISO8601Date(item.Date, { toLocal: true });
                itemHtml += '<div class="newsItemDate">' + date.toLocaleDateString() + '</div>';

                itemHtml += '<div class="newsItemDescription">' + item.DescriptionHtml + '</div>';
                itemHtml += '</div>';

                return itemHtml;
            });

            var pagingHtml = '';
            pagingHtml += '<div>';
            pagingHtml += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, false, [], false);
            pagingHtml += '</div>';
            
            html = html.join('') + pagingHtml;

            var elem = $('.latestNewsItems', page).html(html).trigger('create');

            $('.btnNextPage', elem).on('click', function () {
                DashboardPage.newsStartIndex += query.Limit;
                DashboardPage.reloadNews(page);
            });

            $('.btnPreviousPage', elem).on('click', function () {
                DashboardPage.newsStartIndex -= query.Limit;
                DashboardPage.reloadNews(page);
            });
        });

    },

    onPageHide: function () {

        $(ApiClient).off("websocketmessage", DashboardPage.onWebSocketMessage).off("websocketopen", DashboardPage.onWebSocketConnectionChange).off("websocketerror", DashboardPage.onWebSocketConnectionChange).off("websocketclose", DashboardPage.onWebSocketConnectionChange);
        DashboardPage.stopInterval();
    },

    startInterval: function () {

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("DashboardInfoStart", "0,1500");
        }
    },

    stopInterval: function () {

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("DashboardInfoStop");
        }
    },

    onWebSocketMessage: function (e, msg) {

        var page = $.mobile.activePage;

        if (msg.MessageType == "DashboardInfo") {
            DashboardPage.renderInfo(page, msg.Data);
        }
    },

    onWebSocketConnectionChange: function () {

        DashboardPage.stopInterval();
        DashboardPage.startInterval();
    },

    pollForInfo: function (page) {

        $.getJSON("dashboardInfo").done(function (result) {

            DashboardPage.renderInfo(page, result);

        });
    },

    renderInfo: function (page, dashboardInfo) {

        DashboardPage.lastDashboardInfo = dashboardInfo;

        DashboardPage.renderRunningTasks(dashboardInfo);
        DashboardPage.renderSystemInfo(page, dashboardInfo);
        DashboardPage.renderActiveConnections(page, dashboardInfo);

        Dashboard.hideLoadingMsg();
    },

    renderActiveConnections: function (page, dashboardInfo) {

        var html = '';

        var table = $('.tblConnections', page);

        $('.trSession', table).addClass('deadSession');

        var deviceId = ApiClient.deviceId();

        for (var i = 0, length = dashboardInfo.ActiveConnections.length; i < length; i++) {

            var connection = dashboardInfo.ActiveConnections[i];

            var rowId = 'trSession' + connection.Id;

            var elem = $('#' + rowId, page);

            if (elem.length) {
                DashboardPage.updateSession(elem, connection);
                continue;
            }

            html += '<tr class="trSession" id="' + rowId + '">';

            html += '<td class="clientType" style="text-align:center;">';
            html += DashboardPage.getClientType(connection);
            html += '</td>';

            html += '<td>';

            html += '<div>';

            if (deviceId == connection.DeviceId) {
                html += connection.Client;
            } else {
                html += '<a href="#" onclick="RemoteControl.showMenu({sessionId:\'' + connection.Id + '\'});">' + connection.Client + '</a>';
            }
            html += '</div>';

            html += '<div>' + connection.ApplicationVersion + '</div>';
            html += '<div>' + connection.DeviceName + '</div>';
            html += '</td>';

            html += '<td class="username">';
            html += DashboardPage.getUsersHtml(connection);
            html += '</td>';

            var nowPlayingItem = connection.NowPlayingItem;

            html += '<td class="nowPlayingImage">';
            html += DashboardPage.getNowPlayingImage(nowPlayingItem);
            html += '</td>';

            html += '<td class="nowPlayingText">';
            html += DashboardPage.getNowPlayingText(connection, nowPlayingItem);
            html += '</td>';

            html += '</tr>';

        }

        table.append(html).trigger('create');

        $('.deadSession', table).remove();
    },

    getUsersHtml: function (session) {

        var html = '';

        if (session.UserId) {
            html += '<div><a href="useredit.html?userid=' + session.UserId + '">' + session.UserName + '</a><div>';
        }

        html += session.AdditionalUsers.map(function (currentSession) {

            return '<div><a href="useredit.html?userid=' + currentSession.UserId + '">' + currentSession.UserName + '</a><div>';
        });

        return html;
    },

    updateSession: function (row, session) {

        row.removeClass('deadSession');

        $('.username', row).html(DashboardPage.getUsersHtml(session)).trigger('create');

        var nowPlayingItem = session.NowPlayingItem;

        $('.nowPlayingText', row).html(DashboardPage.getNowPlayingText(session, nowPlayingItem)).trigger('create');

        var imageRow = $('.nowPlayingImage', row);

        var image = $('img', imageRow)[0];

        var nowPlayingItemId = nowPlayingItem ? nowPlayingItem.Id : null;
        var nowPlayingItemImageTag = nowPlayingItem ? nowPlayingItem.PrimaryImageTag : null;

        if (!image || image.getAttribute('data-itemid') != nowPlayingItemId || image.getAttribute('data-tag') != nowPlayingItemImageTag) {
            imageRow.html(DashboardPage.getNowPlayingImage(nowPlayingItem));
        }
    },

    getClientType: function (connection) {

        var clientLowered = connection.Client.toLowerCase();

        if (clientLowered == "dashboard") {

            var device = connection.DeviceName.toLowerCase();

            var imgUrl;

            if (device.indexOf('chrome') != -1) {
                imgUrl = 'css/images/clients/chrome.png';
            }
            else if (device.indexOf('firefox') != -1) {
                imgUrl = 'css/images/clients/firefox.png';
            }
            else if (device.indexOf('internet explorer') != -1) {
                imgUrl = 'css/images/clients/ie.png';
            }
            else if (device.indexOf('safari') != -1) {
                imgUrl = 'css/images/clients/safari.png';
            }
            else {
                imgUrl = 'css/images/clients/html5.png';
            }

            return "<img src='" + imgUrl + "' alt='Dashboard' />";
        }
        if (clientLowered == "mb-classic") {

            return "<img src='css/images/clients/mbc.png' alt='Media Browser Classic' />";
        }
        if (clientLowered == "media browser theater") {

            return "<img src='css/images/clients/mb.png' alt='Media Browser Theater' />";
        }
        if (clientLowered == "android") {

            return "<img src='css/images/clients/android.png' alt='Android' />";
        }
        if (clientLowered == "roku") {

            return "<img src='css/images/clients/roku.jpg' alt='Roku' />";
        }
        if (clientLowered == "ios") {

            return "<img src='css/images/clients/ios.png' alt='iOS' />";
        }
        if (clientLowered == "windows rt") {

            return "<img src='css/images/clients/windowsrt.png' alt='Windows RT' />";
        }
        if (clientLowered == "windows phone") {

            return "<img src='css/images/clients/windowsphone.png' alt='Windows Phone' />";
        }
        if (clientLowered == "dlna") {

            return "<img src='css/images/clients/dlna.png' alt='Dlna' />";
        }
        if (clientLowered == "mbkinect") {

            return "<img src='css/images/clients/mbkinect.png' alt='MB Kinect' />";
        }
        if (clientLowered == "xbmc") {

            return "<img src='css/images/clients/xbmc.png' alt='Xbmc' />";
        }

        return connection.Client;
    },

    getNowPlayingImage: function (item) {

        if (item && item.PrimaryImageTag) {
            var url = ApiClient.getImageUrl(item.Id, {
                type: "Primary",
                height: 100,
                tag: item.PrimaryImageTag
            });

            url += "&xxx=" + new Date().getTime();

            return "<img data-itemid='" + item.Id + "' data-tag='" + item.PrimaryImageTag + "' class='clientNowPlayingImage' src='" + url + "' alt='" + item.Name + "' title='" + item.Name + "' />";
        }

        return "";
    },

    getNowPlayingText: function (connection, item) {

        var html = "";

        if (item) {

            html += "<div><a href='itemdetails.html?id=" + item.Id + "'>" + item.Name + "</a></div>";

            html += "<div>";

            if (item.RunTimeTicks) {
                html += Dashboard.getDisplayTime(connection.NowPlayingPositionTicks || 0) + " / ";

                html += Dashboard.getDisplayTime(item.RunTimeTicks);
            }

            html += "</div>";
        }

        return html;
    },

    renderRunningTasks: function (dashboardInfo) {

        var page = $.mobile.activePage;

        var html = '';

        if (!dashboardInfo.RunningTasks.length) {
            html += '<p>No tasks are currently running.</p>';
            $('#runningTasksCollapsible', page).hide();
        } else {
            $('#runningTasksCollapsible', page).show();
        }

        for (var i = 0, length = dashboardInfo.RunningTasks.length; i < length; i++) {


            var task = dashboardInfo.RunningTasks[i];

            html += '<p>';

            html += task.Name;

            if (task.State == "Running") {
                var progress = (task.CurrentProgressPercentage || 0).toFixed(1);
                html += '<span style="color:#267F00;margin-right:5px;font-weight:bold;"> - ' + progress + '%</span>';

                html += '<button type="button" data-icon="stop" data-iconpos="notext" data-inline="true" data-mini="true" onclick="DashboardPage.stopTask(\'' + task.Id + '\');">Stop</button>';
            }
            else if (task.State == "Cancelling") {
                html += '<span style="color:#cc0000;"> - Stopping</span>';
            }

            html += '</p>';
        }


        $('#divRunningTasks', page).html(html).trigger('create');
    },

    renderSystemInfo: function (page, dashboardInfo) {

        Dashboard.updateSystemInfo(dashboardInfo.SystemInfo);

        $('#appVersionNumber', page).html(dashboardInfo.SystemInfo.Version);

        var port = dashboardInfo.SystemInfo.HttpServerPortNumber;

        if (port == dashboardInfo.SystemInfo.WebSocketPortNumber) {
            $('#ports', page).html('Running on port <b>' + port + '</b>');
        } else {
            $('#ports', page).html('Running on ports <b>' + port + '</b> and <b>' + dashboardInfo.SystemInfo.WebSocketPortNumber + '</b>');
        }

        if (dashboardInfo.RunningTasks.filter(function (task) {

            return task.Id == dashboardInfo.ApplicationUpdateTaskId;

        }).length) {

            $('#btnUpdateApplication', page).buttonEnabled(false);
        } else {
            $('#btnUpdateApplication', page).buttonEnabled(true);
        }

        if (dashboardInfo.SystemInfo.CanSelfRestart) {
            $('.btnRestartContainer', page).removeClass('hide');
        } else {
            $('.btnRestartContainer', page).addClass('hide');
        }

        DashboardPage.renderApplicationUpdateInfo(dashboardInfo);
        DashboardPage.renderPluginUpdateInfo(dashboardInfo);
        DashboardPage.renderPendingInstallations(dashboardInfo.SystemInfo);
    },

    renderApplicationUpdateInfo: function (dashboardInfo) {

        var page = $.mobile.activePage;

        $('#updateFail', page).hide();

        if (!dashboardInfo.SystemInfo.HasPendingRestart) {

            // Only check once every 10 mins
            if (DashboardPage.lastAppUpdateCheck && (new Date().getTime() - DashboardPage.lastAppUpdateCheck) < 600000) {
                return;
            }

            DashboardPage.lastAppUpdateCheck = new Date().getTime();

            ApiClient.getAvailableApplicationUpdate().done(function (packageInfo) {

                var version = packageInfo[0];

                if (!version) {
                    $('#pUpToDate', page).show();
                    $('#pUpdateNow', page).hide();
                } else {
                    $('#pUpToDate', page).hide();

                    $('#pUpdateNow', page).show();

                    if (dashboardInfo.SystemInfo.CanSelfUpdate) {
                        $('#btnUpdateApplicationContainer', page).show();
                        $('#btnManualUpdateContainer', page).hide();
                    } else {
                        $('#btnUpdateApplicationContainer', page).hide();
                        $('#btnManualUpdateContainer', page).show();
                    }

                    $('#newVersionNumber', page).html("Version " + version.versionStr + " is now available for download.");
                }

            }).fail(function () {

                $('#updateFail', page).show();

            });

        } else {

            if (dashboardInfo.SystemInfo.HasPendingRestart) {
                $('#pUpToDate', page).hide();
            } else {
                $('#pUpToDate', page).show();
            }

            $('#pUpdateNow', page).hide();
        }
    },

    renderPendingInstallations: function (systemInfo) {

        var page = $.mobile.activePage;

        if (systemInfo.CompletedInstallations.length) {

            $('#collapsiblePendingInstallations', page).show();

        } else {
            $('#collapsiblePendingInstallations', page).hide();

            return;
        }

        var html = '';

        for (var i = 0, length = systemInfo.CompletedInstallations.length; i < length; i++) {

            var update = systemInfo.CompletedInstallations[i];

            html += '<div><strong>' + update.Name + '</strong> (' + update.Version + ')</div>';
        }

        $('#pendingInstallations', page).html(html);
    },

    renderPluginUpdateInfo: function (dashboardInfo) {

        // Only check once every 10 mins
        if (DashboardPage.lastPluginUpdateCheck && (new Date().getTime() - DashboardPage.lastPluginUpdateCheck) < 600000) {
            return;
        }

        DashboardPage.lastPluginUpdateCheck = new Date().getTime();

        var page = $.mobile.activePage;

        ApiClient.getAvailablePluginUpdates().done(function (updates) {

            var elem = $('#pPluginUpdates', page);

            if (updates.length) {

                elem.show();

            } else {
                elem.hide();

                return;
            }
            var html = '';

            for (var i = 0, length = updates.length; i < length; i++) {

                var update = updates[i];

                html += '<p><strong>A new version of ' + update.name + ' is available!</strong></p>';

                html += '<button type="button" data-icon="arrow-d" data-theme="b" onclick="DashboardPage.installPluginUpdate(this);" data-name="' + update.name + '" data-guid="' + update.guid + '" data-version="' + update.versionStr + '" data-classification="' + update.classification + '">Update Now</button>';
            }

            elem.html(html).trigger('create');

        }).fail(function () {

            $('#updateFail', page).show();

        });
    },

    installPluginUpdate: function (button) {

        $(button).buttonEnabled(false);

        var name = button.getAttribute('data-name');
        var guid = button.getAttribute('data-guid');
        var version = button.getAttribute('data-version');
        var classification = button.getAttribute('data-classification');

        Dashboard.showLoadingMsg();

        ApiClient.installPlugin(name, guid, classification, version).done(function () {

            Dashboard.hideLoadingMsg();
        });
    },

    updateApplication: function () {

        var page = $.mobile.activePage;
        $('#btnUpdateApplication', page).buttonEnabled(false);

        Dashboard.showLoadingMsg();

        ApiClient.startScheduledTask(DashboardPage.lastDashboardInfo.ApplicationUpdateTaskId).done(function () {

            DashboardPage.pollForInfo(page);

            Dashboard.hideLoadingMsg();
        });
    },

    stopTask: function (id) {

        var page = $.mobile.activePage;

        ApiClient.stopScheduledTask(id).done(function () {

            DashboardPage.pollForInfo(page);
        });

    },

    restart: function () {

        Dashboard.confirm("Are you sure you wish to restart Media Browser Server?", "Restart", function (result) {

            if (result) {
                $('#btnRestartServer').buttonEnabled(false);
                $('#btnShutdown').buttonEnabled(false);
                Dashboard.restartServer();
            }

        });
    },

    shutdown: function () {

        Dashboard.confirm("Are you sure you wish to shutdown Media Browser Server?", "Shutdown", function (result) {

            if (result) {
                $('#btnRestartServer').buttonEnabled(false);
                $('#btnShutdown').buttonEnabled(false);
                ApiClient.shutdownServer();
            }

        });
    }
};

$(document).on('pageshow', "#dashboardPage", DashboardPage.onPageShow).on('pagehide', "#dashboardPage", DashboardPage.onPageHide);
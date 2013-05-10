var DashboardPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();
        DashboardPage.pollForInfo();
        DashboardPage.startInterval();
        $(ApiClient).on("websocketmessage", DashboardPage.onWebSocketMessage).on("websocketopen", DashboardPage.onWebSocketConnectionChange).on("websocketerror", DashboardPage.onWebSocketConnectionChange).on("websocketclose", DashboardPage.onWebSocketConnectionChange);

        DashboardPage.lastAppUpdateCheck = null;
        DashboardPage.lastPluginUpdateCheck = null;
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

        if (msg.MessageType == "DashboardInfo") {
            DashboardPage.renderInfo(msg.Data);
        }
    },

    onWebSocketConnectionChange: function () {

        DashboardPage.stopInterval();
        DashboardPage.startInterval();
    },

    pollForInfo: function () {
        $.getJSON("dashboardInfo").done(DashboardPage.renderInfo);
    },

    renderInfo: function (dashboardInfo) {

        DashboardPage.lastDashboardInfo = dashboardInfo;

        DashboardPage.renderRunningTasks(dashboardInfo);
        DashboardPage.renderSystemInfo(dashboardInfo);
        DashboardPage.renderActiveConnections(dashboardInfo);

        Dashboard.hideLoadingMsg();
    },

    renderActiveConnections: function (dashboardInfo) {

        var page = $.mobile.activePage;

        var html = '';

        if (!dashboardInfo.ActiveConnections.length) {
            html += '<p>There are no users currently connected.</p>';
            $('#divConnections', page).html(html).trigger('create');
            return;
        }

        html += '<table class="tblConnections" style="border-collapse:collapse;">';

        for (var i = 0, length = dashboardInfo.ActiveConnections.length; i < length; i++) {

            var connection = dashboardInfo.ActiveConnections[i];

            var user = dashboardInfo.Users.filter(function (u) {
                return u.Id == connection.UserId;
            })[0];

            html += '<tr>';

            html += '<td style="text-align:center;">';
            html += DashboardPage.getClientType(connection);
            html += '</td>';

            html += '<td>';
            html += user.Name;
            html += '</td>';

            html += '<td>';
            html += connection.DeviceName;
            html += '</td>';

            html += '<td>';
            html += DashboardPage.getNowPlayingImage(connection.NowPlayingItem);
            html += '</td>';

            html += '<td>';
            html += DashboardPage.getNowPlayingText(connection, connection.NowPlayingItem);
            html += '</td>';

            html += '</tr>';

        }

        html += '</table>';

        $('#divConnections', page).html(html);
    },

    getClientType: function (connection) {

        var clientLowered = connection.Client.toLowerCase();
        
        if (clientLowered == "dashboard") {

            return "<img src='css/images/clients/html5.png' alt='Dashboard' title='Dashboard' />";
        }
        if (clientLowered == "mb-classic") {

            return "<img src='css/images/clients/mb.png' alt='Media Browser Classic' title='Media Browser Classic' />";
        }
        if (clientLowered == "media browser theater") {

            return "<img src='css/images/clients/mb.png' alt='Media Browser Theater' title='Media Browser Theater' />";
        }
        if (clientLowered == "android") {

            return "<img src='css/images/clients/android.png' alt='Android' title='Android' />";
        }
        if (clientLowered == "ios") {

            return "<img src='css/images/clients/ios.png' alt='iOS' title='iOS' />";
        }
        if (clientLowered == "windows rt") {

            return "<img src='css/images/clients/windowsrt.png' alt='Windows RT' title='Windows RT' />";
        }
        if (clientLowered == "windows phone") {

            return "<img src='css/images/clients/windowsphone.png' alt='Windows Phone' title='Windows Phone' />";
        }
        if (clientLowered == "dlna") {

            return "<img src='css/images/clients/dlna.png' alt='Dlna' title='Dlna' />";
        }

        return connection.Client;
    },

    getNowPlayingImage: function (item) {

        if (item) {

            if (item.PrimaryImageTag) {

                var url = ApiClient.getImageUrl(item.Id, {
                    type: "Primary",
                    height: 100,
                    tag: item.PrimaryImageTag
                });

                return "<img class='clientNowPlayingImage' src='" + url + "' alt='" + item.Name + "' title='" + item.Name + "' />";
            }
        }

        return "";
    },

    getNowPlayingText: function (connection, item) {

        var html = "";

        if (item) {

            html += "<div>" + item.Name + "</div>";

            html += "<div>";

            if (item.RunTimeTicks) {
                html += DashboardPage.getDisplayText(connection.NowPlayingPositionTicks || 0) + " / ";

                html += DashboardPage.getDisplayText(item.RunTimeTicks);
            }

            html += "</div>";
        }

        return html;
    },

    getDisplayText: function (ticks) {

        var ticksPerHour = 36000000000;

        var parts = [];

        var hours = ticks / ticksPerHour;
        hours = parseInt(hours);

        if (hours) {
            parts.push(hours);
        }

        ticks -= (hours * ticksPerHour);

        var ticksPerMinute = 600000000;

        var minutes = ticks / ticksPerMinute;
        minutes = parseInt(minutes);

        ticks -= (minutes * ticksPerMinute);

        if (minutes < 10 && hours) {
            minutes = '0' + minutes;
        }
        parts.push(minutes);

        var ticksPerSecond = 10000000;

        var seconds = ticks / ticksPerSecond;
        seconds = parseInt(seconds);

        if (seconds < 10) {
            seconds = '0' + seconds;
        }
        parts.push(seconds);

        return parts.join(':');
    },

    renderRunningTasks: function (dashboardInfo) {

        var page = $.mobile.activePage;

        var html = '';

        if (!dashboardInfo.RunningTasks.length) {
            html += '<p>No tasks are currently running.</p>';
        }

        for (var i = 0, length = dashboardInfo.RunningTasks.length; i < length; i++) {


            var task = dashboardInfo.RunningTasks[i];

            html += '<p>';

            html += task.Name;

            if (task.State == "Running") {
                var progress = (task.CurrentProgressPercentage || 0).toFixed(1);
                html += '<span style="color:#267F00;margin-right:5px;font-weight:bold;"> - ' + progress + '%</span>';

                html += '<button type="button" data-icon="stop" data-iconpos="notext" data-inline="true" data-theme="b" data-mini="true" onclick="DashboardPage.stopTask(\'' + task.Id + '\');">Stop</button>';
            }
            else if (task.State == "Cancelling") {
                html += '<span style="color:#cc0000;"> - Stopping</span>';
            }

            html += '</p>';
        }


        $('#divRunningTasks', page).html(html).trigger('create');
    },

    renderSystemInfo: function (dashboardInfo) {

        Dashboard.updateSystemInfo(dashboardInfo.SystemInfo);

        var page = $.mobile.activePage;

        $('#appVersionNumber', page).html(dashboardInfo.SystemInfo.Version);

        if (dashboardInfo.RunningTasks.filter(function (task) {

            return task.Id == dashboardInfo.ApplicationUpdateTaskId;

        }).length) {

            $('#btnUpdateApplication', page).button('disable');
        } else {
            $('#btnUpdateApplication', page).button('enable');
        }

        DashboardPage.renderApplicationUpdateInfo(dashboardInfo);
        DashboardPage.renderPluginUpdateInfo(dashboardInfo);
        DashboardPage.renderPendingInstallations(dashboardInfo.SystemInfo);
    },

    renderApplicationUpdateInfo: function (dashboardInfo) {

        var page = $.mobile.activePage;

        if (dashboardInfo.SystemInfo.IsNetworkDeployed && !dashboardInfo.SystemInfo.HasPendingRestart) {

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

                    $('#newVersionNumber', page).html("Version " + version.versionStr + " is now available for download.");
                }

            }).fail(function () {

                Dashboard.showFooterNotification({ html: '<img src="css/images/notifications/error.png" class="notificationIcon" />There was an error connecting to the remote Media Browser repository.', id: "MB3ConnectionError" });

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

                html += '<button type="button" data-icon="download" data-theme="b" onclick="DashboardPage.installPluginUpdate(this);" data-name="' + update.name + '" data-version="' + update.versionStr + '" data-classification="' + update.classification + '">Update Now</button>';
            }

            elem.html(html).trigger('create');

        }).fail(function () {

            Dashboard.showFooterNotification({ html: '<img src="css/images/notifications/error.png" class="notificationIcon" />There was an error connecting to the remote Media Browser repository.', id: "MB3ConnectionError" });

        });
    },

    installPluginUpdate: function (button) {

        $(button).button('disable');

        var name = button.getAttribute('data-name');
        var version = button.getAttribute('data-version');
        var classification = button.getAttribute('data-classification');

        Dashboard.showLoadingMsg();

        ApiClient.installPlugin(name, classification, version).done(function () {

            Dashboard.hideLoadingMsg();
        });
    },

    updateApplication: function () {

        var page = $.mobile.activePage;
        $('#btnUpdateApplication', page).button('disable');

        Dashboard.showLoadingMsg();

        ApiClient.startScheduledTask(DashboardPage.lastDashboardInfo.ApplicationUpdateTaskId).done(function () {

            DashboardPage.pollForInfo();

            Dashboard.hideLoadingMsg();
        });
    },

    stopTask: function (id) {

        ApiClient.stopScheduledTask(id).done(function () {

            DashboardPage.pollForInfo();
        });

    }
};

$(document).on('pageshow', "#dashboardPage", DashboardPage.onPageShow).on('pagehide', "#dashboardPage", DashboardPage.onPageHide);
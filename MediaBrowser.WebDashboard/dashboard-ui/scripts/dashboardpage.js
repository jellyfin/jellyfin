var DashboardPage = {

    newsStartIndex: 0,

    onPageShow: function () {

        var page = this;

        DashboardPage.newsStartIndex = 0;

        Dashboard.showLoadingMsg();
        DashboardPage.pollForInfo(page);
        DashboardPage.startInterval();

        $(ApiClient).on("websocketmessage", DashboardPage.onWebSocketMessage)
            .on("websocketopen", DashboardPage.onWebSocketConnectionChange)
            .on("websocketerror", DashboardPage.onWebSocketConnectionChange)
            .on("websocketclose", DashboardPage.onWebSocketConnectionChange);

        DashboardPage.lastAppUpdateCheck = null;
        DashboardPage.lastPluginUpdateCheck = null;

        Dashboard.getPluginSecurityInfo().done(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter) {
                $('#contribute', page).hide();
            } else {
                $('#contribute', page).show();
            }

        });

        DashboardPage.reloadSystemInfo(page);
        DashboardPage.reloadNews(page);
    },

    reloadSystemInfo: function (page) {

        ApiClient.getSystemInfo().done(function (systemInfo) {

            Dashboard.updateSystemInfo(systemInfo);

            $('#appVersionNumber', page).html('Version ' + systemInfo.Version);

            var port = systemInfo.HttpServerPortNumber;

            if (port == systemInfo.WebSocketPortNumber) {
                $('#ports', page).html('Running on port <b>' + port + '</b>');
            } else {
                $('#ports', page).html('Running on ports <b>' + port + '</b> and <b>' + systemInfo.WebSocketPortNumber + '</b>');
            }

            if (systemInfo.CanSelfRestart) {
                $('.btnRestartContainer', page).removeClass('hide');
            } else {
                $('.btnRestartContainer', page).addClass('hide');
            }

            DashboardPage.renderUrls(page, systemInfo);
            DashboardPage.renderPendingInstallations(page, systemInfo);

            if (systemInfo.CanSelfUpdate) {
                $('#btnUpdateApplicationContainer', page).show();
                $('#btnManualUpdateContainer', page).hide();
            } else {
                $('#btnUpdateApplicationContainer', page).hide();
                $('#btnManualUpdateContainer', page).show();
            }

            DashboardPage.renderHasPendingRestart(page, systemInfo.HasPendingRestart);
        });
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
            ApiClient.sendWebSocketMessage("SessionsStart", "0,1500");
            ApiClient.sendWebSocketMessage("ScheduledTasksInfoStart", "0,1500");
        }
    },

    stopInterval: function () {

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("SessionsStop");
            ApiClient.sendWebSocketMessage("ScheduledTasksInfoStop");
        }
    },

    onWebSocketMessage: function (e, msg) {

        var page = $.mobile.activePage;

        if (msg.MessageType == "Sessions") {
            DashboardPage.renderInfo(page, msg.Data);
        }
        else if (msg.MessageType == "RestartRequired") {
            DashboardPage.renderHasPendingRestart(page, true);
        }
        else if (msg.MessageType == "ServerShuttingDown") {
            DashboardPage.renderHasPendingRestart(page, false);
        }
        else if (msg.MessageType == "ServerRestarting") {
            DashboardPage.renderHasPendingRestart(page, false);
        }
        else if (msg.MessageType == "ScheduledTasksInfo") {

            var tasks = msg.Data;

            DashboardPage.renderRunningTasks(page, tasks);
        }
    },

    onWebSocketConnectionChange: function () {

        DashboardPage.stopInterval();
        DashboardPage.startInterval();
    },

    pollForInfo: function (page) {

        ApiClient.getSessions().done(function (sessions) {

            DashboardPage.renderInfo(page, sessions);
        });
    },

    renderInfo: function (page, sessions) {

        DashboardPage.renderActiveConnections(page, sessions);
        DashboardPage.renderPluginUpdateInfo(page);

        Dashboard.hideLoadingMsg();
    },

    renderActiveConnections: function (page, sessions) {

        var html = '';

        var parentElement = $('.activeDevices', page);

        $('.activeSession', parentElement).addClass('deadSession');

        for (var i = 0, length = sessions.length; i < length; i++) {

            var connection = sessions[i];

            var rowId = 'session' + connection.Id;

            var elem = $('#' + rowId, page);

            if (elem.length) {
                DashboardPage.updateSession(elem, connection);
                continue;
            }

            var nowPlayingItem = connection.NowPlayingItem;

            var className = nowPlayingItem ? 'activeSession' : 'notPlayingSession activeSession';

            html += '<a class="' + className + '" id="' + rowId + '" href="nowplaying.html">';

            html += '<div class="sessionNowPlayingContent"';

            var imgUrl = DashboardPage.getNowPlayingImageUrl(nowPlayingItem);

            if (imgUrl) {
                html += ' data-src="' + imgUrl + '" style="display:inline-block;background-image:url(\'' + imgUrl + '\');"';
            }

            html += '></div>';

            html += '<div class="sessionNowPlayingInnerContent">';

            html += '<div class="sessionAppInfo">';

            var clientImage = DashboardPage.getClientImage(connection);

            if (clientImage) {
                html += clientImage;
            }

            html += '<div class="sessionAppName" style="display:inline-block;">' + connection.DeviceName;
            html += '<br/>' + connection.ApplicationVersion;
            html += '</div>';

            html += '</div>';

            html += '<div class="sessionUserInfo">';

            var userImage = DashboardPage.getUserImage(connection);
            if (userImage) {
                html += '<div class="sessionUserImage" data-src="' + userImage + '">';
                html += '<img src="' + userImage + '" />';
            } else {
                html += '<div class="sessionUserImage">';
            }
            html += '</div>';

            html += '<div class="sessionUserName">';
            html += DashboardPage.getUsersHtml(connection);
            html += '</div>';

            html += '</div>';

            html += '<div class="sessionNowPlayingInfo">';
            if (nowPlayingItem) {
                html += DashboardPage.getNowPlayingName(connection);
            }
            html += '</div>';

            if (nowPlayingItem) {

                var value = (100 * connection.NowPlayingPositionTicks) / nowPlayingItem.RunTimeTicks;

                html += '<progress class="itemProgressBar" min="0" max="100" value="' + value + '"></progress>';
            } else {
                html += '<progress class="itemProgressBar" min="0" max="100" style="display:none;"></progress>';
            }

            html += '</div>';
            html += '</a>';

        }

        parentElement.append(html).trigger('create');

        $('.deadSession', parentElement).remove();
    },

    getNowPlayingName: function (session) {

        var nowPlayingItem = session.NowPlayingItem;
        
        if (!nowPlayingItem) {

            return 'Last seen ' + humane_date(session.LastActivityDate);
        }
        
        var topText = nowPlayingItem.Name;

        if (nowPlayingItem.MediaType == 'Video') {
            if (nowPlayingItem.IndexNumber != null) {
                topText = nowPlayingItem.IndexNumber + " - " + topText;
            }
            if (nowPlayingItem.ParentIndexNumber != null) {
                topText = nowPlayingItem.ParentIndexNumber + "." + topText;
            }
        }

        var bottomText = '';

        if (nowPlayingItem.Artists && nowPlayingItem.Artists.length) {
            bottomText = topText;
            topText = nowPlayingItem.Artists[0];
        }
        else if (nowPlayingItem.SeriesName || nowPlayingItem.Album) {
            bottomText = topText;
            topText = nowPlayingItem.SeriesName || nowPlayingItem.Album;
        }
        else if (nowPlayingItem.ProductionYear) {
            bottomText = nowPlayingItem.ProductionYear;
        }

        return bottomText ? topText + '<br/>' + bottomText : topText;

    },

    getUsersHtml: function (session) {

        var html = [];

        if (session.UserId) {
            html.push(session.UserName);
        }

        for (var i = 0, length = session.AdditionalUsers.length; i < length; i++) {

            html.push(session.AdditionalUsers[i].UserName);
        }

        return html.join(', ');
    },

    getUserImage: function (session) {

        if (session.UserId && session.UserPrimaryImageTag) {
            return ApiClient.getUserImageUrl(session.UserId, {
                
                tag: session.UserPrimaryImageTag,
                height: 24,
                type: 'Primary'

            });
        }

        return null;
    },

    updateSession: function (row, session) {

        row.removeClass('deadSession');

        var nowPlayingItem = session.NowPlayingItem;

        if (nowPlayingItem) {
            row.removeClass('notPlayingSession');
        } else {
            row.addClass('notPlayingSession');
        }

        $('.sessionUserName', row).html(DashboardPage.getUsersHtml(session));

        $('.sessionNowPlayingInfo', row).html(DashboardPage.getNowPlayingName(session));

        if (nowPlayingItem && nowPlayingItem.RunTimeTicks) {

            var value = (100 * session.NowPlayingPositionTicks) / nowPlayingItem.RunTimeTicks;

            $('progress', row).show().val(value);
        } else {
            $('progress', row).hide();
        }

        var imgUrl = DashboardPage.getNowPlayingImageUrl(nowPlayingItem) || '';
        var imgElem = $('.sessionNowPlayingContent', row)[0];

        if (imgUrl != imgElem.getAttribute('data-src')) {
            imgElem.style.backgroundImage = imgUrl ? 'url(\'' + imgUrl + '\')' : '';
            imgElem.setAttribute('data-src', imgUrl);
        }
    },

    getClientImage: function (connection) {

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

            return "<img src='css/images/clients/mbc.png' />";
        }
        if (clientLowered == "media browser theater") {

            return "<img src='css/images/clients/mb.png' />";
        }
        if (clientLowered == "android") {

            return "<img src='css/images/clients/android.png' />";
        }
        if (clientLowered == "roku") {

            return "<img src='css/images/clients/roku.jpg' />";
        }
        if (clientLowered == "ios") {

            return "<img src='css/images/clients/ios.png' />";
        }
        if (clientLowered == "windows rt") {

            return "<img src='css/images/clients/windowsrt.png' />";
        }
        if (clientLowered == "windows phone") {

            return "<img src='css/images/clients/windowsphone.png' />";
        }
        if (clientLowered == "dlna") {

            return "<img src='css/images/clients/dlna.png' />";
        }
        if (clientLowered == "mbkinect") {

            return "<img src='css/images/clients/mbkinect.png' />";
        }
        if (clientLowered == "xbmc") {
            return "<img src='css/images/clients/xbmc.png' />";
        }
        if (clientLowered == "chromecast") {

            return "<img src='css/images/chromecast/ic_media_route_on_holo_light.png' />";
        }

        return null;
    },

    getNowPlayingImageUrl: function (item) {

        if (item && item.BackdropImageTag) {

            return ApiClient.getImageUrl(item.BackdropItemId, {
                type: "Backdrop",
                width: 810,
                tag: item.BackdropImageTag
            });
        }

        if (item && item.ThumbImageTag) {

            return ApiClient.getImageUrl(item.ThumbItemId, {
                type: "Thumb",
                width: 810,
                tag: item.ThumbImageTag
            });
        }

        if (item && item.PrimaryImageTag) {

            return ApiClient.getImageUrl(item.PrimaryImageItemId, {
                type: "Primary",
                width: 810,
                tag: item.PrimaryImageTag
            });
        }

        return null;
    },

    systemUpdateTaskKey: "SystemUpdateTask",

    renderRunningTasks: function (page, tasks) {

        var html = '';

        tasks = tasks.filter(function (t) {
            return t.State != 'Idle';
        });

        if (tasks.filter(function (t) {

            return t.Key == DashboardPage.systemUpdateTaskKey;

        }).length) {

            $('#btnUpdateApplication', page).buttonEnabled(false);
        } else {
            $('#btnUpdateApplication', page).buttonEnabled(true);
        }

        if (!tasks.length) {
            html += '<p>No tasks are currently running.</p>';
            $('#runningTasksCollapsible', page).hide();
        } else {
            $('#runningTasksCollapsible', page).show();
        }

        for (var i = 0, length = tasks.length; i < length; i++) {


            var task = tasks[i];

            html += '<p>';

            html += task.Name + "<br/>";

            if (task.State == "Running") {
                var progress = (task.CurrentProgressPercentage || 0).toFixed(1);

                html += '<progress max="100" value="' + progress + '" title="' + progress + '%">';
                html += '' + progress + '%';
                html += '</progress>';

                html += "<span style='color:#009F00;margin-left:5px;margin-right:5px;'>" + progress + "%</span>";

                html += '<button type="button" data-icon="stop" data-iconpos="notext" data-inline="true" data-mini="true" onclick="DashboardPage.stopTask(\'' + task.Id + '\');">Stop</button>';
            }
            else if (task.State == "Cancelling") {
                html += '<span style="color:#cc0000;">Stopping</span>';
            }

            html += '</p>';
        }


        $('#divRunningTasks', page).html(html).trigger('create');
    },

    renderUrls: function (page, systemInfo) {

        var url = ApiClient.serverAddress() + "/mediabrowser";

        $('#bookmarkUrl', page).html(url).attr("href", url);

        if (systemInfo.WanAddress) {

            var externalUrl = systemInfo.WanAddress + "/mediabrowser";

            $('.externalUrl', page).html('Remote access: <a href="' + externalUrl + '" target="_blank">' + externalUrl + '</a>').show().trigger('create');
        } else {
            $('.externalUrl', page).hide();
        }
    },

    renderHasPendingRestart: function (page, hasPendingRestart) {

        $('#updateFail', page).hide();

        if (!hasPendingRestart) {

            // Only check once every 30 mins
            if (DashboardPage.lastAppUpdateCheck && (new Date().getTime() - DashboardPage.lastAppUpdateCheck) < 1800000) {
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

                $('#updateFail', page).show();

            });

        } else {

            $('#pUpToDate', page).hide();

            $('#pUpdateNow', page).hide();
        }
    },

    renderPendingInstallations: function (page, systemInfo) {

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

    renderPluginUpdateInfo: function (page) {

        // Only check once every 30 mins
        if (DashboardPage.lastPluginUpdateCheck && (new Date().getTime() - DashboardPage.lastPluginUpdateCheck) < 1800000) {
            return;
        }

        DashboardPage.lastPluginUpdateCheck = new Date().getTime();

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

        ApiClient.getScheduledTasks().done(function (tasks) {

            var task = tasks.filter(function (t) {

                return t.Key == DashboardPage.systemUpdateTaskKey;
            })[0];

            ApiClient.startScheduledTask(task.Id).done(function () {

                DashboardPage.pollForInfo(page);

                Dashboard.hideLoadingMsg();
            });
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
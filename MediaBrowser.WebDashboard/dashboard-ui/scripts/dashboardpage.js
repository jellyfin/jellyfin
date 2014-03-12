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

        var container = $('.connections', page);

        $('.sessionPosterItem', container).addClass('deadSession');

        var deviceId = ApiClient.deviceId();

        for (var i = 0, length = dashboardInfo.ActiveConnections.length; i < length; i++) {

            var connection = dashboardInfo.ActiveConnections[i];

            var itemId = 'session' + connection.Id;

            var elem = $('#' + itemId, page);

            if (elem.length) {
                DashboardPage.updateSession(elem, connection);
                continue;
            }

            html += '<div class="sessionPosterItem posterItem squarePosterItem" id="' + itemId + '" style="vertical-align:top;margin-bottom:2em;">';

            var nowPlayingItem = connection.NowPlayingItem;
            var imageUrl = DashboardPage.getNowPlayingImage(nowPlayingItem);

            var style = "";

            if (imageUrl) {
                style += 'background-image:url(\'' + imageUrl + '\');';
            }

            var onclick = connection.DeviceId == deviceId ? '' : ' onclick="RemoteControl.showMenu({sessionId:\'' + connection.Id + '\'});"';

            html += '<a' + onclick + ' data-imageurl="' + imageUrl + '" href="#" class="posterItemImage coveredPosterItemImage" style="' + style + 'background-color:#f2f2f2;display:block;">';

            var defaultTextStyle = '';

            if (nowPlayingItem) {
                defaultTextStyle = "display:none;";
            }
            html += '<div class="posterItemDefaultText" style="' + defaultTextStyle + '">Nothing currently playing</div>';

            html += '<div class="posterItemTextOverlay">';

            var itemNameStyle='';

            if (!nowPlayingItem) {
                itemNameStyle = "display:none;";
            }
            html += '<div class="posterItemText posterItemName" style="' + itemNameStyle + '">' + (nowPlayingItem ? nowPlayingItem.Name : '') + '</div>';

            var progressStyle='';

            if (!nowPlayingItem) {
                progressStyle = "display:none;";
            }
            html += "<div class='posterItemText posterItemProgress' style='" + progressStyle + "'>";

            html += '<progress class="itemProgressBar" min="0" max="100" value="' + DashboardPage.getPlaybackProgress(connection) + '" style="opacity:.9;"></progress>';
            html += "</div>";
            html += "</div>";

            html += '<img src="' + DashboardPage.getClientImage(connection) + '" style="top:10px;left:10px;height:24px;position:absolute;opacity: .95;" />';

            html += '</a>';

            html += '<div class="sessionItemText">' + DashboardPage.getSessionItemText(connection) + '</div>';

            //html += '<td class="clientType" style="text-align:center;">';
            //html += DashboardPage.getClientType(connection);
            //html += '</td>';

            //html += '<td>';

            //html += '<div>';

            //if (deviceId == connection.DeviceId) {
            //    html += connection.Client;
            //} else {
            //    html += '<a href="#" onclick="RemoteControl.showMenu({sessionId:\'' + connection.Id + '\'});">' + connection.Client + '</a>';
            //}
            //html += '</div>';

            //html += '</td>';

            //html += '<td class="nowPlayingImage">';
            //html += DashboardPage.getNowPlayingImage(nowPlayingItem);
            //html += '</td>';

            //html += '<td class="nowPlayingText">';
            //html += DashboardPage.getNowPlayingText(connection, nowPlayingItem);
            //html += '</td>';

            html += '</div>';
        }

        container.append(html).trigger('create');

        $('.deadSession', container).remove();
    },

    getPlaybackProgress: function (session) {

        if (session.NowPlayingItem) {
            if (session.NowPlayingItem.RunTimeTicks) {

                var pct = (session.NowPlayingPositionTicks || 0) / session.NowPlayingItem.RunTimeTicks;

                return pct * 100;
            }
        }

        return 0;
    },

    getUsersHtml: function (session) {

        var html = '<div>';

        if (session.UserId) {
            html += session.UserName;
        }

        html += session.AdditionalUsers.map(function (currentSession) {

            return ', ' + currentSession.UserName;
        });

        html += '</div>';

        return html;
    },

    updateSession: function (elem, session) {

        elem.removeClass('deadSession');

        $('.sessionItemText', elem).html(DashboardPage.getSessionItemText(session));

        var nowPlayingItem = session.NowPlayingItem;

        if (nowPlayingItem) {
            $('.posterItemDefaultText', elem).hide();
            $('.posterItemProgress', elem).show();
            $('.posterItemName', elem).show().html(nowPlayingItem.Name);

            $('progress', elem).val(DashboardPage.getPlaybackProgress(session));
        } else {
            $('.posterItemDefaultText', elem).show();
            $('.posterItemProgress', elem).hide();
            $('.posterItemName', elem).hide().html('');
        }

        var imageUrl = DashboardPage.getNowPlayingImage(nowPlayingItem);

        var image = $('.posterItemImage', elem)[0];

        if (imageUrl && imageUrl != image.getAttribute('data-imageurl')) {

            image.style.backgroundImage = 'url(\'' + imageUrl + '\')';
            image.setAttribute('data-imageurl', imageUrl);

        } else if (!imageUrl && image.getAttribute('data-imageurl')) {

            image.style.backgroundImage = null;
            image.setAttribute('data-imageurl', '');
        }
    },

    getSessionItemText: function (connection) {

        var html = '';
        
        html += '<div class="posterItemText">';
        html += DashboardPage.getUsersHtml(connection);
        html += '</div>';

        //html += '<div class="posterItemText">' + connection.Client + '</div>';
        //html += '<div class="posterItemText">' + connection.ApplicationVersion + '</div>';

        html += '<div class="posterItemText">' + connection.DeviceName + '</div>';

        return html;
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

            return imgUrl;
        }
        if (clientLowered == "mb-classic") {

            return "css/images/clients/mbc.png";
        }
        if (clientLowered == "media browser theater") {

            return "css/images/clients/mb.png";
        }
        if (clientLowered == "android") {

            return "css/images/clients/android.png";
        }
        if (clientLowered == "roku") {

            return "css/images/clients/roku.jpg";
        }
        if (clientLowered == "ios") {

            return "css/images/clients/ios.png";
        }
        if (clientLowered == "windows rt") {

            return "css/images/clients/windowsrt.png";
        }
        if (clientLowered == "windows phone") {

            return "css/images/clients/windowsphone.png";
        }
        if (clientLowered == "dlna") {

            return "css/images/clients/dlna.png";
        }
        if (clientLowered == "mbkinect") {

            return "css/images/clients/mbkinect.png";
        }
        if (clientLowered == "xbmc") {

            return "css/images/clients/xbmc.png";
        }

        return "css/images/clients/mb.png";
    },

    getNowPlayingImage: function (item) {

        if (item && item.ThumbItemId) {
            return ApiClient.getImageUrl(item.ThumbItemId, {
                type: "Thumb",
                height: 300,
                tag: item.ThumbImageTag
            });
        }

        if (item && item.BackdropItemId) {
            return ApiClient.getImageUrl(item.BackdropItemId, {
                type: "Backdrop",
                height: 300,
                tag: item.BackdropImageTag
            });
        }

        if (item && item.PrimaryImageTag) {
            return ApiClient.getImageUrl(item.Id, {
                type: "Primary",
                height: 300,
                tag: item.PrimaryImageTag
            });
        }

        return "";
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

        DashboardPage.renderUrls(page, dashboardInfo.SystemInfo);
        DashboardPage.renderApplicationUpdateInfo(page, dashboardInfo);
        DashboardPage.renderPluginUpdateInfo(page, dashboardInfo);
        DashboardPage.renderPendingInstallations(page, dashboardInfo.SystemInfo);
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

    renderApplicationUpdateInfo: function (page, dashboardInfo) {

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

    renderPluginUpdateInfo: function (page, dashboardInfo) {

        // Only check once every 10 mins
        if (DashboardPage.lastPluginUpdateCheck && (new Date().getTime() - DashboardPage.lastPluginUpdateCheck) < 600000) {
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
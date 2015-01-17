var DashboardPage = {

    newsStartIndex: 0,

    onPageShow: function () {

        var page = this;

        DashboardPage.newsStartIndex = 0;

        Dashboard.showLoadingMsg();
        DashboardPage.pollForInfo(page);
        DashboardPage.startInterval();

        $(ApiClient).on("websocketmessage", DashboardPage.onWebSocketMessage)
            .on("websocketopen", DashboardPage.onWebSocketOpen);

        DashboardPage.lastAppUpdateCheck = null;
        DashboardPage.lastPluginUpdateCheck = null;

        Dashboard.getPluginSecurityInfo().done(function (pluginSecurityInfo) {

            if (pluginSecurityInfo.IsMBSupporter) {
                $('#contribute', page).hide();
            } else {
                $('#contribute', page).show();
            }

            DashboardPage.renderSupporterIcon(page, pluginSecurityInfo);
        });

        DashboardPage.reloadSystemInfo(page);
        DashboardPage.reloadNews(page);
        DashboardPage.sessionUpdateTimer = setInterval(DashboardPage.refreshSessionsLocally, 60000);

        $('.activityItems', page).activityLogList();
    },

    onPageHide: function () {

        var page = this;

        $('.activityItems', page).activityLogList('destroy');

        $(ApiClient).off("websocketmessage", DashboardPage.onWebSocketMessage).off("websocketopen", DashboardPage.onWebSocketConnectionChange).off("websocketerror", DashboardPage.onWebSocketConnectionChange).off("websocketclose", DashboardPage.onWebSocketConnectionChange);
        DashboardPage.stopInterval();

        if (DashboardPage.sessionUpdateTimer) {
            clearInterval(DashboardPage.sessionUpdateTimer);
        }
    },

    renderPaths: function (page, systemInfo) {

        $('#cachePath', page).html(systemInfo.CachePath);
        $('#logPath', page).html(systemInfo.LogPath);
        $('#imagesByNamePath', page).html(systemInfo.ItemsByNamePath);
        $('#transcodingTemporaryPath', page).html(systemInfo.TranscodingTempPath);
        $('#metadataPath', page).html(systemInfo.InternalMetadataPath);
    },

    refreshSessionsLocally: function () {

        var list = DashboardPage.sessionsList;

        if (list) {
            console.log('refreshSessionsLocally');
            DashboardPage.renderActiveConnections($.mobile.activePage, list);
        }
    },

    reloadSystemInfo: function (page) {

        ApiClient.getSystemInfo().done(function (systemInfo) {

            Dashboard.updateSystemInfo(systemInfo);

            $('#appVersionNumber', page).html(Globalize.translate('LabelVersionNumber').replace('{0}', systemInfo.Version));

            var httpPort = systemInfo.HttpServerPortNumber;

            var portHtml = Globalize.translate('LabelRunningOnPort', '<b>' + httpPort + '</b>');

            if (systemInfo.UseHttps) {
                var httpsPort = systemInfo.HttpsPortNumber;
                portHtml += '<br>';
                portHtml += Globalize.translate('LabelRunningOnHttpsPort', '<b>' + httpsPort + '</b>');
            }

            $('#ports', page).html(portHtml);

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

            DashboardPage.renderPaths(page, systemInfo);
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

                itemHtml += '<div class="newsItemDescription">' + item.Description + '</div>';
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

    startInterval: function () {

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("SessionsStart", "0,1500");
            ApiClient.sendWebSocketMessage("ScheduledTasksInfoStart", "0,1000");
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
            DashboardPage.renderHasPendingRestart(page, true);
        }
        else if (msg.MessageType == "ServerRestarting") {
            DashboardPage.renderHasPendingRestart(page, true);
        }
        else if (msg.MessageType == "ScheduledTasksInfo") {

            var tasks = msg.Data;

            DashboardPage.renderRunningTasks(page, tasks);
        }
        else if (msg.MessageType == "PackageInstalling" || msg.MessageType == "PackageInstallationCompleted") {

            DashboardPage.pollForInfo(page, true);
            DashboardPage.reloadSystemInfo(page);
        }
    },

    onWebSocketOpen: function () {

        DashboardPage.startInterval();
    },

    pollForInfo: function (page, forceUpdate) {

        ApiClient.getSessions().done(function (sessions) {

            DashboardPage.renderInfo(page, sessions, forceUpdate);
        });
        ApiClient.getScheduledTasks().done(function (tasks) {

            DashboardPage.renderRunningTasks(page, tasks);
        });
    },

    renderInfo: function (page, sessions, forceUpdate) {

        DashboardPage.renderActiveConnections(page, sessions);
        DashboardPage.renderPluginUpdateInfo(page, forceUpdate);

        Dashboard.hideLoadingMsg();
    },

    renderActiveConnections: function (page, sessions) {

        var html = '';

        DashboardPage.sessionsList = sessions;

        var parentElement = $('.activeDevices', page);

        $('.card', parentElement).addClass('deadSession');

        for (var i = 0, length = sessions.length; i < length; i++) {

            var session = sessions[i];

            var rowId = 'session' + session.Id;

            var elem = $('#' + rowId, page);

            if (elem.length) {
                DashboardPage.updateSession(elem, session);
                continue;
            }

            var nowPlayingItem = session.NowPlayingItem;

            var className = nowPlayingItem ? 'card activeSession' : 'card activeSession';

            if (session.TranscodingInfo && session.TranscodingInfo.CompletionPercentage) {
                className += ' transcodingSession';
            }

            html += '<div class="' + className + '" id="' + rowId + '">';

            html += '<div class="cardBox">';
            html += '<div class="cardScalable">';

            html += '<div class="cardPadder"></div>';
            html += '<div class="cardContent">';

            html += '<div class="sessionNowPlayingContent"';

            var imgUrl = DashboardPage.getNowPlayingImageUrl(nowPlayingItem);

            if (imgUrl) {
                html += ' data-src="' + imgUrl + '" style="display:inline-block;background-image:url(\'' + imgUrl + '\');"';
            }

            html += '></div>';

            html += '<div class="sessionNowPlayingInnerContent">';

            html += '<div class="sessionAppInfo">';

            var clientImage = DashboardPage.getClientImage(session);

            if (clientImage) {
                html += clientImage;
            }

            html += '<div class="sessionAppName" style="display:inline-block;">';
            html += '<div class="sessionDeviceName">' + session.DeviceName + '</div>';
            html += '<div class="sessionAppSecondaryText">' + DashboardPage.getAppSecondaryText(session) + '</div>';
            html += '</div>';

            html += '</div>';

            html += '<div class="sessionUserInfo">';

            var userImage = DashboardPage.getUserImage(session);
            if (userImage) {
                html += '<div class="sessionUserImage" data-src="' + userImage + '">';
                html += '<img src="' + userImage + '" />';
            } else {
                html += '<div class="sessionUserImage">';
            }
            html += '</div>';

            html += '<div class="sessionUserName">';
            html += DashboardPage.getUsersHtml(session);
            html += '</div>';

            html += '</div>';

            var nowPlayingName = DashboardPage.getNowPlayingName(session);

            html += '<div class="sessionNowPlayingInfo" data-imgsrc="' + nowPlayingName.image + '">';
            html += nowPlayingName.html;
            html += '</div>';

            if (nowPlayingItem && nowPlayingItem.RunTimeTicks) {

                var position = session.PlayState.PositionTicks || 0;
                var value = (100 * position) / nowPlayingItem.RunTimeTicks;

                html += '<progress class="itemProgressBar playbackProgress" min="0" max="100" value="' + value + '"></progress>';
            } else {
                html += '<progress class="itemProgressBar playbackProgress" min="0" max="100" style="display:none;"></progress>';
            }

            if (session.TranscodingInfo && session.TranscodingInfo.CompletionPercentage) {

                html += '<progress class="itemProgressBar transcodingProgress" min="0" max="100" value="' + session.TranscodingInfo.CompletionPercentage.toFixed(1) + '"></progress>';
            } else {
                html += '<progress class="itemProgressBar transcodingProgress" min="0" max="100" style="display:none;"></progress>';
            }

            html += '</div>';

            html += '<div class="cardOverlayTarget">';

            html += '<div class="sessionNowPlayingStreamInfo">' + DashboardPage.getSessionNowPlayingStreamInfo(session) + '</div>';
            html += '<div class="sessionNowPlayingTime">' + DashboardPage.getSessionNowPlayingTime(session) + '</div>';

            if (session.TranscodingInfo && session.TranscodingInfo.Framerate) {

                html += '<div class="sessionTranscodingFramerate">' + session.TranscodingInfo.Framerate + ' fps</div>';
            } else {
                html += '<div class="sessionTranscodingFramerate"></div>';
            }
            html += '</div>';

            html += '</div>';

            // cardScalable
            html += '</div>';

            // cardBox
            html += '</div>';

            // card
            html += '</div>';

        }

        parentElement.append(html).createSessionItemMenus().trigger('create');

        $('.deadSession', parentElement).remove();
    },

    getSessionNowPlayingStreamInfo: function (session) {

        var html = '';

        html += '<div>';

        if (session.TranscodingInfo && session.TranscodingInfo.IsAudioDirect && session.TranscodingInfo.IsVideoDirect) {
            html += Globalize.translate('LabelPlayMethodDirectStream');
        }
        else if (session.PlayState.PlayMethod == 'Transcode') {
            html += Globalize.translate('LabelPlayMethodTranscoding');
        }
        else if (session.PlayState.PlayMethod == 'DirectStream') {
            html += Globalize.translate('LabelPlayMethodDirectStream');
        }
        else if (session.PlayState.PlayMethod == 'DirectPlay') {
            html += Globalize.translate('LabelPlayMethodDirectPlay');
        }

        html += '</div>';

        if (session.TranscodingInfo) {

            html += '<br/>';

            var line = [];

            if (session.TranscodingInfo.Container) {

                line.push(session.TranscodingInfo.Container);
            }
            if (session.TranscodingInfo.Bitrate) {

                if (session.TranscodingInfo.Bitrate > 1000000) {
                    line.push((session.TranscodingInfo.Bitrate / 1000000).toFixed(1) + ' Mbps');
                } else {
                    line.push(Math.floor(session.TranscodingInfo.Bitrate / 1000) + ' kbps');
                }
            }
            if (line.length) {

                html += '<div>' + line.join(' ') + '</div>';
            }

            if (session.TranscodingInfo.VideoCodec) {

                html += '<div>' + Globalize.translate('LabelVideoCodec').replace('{0}', session.TranscodingInfo.VideoCodec) + '</div>';
            }
            if (session.TranscodingInfo.AudioCodec && session.TranscodingInfo.AudioCodec != session.TranscodingInfo.Container) {

                html += '<div>' + Globalize.translate('LabelAudioCodec').replace('{0}', session.TranscodingInfo.AudioCodec) + '</div>';
            }

        }

        return html;
    },

    getSessionNowPlayingTime: function (session) {

        var html = '';

        if (session.PlayState.PositionTicks) {
            html += Dashboard.getDisplayTime(session.PlayState.PositionTicks);
        } else {
            html += '--:--:--';
        }

        html += ' / ';

        var nowPlayingItem = session.NowPlayingItem;

        if (nowPlayingItem && nowPlayingItem.RunTimeTicks) {
            html += Dashboard.getDisplayTime(nowPlayingItem.RunTimeTicks);
        } else {
            html += '--:--:--';
        }

        return html;
    },

    getAppSecondaryText: function (session) {

        return session.ApplicationVersion;
    },

    getNowPlayingName: function (session) {

        var imgUrl = '';

        var nowPlayingItem = session.NowPlayingItem;

        if (!nowPlayingItem) {

            return {
                html: 'Last seen ' + humane_date(session.LastActivityDate),
                image: imgUrl
            };
        }

        var topText = nowPlayingItem.Name;

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

        if (nowPlayingItem.LogoItemId) {

            imgUrl = ApiClient.getScaledImageUrl(nowPlayingItem.LogoItemId, {

                tag: session.LogoImageTag,
                maxHeight: 24,
                maxWidth: 130,
                type: 'Logo'

            });

            topText = '<img src="' + imgUrl + '" style="max-height:24px;max-width:130px;" />';
        }

        var text = bottomText ? topText + '<br/>' + bottomText : topText;

        return {
            html: text,
            image: imgUrl
        };
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
            row.addClass('playingSession');
        } else {
            row.removeClass('playingSession');
        }

        $('.sessionNowPlayingStreamInfo', row).html(DashboardPage.getSessionNowPlayingStreamInfo(session));
        $('.sessionNowPlayingTime', row).html(DashboardPage.getSessionNowPlayingTime(session));

        $('.sessionUserName', row).html(DashboardPage.getUsersHtml(session));

        $('.sessionAppSecondaryText', row).html(DashboardPage.getAppSecondaryText(session));

        $('.sessionTranscodingFramerate', row).html((session.TranscodingInfo && session.TranscodingInfo.Framerate) ? session.TranscodingInfo.Framerate + ' fps' : '');

        var nowPlayingName = DashboardPage.getNowPlayingName(session);
        var nowPlayingInfoElem = $('.sessionNowPlayingInfo', row);

        if (!nowPlayingName.image || nowPlayingName.image != nowPlayingInfoElem.attr('data-imgsrc')) {
            nowPlayingInfoElem.html(nowPlayingName.html);
            nowPlayingInfoElem.attr('data-imgsrc', nowPlayingName.image || '');
        }

        if (nowPlayingItem && nowPlayingItem.RunTimeTicks) {

            var position = session.PlayState.PositionTicks || 0;
            var value = (100 * position) / nowPlayingItem.RunTimeTicks;

            $('.playbackProgress', row).show().val(value);
        } else {
            $('.playbackProgress', row).hide();
        }

        if (session.TranscodingInfo && session.TranscodingInfo.CompletionPercentage) {

            row.addClass('transcodingSession');
            $('.transcodingProgress', row).show().val(session.TranscodingInfo.CompletionPercentage);
        } else {
            $('.transcodingProgress', row).hide();
            row.removeClass('transcodingSession');
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
        if (clientLowered == "android" || clientLowered == "androidtv") {

            return "<img src='css/images/clients/android.png' />";
        }
        if (clientLowered == "nuvue") {

            return "<img src='css/images/clients/nuvue.png' />";
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
        if (clientLowered == "kodi" || clientLowered == "xbmc") {
            return "<img src='css/images/clients/kodi.png' />";
        }
        if (clientLowered == "chromecast") {

            return "<img src='css/images/chromecast/ic_media_route_on_holo_light.png' />";
        }
        if (clientLowered == "chrome companion") {

            return "<img src='css/images/clients/chrome_companion.png' />";
        }


        return null;
    },

    getNowPlayingImageUrl: function (item) {

        if (item && item.BackdropImageTag) {

            return ApiClient.getScaledImageUrl(item.BackdropItemId, {
                type: "Backdrop",
                width: 275,
                tag: item.BackdropImageTag
            });
        }

        if (item && item.ThumbImageTag) {

            return ApiClient.getScaledImageUrl(item.ThumbItemId, {
                type: "Thumb",
                width: 275,
                tag: item.ThumbImageTag
            });
        }

        if (item && item.PrimaryImageTag) {

            return ApiClient.getScaledImageUrl(item.PrimaryImageItemId, {
                type: "Primary",
                width: 275,
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

                html += '<button type="button" data-icon="stop" data-iconpos="notext" data-inline="true" data-mini="true" onclick="DashboardPage.stopTask(\'' + task.Id + '\');">' + Globalize.translate('ButtonStop') + '</button>';
            }
            else if (task.State == "Cancelling") {
                html += '<span style="color:#cc0000;">' + Globalize.translate('LabelStopping') + '</span>';
            }

            html += '</p>';
        }


        $('#divRunningTasks', page).html(html).trigger('create');
    },

    renderUrls: function (page, systemInfo) {

        if (systemInfo.WanAddress) {

            var externalUrl = systemInfo.WanAddress + ApiClient.apiPrefix();

            var remoteAccessHtml = Globalize.translate('LabelRemoteAccessUrl').replace('{0}', '<a href="' + externalUrl + '" target="_blank">' + externalUrl + '</a>');

            $('.externalUrl', page).html(remoteAccessHtml).show().trigger('create');
        } else {
            $('.externalUrl', page).hide();
        }
    },

    renderSupporterIcon: function (page, pluginSecurityInfo) {

        var imgUrl, text;

        if (pluginSecurityInfo.IsMBSupporter) {

            imgUrl = "css/images/supporter/supporterbadge.png";
            text = "Thank you for supporting Media Browser.";

            $('.supporterIconContainer', page).html('<a class="imageLink supporterIcon" href="supporter.html" title="' + text + '"><img src="' + imgUrl + '" style="height:32px;vertical-align: middle; margin-right: .5em;" /></a><span style="position:relative;top:2px;text-decoration:none;">' + text + '</span>');
        } else {

            imgUrl = "css/images/supporter/nonsupporterbadge.png";
            text = "Please support Media Browser.";

            $('.supporterIconContainer', page).html('<a class="imageLink supporterIcon" href="supporter.html" title="' + text + '"><img src="' + imgUrl + '" style="height:32px;vertical-align: middle; margin-right: .5em;" /><span style="position:relative;top:2px;text-decoration:none;">' + text + '</span></a>');
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

                    $('#newVersionNumber', page).html(Globalize.translate('VersionXIsAvailableForDownload').replace('{0}', version.versionStr));
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

    renderPluginUpdateInfo: function (page, forceUpdate) {

        // Only check once every 30 mins
        if (!forceUpdate && DashboardPage.lastPluginUpdateCheck && (new Date().getTime() - DashboardPage.lastPluginUpdateCheck) < 1800000) {
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

                html += '<p><strong>' + Globalize.translate('NewVersionOfSomethingAvailable').replace('{0}', update.name) + '</strong></p>';

                html += '<button type="button" data-icon="arrow-d" data-theme="b" onclick="DashboardPage.installPluginUpdate(this);" data-name="' + update.name + '" data-guid="' + update.guid + '" data-version="' + update.versionStr + '" data-classification="' + update.classification + '">' + Globalize.translate('ButtonUpdateNow') + '</button>';
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

        Dashboard.confirm(Globalize.translate('MessageConfirmRestart'), Globalize.translate('HeaderRestart'), function (result) {

            if (result) {
                $('#btnRestartServer').buttonEnabled(false);
                $('#btnShutdown').buttonEnabled(false);
                Dashboard.restartServer();
            }

        });
    },

    shutdown: function () {

        Dashboard.confirm(Globalize.translate('MessageConfirmShutdown'), Globalize.translate('HeaderShutdown'), function (result) {

            if (result) {
                $('#btnRestartServer').buttonEnabled(false);
                $('#btnShutdown').buttonEnabled(false);
                ApiClient.shutdownServer();
            }

        });
    }
};

$(document).on('pagebeforeshow', "#dashboardPage", DashboardPage.onPageShow)
    .on('pagehide', "#dashboardPage", DashboardPage.onPageHide);

(function ($, document, window) {

    var showOverlayTimeout;

    function onHoverOut() {

        if (showOverlayTimeout) {
            clearTimeout(showOverlayTimeout);
            showOverlayTimeout = null;
        }

        $('.cardOverlayTarget:visible', this).each(function () {

            var elem = this;

            $(this).animate({ "height": "0" }, "fast", function () {

                $(elem).hide();

            });

        });

        $('.cardOverlayTarget:visible', this).stop().animate({ "height": "0" }, function () {

            $(this).hide();

        });
    }

    $.fn.createSessionItemMenus = function () {

        function onShowTimerExpired(elem) {

            if ($('.itemSelectionPanel:visible', elem).length) {
                return;
            }

            var innerElem = $('.cardOverlayTarget', elem);

            innerElem.show().each(function () {

                this.style.height = 0;

            }).animate({ "height": "100%" }, "fast");
        }

        function onHoverIn() {

            if (showOverlayTimeout) {
                clearTimeout(showOverlayTimeout);
                showOverlayTimeout = null;
            }

            var elem = this;

            showOverlayTimeout = setTimeout(function () {

                onShowTimerExpired(elem);

            }, 1000);
        }

        // https://hacks.mozilla.org/2013/04/detecting-touch-its-the-why-not-the-how/

        if (('ontouchstart' in window) || (navigator.maxTouchPoints > 0) || (navigator.msMaxTouchPoints > 0)) {
            /* browser with either Touch Events of Pointer Events
               running on touch-capable device */
            return this;
        }

        return this.off('.sessionItemMenu').on('mouseenter.sessionItemMenu', '.playingSession', onHoverIn)
            .on('mouseleave.sessionItemMenu', '.playingSession', onHoverOut);
    };

})(jQuery, document, window);


(function ($, document, window) {

    function getEntryHtml(entry) {

        var html = '';

        html += '<div class="newsItem" style="padding: .5em 0;">';

        html += '<div class="notificationContent" style="display:block;">';

        var date = parseISO8601Date(entry.Date, { toLocal: true });

        var color = entry.Severity == 'Error' || entry.Severity == 'Fatal' || entry.Severity == 'Warn' ? '#cc0000' : 'green';

        html += '<div style="margin: 0;color:' + color + ';">';
        if (entry.UserId && entry.UserPrimaryImageTag) {

            var userImgUrl = ApiClient.getUserImageUrl(entry.UserId, {
                type: 'Primary',
                tag: entry.UserPrimaryImageTag,
                height: 20
            });
            html += '<img src="' + userImgUrl + '" style="height:20px;vertical-align:middle;margin-right:5px;" />';
        }

        html += date.toLocaleDateString() + ' ' + date.toLocaleTimeString().toLowerCase();
        html += '</div>';

        html += '<div class="notificationName" style="margin:.5em 0 0;white-space:nowrap;">';
        html += entry.Name;
        html += '</div>';

        entry.ShortOverview = entry.ShortOverview || '&nbsp;';

        if (entry.ShortOverview) {

            html += '<div class="newsItemDescription" style="margin: .5em 0 0;">';

            if (entry.Overview) {
                html += '<a href="#" class="btnShowOverview" style="text-decoration:none;font-weight:500;">';
            }
            html += entry.ShortOverview;
            if (entry.Overview) {
                html += '</a>';
            }

            html += '</div>';

            if (entry.Overview) {
                html += '<div class="newsItemLongDescription" style="display:none;">' + entry.Overview + '</div>';
            }
        }

        //if (notification.Url) {
        //    html += '<p style="margin: .25em 0;"><a href="' + notification.Url + '" target="_blank">' + Globalize.translate('ButtonMoreInformation') + '</a></p>';
        //}

        html += '</div>';

        html += '</div>';

        return html;
    }

    function renderList(elem, result, startIndex, limit) {

        var html = result.Items.map(getEntryHtml).join('');

        if (result.TotalRecordCount > limit) {

            var query = { StartIndex: startIndex, Limit: limit };

            html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, false, limit, false);
        }

        $(elem).html(html).trigger('create');

        $('.btnNextPage', elem).on('click', function () {
            reloadData(elem, startIndex + limit, limit);
        });

        $('.btnPreviousPage', elem).on('click', function () {
            reloadData(elem, startIndex - limit, limit);
        });

        $('.btnShowOverview', elem).on('click', function () {

            var item = $(this).parents('.newsItem');
            var overview = $('.newsItemLongDescription', item).html();
            var name = $('.notificationName', item).html();

            Dashboard.alert({
                message: '<div style="max-height:300px; overflow: auto;">' + overview + '</div>',
                title: name
            });
        });
    }

    function reloadData(elem, startIndex, limit) {

        if (startIndex == null) {
            startIndex = parseInt(elem.getAttribute('data-activitystartindex') || '0');
        }

        limit = limit || parseInt(elem.getAttribute('data-activitylimit') || '7');

        // Show last 24 hours
        var minDate = new Date();
        minDate.setTime(minDate.getTime() - 86400000);

        ApiClient.getJSON(ApiClient.getUrl('System/ActivityLog/Entries', {

            startIndex: startIndex,
            limit: limit,
            minDate: minDate.toISOString()

        })).done(function (result) {

            elem.setAttribute('data-activitystartindex', startIndex);
            elem.setAttribute('data-activitylimit', limit);

            renderList(elem, result, startIndex, limit);
        });
    }

    function createList(elem) {

        elem.each(function () {

            reloadData(this);
        });

        $(ApiClient).on('websocketmessage.activityloglistener', function (e, data) {

            var msg = data;

            if (msg.MessageType === "ActivityLogEntry") {
                elem.each(function () {

                    reloadData(this);
                });
            }

        }).on('websocketopen.activityloglistener', function (e, data) {

            startListening();
        });
    }

    function startListening() {

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("ActivityLogEntryStart", "0,1500");
        }

    }

    function stopListening() {

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("ActivityLogEntryStop", "0,1500");
        }

    }

    function destroyList(elem) {

        $(ApiClient).off('websocketopen.activityloglistener').off('websocketmessage.activityloglistener');

        stopListening();

        return this;
    }

    $.fn.activityLogList = function (action) {

        if (action == 'destroy') {
            destroyList(this);
        } else {
            createList(this);
        }

        startListening();

        return this;
    };

})(jQuery, document, window);

(function ($, document, window) {

    var welcomeDismissValue = '10';
    var welcomeTourKey = 'welcomeTour';

    function dismissWelcome(page, userId) {

        ApiClient.getDisplayPreferences('dashboard', userId, 'dashboard').done(function (result) {

            result.CustomPrefs[welcomeTourKey] = welcomeDismissValue;
            ApiClient.updateDisplayPreferences('dashboard', result, userId, 'dashboard');

            $(page).off('pagebeforeshow.checktour');
        });
    }

    function showWelcomeIfNeeded(page) {

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getDisplayPreferences('dashboard', userId, 'dashboard').done(function (result) {

            if (result.CustomPrefs[welcomeTourKey] == welcomeDismissValue) {
                $('.welcomeMessage', page).hide();
            } else {

                var elem = $('.welcomeMessage', page).show();

                if (result.CustomPrefs[welcomeTourKey]) {

                    $('.tourHeader', elem).html(Globalize.translate('HeaderWelcomeBack'));
                    $('.tourButtonText', elem).html(Globalize.translate('ButtonTakeTheTourToSeeWhatsNew'));

                } else {
                    
                    $('.tourHeader', elem).html(Globalize.translate('HeaderWelcomeToMediaBrowserServerDashboard'));
                    $('.tourButtonText', elem).html(Globalize.translate('ButtonTakeTheTour'));
                }
            }
        });
    }

    function takeTour(page, userId) {

        $.swipebox([
                { href: 'css/images/tour/dashboard/dashboard.png', title: Globalize.translate('DashboardTourDashboard') },
                { href: 'css/images/tour/dashboard/help.png', title: Globalize.translate('DashboardTourHelp') },
                { href: 'css/images/tour/dashboard/users.png', title: Globalize.translate('DashboardTourUsers') },
                { href: 'css/images/tour/dashboard/cinemamode.png', title: Globalize.translate('DashboardTourCinemaMode') },
                { href: 'css/images/tour/dashboard/chapters.png', title: Globalize.translate('DashboardTourChapters') },
                { href: 'css/images/tour/dashboard/subtitles.png', title: Globalize.translate('DashboardTourSubtitles') },
                { href: 'css/images/tour/dashboard/plugins.png', title: Globalize.translate('DashboardTourPlugins') },
                { href: 'css/images/tour/dashboard/notifications.png', title: Globalize.translate('DashboardTourNotifications') },
                { href: 'css/images/tour/dashboard/scheduledtasks.png', title: Globalize.translate('DashboardTourScheduledTasks') },
                { href: 'css/images/tour/dashboard/mobile.png', title: Globalize.translate('DashboardTourMobile') },
                { href: 'css/images/tour/enjoy.jpg', title: Globalize.translate('MessageEnjoyYourStay') }
        ], {
            afterClose: function () {
                dismissWelcome(page, userId);
                $('.welcomeMessage', page).hide();
            },
            hideBarsDelay: 30000
        });
    }

    $(document).on('pageinit', "#dashboardPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        $('.btnTakeTour', page).on('click', function () {
            takeTour(page, userId);
        });

    }).on('pagebeforeshow.checktour', "#dashboardPage", function () {

        var page = this;

        showWelcomeIfNeeded(page);

    });

})(jQuery, document, window);
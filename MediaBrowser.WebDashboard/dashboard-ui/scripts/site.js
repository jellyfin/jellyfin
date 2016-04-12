(function () {

    function onOneDocumentClick() {

        document.removeEventListener('click', onOneDocumentClick);

        if (window.Notification) {
            Notification.requestPermission();
        }
    }
    document.addEventListener('click', onOneDocumentClick);

})();

// Compatibility
window.Logger = {
    log: function (msg) {
        console.log(msg);
    }
};

var Dashboard = {

    isConnectMode: function () {

        if (AppInfo.isNativeApp) {
            return true;
        }

        var url = window.location.href.toLowerCase();

        return url.indexOf('mediabrowser.tv') != -1 ||
            url.indexOf('emby.media') != -1;
    },

    isRunningInCordova: function () {

        return window.appMode == 'cordova';
    },

    onRequestFail: function (e, data) {

        if (data.status == 401) {

            var url = data.url.toLowerCase();

            // Don't bounce to login on failures to contact our external servers
            if (url.indexOf('emby.media') != -1 || url.indexOf('mb3admin.com') != -1) {
                Dashboard.hideLoadingMsg();
                return;
            }

            // Don't bounce if the failure is in a sync service
            if (url.indexOf('/sync') != -1) {
                Dashboard.hideLoadingMsg();
                return;
            }

            // Bounce to the login screen, but not if a password entry fails, obviously
            if (url.indexOf('/password') == -1 &&
                url.indexOf('/authenticate') == -1 &&
                !$($.mobile.activePage).is('.standalonePage')) {

                if (data.errorCode == "ParentalControl") {

                    Dashboard.alert({
                        message: Globalize.translate('MessageLoggedOutParentalControl'),
                        callback: function () {
                            Dashboard.logout(false);
                        }
                    });

                } else {
                    Dashboard.logout(false);
                }
            }
            return;
            Dashboard.hideLoadingMsg();
        }
    },

    onPopupOpen: function () {
        Dashboard.popupCount = (Dashboard.popupCount || 0) + 1;
        document.body.classList.add('bodyWithPopupOpen');
    },

    onPopupClose: function () {

        Dashboard.popupCount = (Dashboard.popupCount || 1) - 1;

        if (!Dashboard.popupCount) {
            document.body.classList.remove('bodyWithPopupOpen');
        }
    },

    getCurrentUser: function () {

        return window.ApiClient.getCurrentUser();
    },

    serverAddress: function () {

        if (Dashboard.isConnectMode()) {
            var apiClient = window.ApiClient;

            if (apiClient) {
                return apiClient.serverAddress();
            }

            return null;
        }

        // Try to get the server address from the browser url
        // This will preserve protocol, hostname, port and subdirectory
        var urlLower = window.location.href.toLowerCase();
        var index = urlLower.lastIndexOf('/web');

        if (index != -1) {
            return urlLower.substring(0, index);
        }

        // If the above failed, just piece it together manually
        var loc = window.location;

        var address = loc.protocol + '//' + loc.hostname;

        if (loc.port) {
            address += ':' + loc.port;
        }

        return address;
    },

    getCurrentUserId: function () {

        var apiClient = window.ApiClient;

        if (apiClient) {
            return apiClient.getCurrentUserId();
        }

        return null;
    },

    onServerChanged: function (userId, accessToken, apiClient) {

        apiClient = apiClient || window.ApiClient;

        window.ApiClient = apiClient;
    },

    logout: function (logoutWithServer) {

        function onLogoutDone() {

            var loginPage;

            if (Dashboard.isConnectMode()) {
                loginPage = 'connectlogin.html';
                window.ApiClient = null;
            } else {
                loginPage = 'login.html';
            }
            Dashboard.navigate(loginPage);
        }

        if (logoutWithServer === false) {
            onLogoutDone();
        } else {
            ConnectionManager.logout().then(onLogoutDone);
        }
    },

    importCss: function (url) {

        var originalUrl = url;
        url += "?v=" + AppInfo.appVersion;

        if (!Dashboard.importedCss) {
            Dashboard.importedCss = [];
        }

        if (Dashboard.importedCss.indexOf(url) != -1) {
            return;
        }

        Dashboard.importedCss.push(url);

        if (document.createStyleSheet) {
            document.createStyleSheet(url);
        } else {
            var link = document.createElement('link');
            link.setAttribute('rel', 'stylesheet');
            link.setAttribute('data-url', originalUrl);
            link.setAttribute('type', 'text/css');
            link.setAttribute('href', url);
            document.head.appendChild(link);
        }
    },

    removeStylesheet: function (url) {

        var elem = document.querySelector('link[data-url=\'' + url + '\']');
        if (elem) {
            elem.parentNode.removeChild(elem);
        }
    },

    updateSystemInfo: function (info) {

        Dashboard.lastSystemInfo = info;

        if (!Dashboard.initialServerVersion) {
            Dashboard.initialServerVersion = info.Version;
        }

        if (info.HasPendingRestart) {

            Dashboard.hideDashboardVersionWarning();

            Dashboard.getCurrentUser().then(function (currentUser) {

                if (currentUser.Policy.IsAdministrator) {
                    Dashboard.showServerRestartWarning(info);
                }
            });

        } else {

            Dashboard.hideServerRestartWarning();

            if (Dashboard.initialServerVersion != info.Version) {

                Dashboard.showDashboardRefreshNotification();
            }
        }

        Dashboard.showInProgressInstallations(info.InProgressInstallations);
    },

    showInProgressInstallations: function (installations) {

        installations = installations || [];

        for (var i = 0, length = installations.length; i < length; i++) {

            var installation = installations[i];

            var percent = installation.PercentComplete || 0;

            if (percent < 100) {
                Dashboard.showPackageInstallNotification(installation, "progress");
            }
        }

        if (installations.length) {

            Dashboard.ensureInstallRefreshInterval();
        } else {
            Dashboard.stopInstallRefreshInterval();
        }
    },

    ensureInstallRefreshInterval: function () {

        if (!Dashboard.installRefreshInterval) {

            if (ApiClient.isWebSocketOpen()) {
                ApiClient.sendWebSocketMessage("SystemInfoStart", "0,500");
            }
            Dashboard.installRefreshInterval = 1;
        }
    },

    stopInstallRefreshInterval: function () {

        if (Dashboard.installRefreshInterval) {
            if (ApiClient.isWebSocketOpen()) {
                ApiClient.sendWebSocketMessage("SystemInfoStop");
            }
            Dashboard.installRefreshInterval = null;
        }
    },

    cancelInstallation: function (id) {

        ApiClient.cancelPackageInstallation(id).then(Dashboard.refreshSystemInfoFromServer, Dashboard.refreshSystemInfoFromServer);

    },

    showServerRestartWarning: function (systemInfo) {

        if (AppInfo.isNativeApp) {
            return;
        }

        var html = '<span style="margin-right: 1em;">' + Globalize.translate('MessagePleaseRestart') + '</span>';

        if (systemInfo.CanSelfRestart) {
            html += '<paper-button raised class="submit mini" onclick="this.disabled=\'disabled\';Dashboard.restartServer();"><iron-icon icon="refresh"></iron-icon><span>' + Globalize.translate('ButtonRestart') + '</span></paper-button>';
        }

        Dashboard.showFooterNotification({ id: "serverRestartWarning", html: html, forceShow: true, allowHide: false });
    },

    hideServerRestartWarning: function () {

        var elem = document.getElementById('serverRestartWarning');
        if (elem) {
            elem.parentNode.removeChild(elem);
        }
    },

    showDashboardRefreshNotification: function () {

        if (AppInfo.isNativeApp) {
            return;
        }

        var html = '<span style="margin-right: 1em;">' + Globalize.translate('MessagePleaseRefreshPage') + '</span>';

        html += '<paper-button raised class="submit mini" onclick="this.disabled=\'disabled\';Dashboard.reloadPage();"><iron-icon icon="refresh"></iron-icon><span>' + Globalize.translate('ButtonRefresh') + '</span></paper-button>';

        Dashboard.showFooterNotification({ id: "dashboardVersionWarning", html: html, forceShow: true, allowHide: false });
    },

    reloadPage: function () {

        window.location.reload(true);
    },

    hideDashboardVersionWarning: function () {

        var elem = document.getElementById('dashboardVersionWarning');

        if (elem) {

            elem.parentNode.removeChild(elem);
        }
    },

    showFooterNotification: function (options) {

        var removeOnHide = !options.id;

        options.id = options.id || "notification" + new Date().getTime() + parseInt(Math.random());

        if (!document.querySelector(".footer")) {

            var footerHtml = '<div id="footer" class="footer" data-theme="b" class="ui-bar-b">';

            footerHtml += '<div id="footerNotifications"></div>';
            footerHtml += '</div>';

            $(document.body).append(footerHtml);

        }

        var footer = $(".footer").css("top", "initial").show();

        var parentElem = $('#footerNotifications', footer);

        var elem = $('#' + options.id, parentElem);

        if (!elem.length) {
            elem = $('<p id="' + options.id + '" class="footerNotification"></p>').appendTo(parentElem);
        }

        var onclick = removeOnHide ? "jQuery(\"#" + options.id + "\").trigger(\"notification.remove\").remove();" : "jQuery(\"#" + options.id + "\").trigger(\"notification.hide\").hide();";

        if (options.allowHide !== false) {
            options.html += "<span style='margin-left: 1em;'><paper-button class='submit' onclick='" + onclick + "'>" + Globalize.translate('ButtonHide') + "</paper-button></span>";
        }

        if (options.forceShow) {
            elem.show();
        }

        elem.html(options.html);

        if (options.timeout) {

            setTimeout(function () {

                if (removeOnHide) {
                    elem.trigger("notification.remove").remove();
                } else {
                    elem.trigger("notification.hide").hide();
                }

            }, options.timeout);
        }

        footer.on("notification.remove notification.hide", function (e) {

            setTimeout(function () { // give the DOM time to catch up

                if (!parentElem.html()) {
                    footer.hide();
                }

            }, 50);

        });
    },

    getConfigurationPageUrl: function (name) {
        return "configurationpage?name=" + encodeURIComponent(name);
    },

    navigate: function (url, preserveQueryString) {

        if (!url) {
            throw new Error('url cannot be null or empty');
        }

        var queryString = getWindowLocationSearch();
        if (preserveQueryString && queryString) {
            url += queryString;
        }

        if (url.indexOf('/') != 0) {
            if (url.indexOf('http') != 0 && url.indexOf('file:') != 0) {
                url = '/' + url;
            }
        }
        Emby.Page.show(url);
    },

    showLoadingMsg: function () {

        Dashboard.loadingVisible = true;

        require(['loading'], function (loading) {
            if (Dashboard.loadingVisible) {
                loading.show();
            } else {
                loading.hide();
            }
        });
    },

    hideLoadingMsg: function () {

        Dashboard.loadingVisible = false;

        require(['loading'], function (loading) {
            if (Dashboard.loadingVisible) {
                loading.show();
            } else {
                loading.hide();
            }
        });
    },

    getModalLoadingMsg: function () {

        var elem = document.querySelector('.modalLoading');

        if (!elem) {

            elem = document.createElement('modalLoading');
            elem.classList.add('modalLoading');
            elem.classList.add('hide');
            document.body.appendChild(elem);

        }

        return elem;
    },

    showModalLoadingMsg: function () {
        Dashboard.getModalLoadingMsg().classList.remove('hide');
        Dashboard.showLoadingMsg();
    },

    hideModalLoadingMsg: function () {
        Dashboard.getModalLoadingMsg().classList.add('hide');
        Dashboard.hideLoadingMsg();
    },

    processPluginConfigurationUpdateResult: function () {

        Dashboard.hideLoadingMsg();

        require(['toast'], function (toast) {
            toast(Globalize.translate('MessageSettingsSaved'));
        });
    },

    processServerConfigurationUpdateResult: function (result) {

        Dashboard.hideLoadingMsg();

        require(['toast'], function (toast) {
            toast(Globalize.translate('MessageSettingsSaved'));
        });
    },

    alert: function (options) {

        if (typeof options == "string") {

            require(['toast'], function (toast) {

                toast({
                    text: options
                });

            });

            return;
        }

        require(['alert'], function (alert) {
            alert({
                title: options.title || Globalize.translate('HeaderAlert'),
                text: options.message
            }).then(options.callback || function () { });
        });
    },

    refreshSystemInfoFromServer: function () {

        var apiClient = ApiClient;

        if (apiClient && apiClient.accessToken()) {
            if (AppInfo.enableFooterNotifications) {
                apiClient.getSystemInfo().then(function (info) {

                    Dashboard.updateSystemInfo(info);
                });
            }
        }
    },

    restartServer: function () {

        Dashboard.suppressAjaxErrors = true;
        Dashboard.showLoadingMsg();

        ApiClient.restartServer().then(function () {

            setTimeout(function () {
                Dashboard.reloadPageWhenServerAvailable();
            }, 250);

        }, function () {
            Dashboard.suppressAjaxErrors = false;
        });
    },

    reloadPageWhenServerAvailable: function (retryCount) {

        // Don't use apiclient method because we don't want it reporting authentication under the old version
        ApiClient.getJSON(ApiClient.getUrl("System/Info")).then(function (info) {

            // If this is back to false, the restart completed
            if (!info.HasPendingRestart) {
                Dashboard.reloadPage();
            } else {
                Dashboard.retryReload(retryCount);
            }

        }, function () {
            Dashboard.retryReload(retryCount);
        });
    },

    retryReload: function (retryCount) {
        setTimeout(function () {

            retryCount = retryCount || 0;
            retryCount++;

            if (retryCount < 10) {
                Dashboard.reloadPageWhenServerAvailable(retryCount);
            } else {
                Dashboard.suppressAjaxErrors = false;
            }
        }, 500);
    },

    showUserFlyout: function () {

        Dashboard.navigate('mypreferencesmenu.html?userId=' + ApiClient.getCurrentUserId());
    },

    getPluginSecurityInfo: function () {

        var apiClient = ApiClient;

        if (!apiClient) {

            return Promise.reject();
        }

        var cachedInfo = Dashboard.pluginSecurityInfo;
        if (cachedInfo) {
            return Promise.resolve(cachedInfo);
        }

        return apiClient.ajax({
            type: "GET",
            url: apiClient.getUrl("Plugins/SecurityInfo"),
            dataType: 'json',

            error: function () {
                // Don't show normal dashboard errors
            }

        }).then(function (result) {
            Dashboard.pluginSecurityInfo = result;
            return result;
        });
    },

    resetPluginSecurityInfo: function () {
        Dashboard.pluginSecurityInfo = null;
    },

    ensureHeader: function (page) {

        if (page.classList.contains('standalonePage') && !page.classList.contains('noHeaderPage')) {

            Dashboard.renderHeader(page);
        }
    },

    renderHeader: function (page) {

        var header = page.querySelector('.header');

        if (!header) {
            var headerHtml = '';

            headerHtml += '<div class="header">';

            headerHtml += '<a class="logo" href="home.html" style="text-decoration:none;font-size: 22px;">';

            if (page.classList.contains('standalonePage')) {

                headerHtml += '<img class="imgLogoIcon" src="css/images/mblogoicon.png" />';
                headerHtml += '<span class="logoLibraryMenuButtonText">EMBY</span>';
            }

            headerHtml += '</a>';

            headerHtml += '</div>';
            $(page).prepend(headerHtml);
        }
    },

    getToolsLinkHtml: function (item) {

        var menuHtml = '';
        var pageIds = item.pageIds ? item.pageIds.join(',') : '';
        pageIds = pageIds ? (' data-pageids="' + pageIds + '"') : '';
        menuHtml += '<a class="sidebarLink" href="' + item.href + '"' + pageIds + '>';

        var icon = item.icon;

        if (icon) {
            var style = item.color ? ' style="color:' + item.color + '"' : '';

            menuHtml += '<iron-icon icon="' + icon + '" class="sidebarLinkIcon"' + style + '></iron-icon>';
        }

        menuHtml += '<span class="sidebarLinkText">';
        menuHtml += item.name;
        menuHtml += '</span>';
        menuHtml += '</a>';
        return menuHtml;
    },

    getToolsMenuHtml: function (page) {

        var items = Dashboard.getToolsMenuLinks(page);

        var i, length, item;
        var menuHtml = '';
        menuHtml += '<div class="drawerContent">';
        for (i = 0, length = items.length; i < length; i++) {

            item = items[i];

            if (item.items) {

                var style = item.color ? ' iconstyle="color:' + item.color + '"' : '';
                var expanded = item.expanded ? (' expanded') : '';
                if (item.icon) {
                    menuHtml += '<emby-collapsible icon="' + item.icon + '" title="' + item.name + '"' + style + expanded + '>';
                } else {
                    menuHtml += '<emby-collapsible title="' + item.name + '"' + style + expanded + '>';
                }
                menuHtml += item.items.map(Dashboard.getToolsLinkHtml).join('');
                menuHtml += '</emby-collapsible>';
            }
            else if (item.href) {

                menuHtml += Dashboard.getToolsLinkHtml(item);
            } else {

                menuHtml += '<div class="sidebarHeader">';
                menuHtml += item.name;
                menuHtml += '</div>';
            }
        }
        menuHtml += '</div>';

        return menuHtml;
    },

    getToolsMenuLinks: function () {

        return [{
            name: Globalize.translate('TabServer'),
            icon: 'dashboard',
            color: '#38c',
            expanded: true,
            items: [
                {
                    name: Globalize.translate('TabDashboard'),
                    href: "dashboard.html",
                    pageIds: ['dashboardPage'],
                    icon: 'dashboard'
                }, {
                    name: Globalize.translate('TabSettings'),
                    href: "dashboardgeneral.html",
                    pageIds: ['dashboardGeneralPage'],
                    icon: 'settings'
                }, {
                    name: Globalize.translate('TabDevices'),
                    href: "devices.html",
                    pageIds: ['devicesPage'],
                    icon: 'tablet'
                }, {
                    name: Globalize.translate('TabUsers'),
                    href: "userprofiles.html",
                    pageIds: ['userProfilesPage'],
                    icon: 'people'
                }
            ]
        }, {
            name: Globalize.translate('TabLibrary'),
            icon: 'folder',
            color: '#ECA403',
            expanded: true,
            items: [
                {
                    name: Globalize.translate('TabFolders'),
                    href: "library.html",
                    pageIds: ['mediaLibraryPage'],
                    icon: 'folder'
                },
                {
                    name: Globalize.translate('TabMetadata'),
                    href: "metadata.html",
                    pageIds: ['metadataConfigurationPage'],
                    icon: 'insert-drive-file'
                },
                {
                    name: Globalize.translate('TabServices'),
                    href: "metadataimages.html",
                    pageIds: ['metadataImagesConfigurationPage'],
                    icon: 'insert-drive-file'
                },
                {
                    name: Globalize.translate('TabNfoSettings'),
                    href: "metadatanfo.html",
                    pageIds: ['metadataNfoPage'],
                    icon: 'insert-drive-file'
                },
                {
                    name: Globalize.translate('TabPathSubstitution'),
                    href: "librarypathmapping.html",
                    pageIds: ['libraryPathMappingPage'],
                    icon: 'mode-edit'
                },
                {
                    name: Globalize.translate('TabSubtitles'),
                    href: "metadatasubtitles.html",
                    pageIds: ['metadataSubtitlesPage'],
                    icon: 'closed-caption'
                },
                {
                    name: Globalize.translate('TabAdvanced'),
                    href: "librarysettings.html",
                    pageIds: ['librarySettingsPage'],
                    icon: 'settings'
                }
            ]
        }, {
            name: Globalize.translate('DLNA'),
            icon: 'live-tv',
            color: '#E5342E',
            items: [
                {
                    name: Globalize.translate('TabSettings'),
                    href: "dlnasettings.html",
                    pageIds: ['dlnaSettingsPage'],
                    icon: 'settings'
                },
                {
                    name: Globalize.translate('TabProfiles'),
                    href: "dlnaprofiles.html",
                    pageIds: ['dlnaProfilesPage', 'dlnaProfilePage'],
                    icon: 'live-tv'
                }
            ]
        }, {
            name: Globalize.translate('TabLiveTV'),
            icon: 'dvr',
            color: '#293AAE',
            items: [
                {
                    name: Globalize.translate('TabSettings'),
                    href: "livetvstatus.html",
                    pageIds: ['liveTvStatusPage'],
                    icon: 'settings'
                },
                {
                    name: Globalize.translate('TabAdvanced'),
                    href: "livetvsettings.html",
                    pageIds: ['liveTvSettingsPage'],
                    icon: 'settings'
                },
                {
                    name: Globalize.translate('TabServices'),
                    href: "appservices.html?context=livetv",
                    //selected: (isServicesPage && context == 'livetv'),
                    icon: 'add-shopping-cart'
                }
            ]
        }, {
            name: Globalize.translate('TabNotifications'),
            icon: 'notifications',
            color: 'brown',
            href: "notificationsettings.html"
        }, {
            name: Globalize.translate('TabPlayback'),
            icon: 'play-circle-filled',
            color: '#E5342E',
            items: [
                {
                    name: Globalize.translate('TabCinemaMode'),
                    href: "cinemamodeconfiguration.html",
                    pageIds: ['cinemaModeConfigurationPage'],
                    icon: 'local-movies'
                },
                {
                    name: Globalize.translate('TabResumeSettings'),
                    href: "playbackconfiguration.html",
                    pageIds: ['playbackConfigurationPage'],
                    icon: 'play-circle-filled'
                },
                {
                    name: Globalize.translate('TabStreaming'),
                    href: "streamingsettings.html",
                    pageIds: ['streamingSettingsPage'],
                    icon: 'wifi'
                },
                {
                    name: Globalize.translate('TabTranscoding'),
                    href: "encodingsettings.html",
                    pageIds: ['encodingSettingsPage'],
                    icon: 'play-circle-filled'
                }
            ]
        }, {
            name: Globalize.translate('TabPlugins'),
            icon: 'add-shopping-cart',
            color: '#9D22B1',
            items: [
                {
                    name: Globalize.translate('TabMyPlugins'),
                    href: "plugins.html",
                    pageIds: ['pluginsPage'],
                    icon: 'file-download'
                }, {
                    name: Globalize.translate('TabCatalog'),
                    href: "plugincatalog.html",
                    pageIds: ['pluginCatalogPage'],
                    icon: 'add-shopping-cart'
                }
            ]
        }, {
            name: Globalize.translate('TabSync'),
            icon: 'sync',
            items: [
                {
                    name: Globalize.translate('TabSyncJobs'),
                    href: "syncactivity.html",
                    pageIds: ['syncActivityPage', 'syncJobPage'],
                    icon: 'menu'
                }, {
                    name: Globalize.translate('TabCameraUpload'),
                    href: "devicesupload.html",
                    pageIds: ['devicesUploadPage'],
                    icon: 'photo'
                }, {
                    name: Globalize.translate('TabServices'),
                    href: "appservices.html?context=sync",
                    //selected: (isServicesPage && context == 'sync'),
                    icon: 'add-shopping-cart'
                }, {
                    name: Globalize.translate('TabSettings'),
                    href: "syncsettings.html",
                    pageIds: ['syncSettingsPage'],
                    icon: 'settings'
                }
            ]
        }, {
            name: Globalize.translate('TabAdvanced'),
            icon: 'settings',
            color: '#F16834',
            items: [
                {
                    name: Globalize.translate('TabAutoOrganize'),
                    href: "autoorganizelog.html",
                    pageIds: ['libraryFileOrganizerPage', 'libraryFileOrganizerSmartMatchPage', 'libraryFileOrganizerLogPage'],
                    icon: 'folder'
                },
                {
                    name: Globalize.translate('TabHosting'),
                    href: "dashboardhosting.html",
                    pageIds: ['dashboardHostingPage'],
                    icon: 'wifi'
                }, {
                    name: Globalize.translate('TabScheduledTasks'),
                    href: "scheduledtasks.html",
                    pageIds: ['scheduledTasksPage', 'scheduledTaskPage'],
                    icon: 'schedule'
                },
                {
                    name: Globalize.translate('TabSecurity'),
                    href: "serversecurity.html",
                    pageIds: ['serverSecurityPage'],
                    icon: 'lock'
                }
            ]
        }, {
            name: Globalize.translate('TabHelp'),
            icon: 'info',
            items: [
                {
                    name: Globalize.translate('TabAbout'),
                    href: "about.html",
                    pageIds: ['aboutPage'],
                    icon: 'info'
                },
                {
                    name: Globalize.translate('TabLogs'),
                    href: "log.html",
                    pageIds: ['logPage'],
                    icon: 'menu'
                },
                {
                    name: Globalize.translate('TabEmbyPremiere'),
                    href: "supporterkey.html",
                    pageIds: ['supporterKeyPage'],
                    icon: 'add-circle'
                }
            ]
        }];

    },

    processGeneralCommand: function (cmd) {

        // Full list
        // https://github.com/MediaBrowser/MediaBrowser/blob/master/MediaBrowser.Model/Session/GeneralCommand.cs#L23

        switch (cmd.Name) {

            case 'GoHome':
                Dashboard.navigate('home.html');
                break;
            case 'GoToSettings':
                Dashboard.navigate('dashboard.html');
                break;
            case 'DisplayContent':
                Dashboard.onBrowseCommand(cmd.Arguments);
                break;
            case 'GoToSearch':
                Search.showSearchPanel();
                break;
            case 'DisplayMessage':
                {
                    var args = cmd.Arguments;

                    if (args.TimeoutMs && window.Notification && Notification.permission === "granted") {

                        var notification = {
                            title: args.Header,
                            body: args.Text,
                            vibrate: true,
                            timeout: args.TimeoutMs
                        };

                        var notif = new Notification(notification.title, notification);

                        if (notif.show) {
                            notif.show();
                        }

                        if (notification.timeout) {
                            setTimeout(function () {

                                if (notif.close) {
                                    notif.close();
                                }
                                else if (notif.cancel) {
                                    notif.cancel();
                                }
                            }, notification.timeout);
                        }
                    }
                    else {
                        Dashboard.alert({ title: args.Header, message: args.Text });
                    }

                    break;
                }
            case 'VolumeUp':
            case 'VolumeDown':
            case 'Mute':
            case 'Unmute':
            case 'ToggleMute':
            case 'SetVolume':
            case 'SetAudioStreamIndex':
            case 'SetSubtitleStreamIndex':
            case 'ToggleFullscreen':
            case 'SetRepeatMode':
                break;
            default:
                console.log('Unrecognized command: ' + cmd.Name);
                break;
        }
    },

    onWebSocketMessageReceived: function (e, data) {

        var msg = data;

        if (msg.MessageType === "LibraryChanged") {
            Dashboard.processLibraryUpdateNotification(msg.Data);
        }
        else if (msg.MessageType === "ServerShuttingDown") {
            Dashboard.hideServerRestartWarning();
        }
        else if (msg.MessageType === "ServerRestarting") {
            Dashboard.hideServerRestartWarning();
        }
        else if (msg.MessageType === "SystemInfo") {
            Dashboard.updateSystemInfo(msg.Data);
        }
        else if (msg.MessageType === "RestartRequired") {
            Dashboard.updateSystemInfo(msg.Data);
        }
        else if (msg.MessageType === "PackageInstallationCompleted") {
            Dashboard.getCurrentUser().then(function (currentUser) {

                if (currentUser.Policy.IsAdministrator) {
                    Dashboard.showPackageInstallNotification(msg.Data, "completed");
                    Dashboard.refreshSystemInfoFromServer();
                }
            });
        }
        else if (msg.MessageType === "PackageInstallationFailed") {
            Dashboard.getCurrentUser().then(function (currentUser) {

                if (currentUser.Policy.IsAdministrator) {
                    Dashboard.showPackageInstallNotification(msg.Data, "failed");
                    Dashboard.refreshSystemInfoFromServer();
                }
            });
        }
        else if (msg.MessageType === "PackageInstallationCancelled") {
            Dashboard.getCurrentUser().then(function (currentUser) {

                if (currentUser.Policy.IsAdministrator) {
                    Dashboard.showPackageInstallNotification(msg.Data, "cancelled");
                    Dashboard.refreshSystemInfoFromServer();
                }
            });
        }
        else if (msg.MessaapiclientcgeType === "PackageInstalling") {
            Dashboard.getCurrentUser().then(function (currentUser) {

                if (currentUser.Policy.IsAdministrator) {
                    Dashboard.showPackageInstallNotification(msg.Data, "progress");
                    Dashboard.refreshSystemInfoFromServer();
                }
            });
        }
        else if (msg.MessageType === "GeneralCommand") {

            var cmd = msg.Data;
            // Media Controller should catch this
            //Dashboard.processGeneralCommand(cmd);
        }
    },

    onBrowseCommand: function (cmd) {

        var url;

        var type = (cmd.ItemType || "").toLowerCase();

        if (type == "genre") {
            url = "itemdetails.html?id=" + cmd.ItemId;
        }
        else if (type == "musicgenre") {
            url = "itemdetails.html?id=" + cmd.ItemId;
        }
        else if (type == "gamegenre") {
            url = "itemdetails.html?id=" + cmd.ItemId;
        }
        else if (type == "studio") {
            url = "itemdetails.html?id=" + cmd.ItemId;
        }
        else if (type == "person") {
            url = "itemdetails.html?id=" + cmd.ItemId;
        }
        else if (type == "musicartist") {
            url = "itemdetails.html?id=" + cmd.ItemId;
        }

        if (url) {
            Dashboard.navigate(url);
            return;
        }

        ApiClient.getItem(Dashboard.getCurrentUserId(), cmd.ItemId).then(function (item) {

            Dashboard.navigate(LibraryBrowser.getHref(item, null, ''));

        });

    },

    showPackageInstallNotification: function (installation, status) {

        if (AppInfo.isNativeApp) {
            return;
        }

        var html = '';

        if (status == 'completed') {
            html += '<img src="css/images/notifications/done.png" class="notificationIcon" />';
        }
        else if (status == 'cancelled') {
            html += '<img src="css/images/notifications/info.png" class="notificationIcon" />';
        }
        else if (status == 'failed') {
            html += '<img src="css/images/notifications/error.png" class="notificationIcon" />';
        }
        else if (status == 'progress') {
            html += '<img src="css/images/notifications/download.png" class="notificationIcon" />';
        }

        html += '<span style="margin-right: 1em;">';

        if (status == 'completed') {
            html += Globalize.translate('LabelPackageInstallCompleted').replace('{0}', installation.Name + ' ' + installation.Version);
        }
        else if (status == 'cancelled') {
            html += Globalize.translate('LabelPackageInstallCancelled').replace('{0}', installation.Name + ' ' + installation.Version);
        }
        else if (status == 'failed') {
            html += Globalize.translate('LabelPackageInstallFailed').replace('{0}', installation.Name + ' ' + installation.Version);
        }
        else if (status == 'progress') {
            html += Globalize.translate('LabelInstallingPackage').replace('{0}', installation.Name + ' ' + installation.Version);
        }

        html += '</span>';

        if (status == 'progress') {

            var percentComplete = Math.round(installation.PercentComplete || 0);

            html += '<progress style="margin-right: 1em;" max="100" value="' + percentComplete + '" title="' + percentComplete + '%">';
            html += '' + percentComplete + '%';
            html += '</progress>';

            if (percentComplete < 100) {
                html += '<paper-button raised class="cancelDark mini" onclick="this.disabled=\'disabled\';Dashboard.cancelInstallation(\'' + installation.Id + '\');"><iron-icon icon="cancel"></iron-icon><span>' + Globalize.translate('ButtonCancel') + '</span></paper-button>';
            }
        }

        var timeout = 0;

        if (status == 'cancelled') {
            timeout = 2000;
        }

        var forceShow = status != "progress";
        var allowHide = status != "progress" && status != 'cancelled';

        Dashboard.showFooterNotification({ html: html, id: installation.Id, timeout: timeout, forceShow: forceShow, allowHide: allowHide });
    },

    processLibraryUpdateNotification: function (data) {

        var newItems = data.ItemsAdded;

        if (!newItems.length || AppInfo.isNativeApp || !window.Notification || Notification.permission !== "granted") {
            return;
        }

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            Recursive: true,
            Limit: 3,
            Filters: "IsNotFolder",
            SortBy: "DateCreated",
            SortOrder: "Descending",
            ImageTypes: "Primary",
            Ids: newItems.join(',')

        }).then(function (result) {

            var items = result.Items;

            for (var i = 0, length = Math.min(items.length, 2) ; i < length; i++) {

                var item = items[i];

                var notification = {
                    title: "New " + item.Type,
                    body: item.Name,
                    timeout: 15000,
                    vibrate: true,

                    data: {
                        options: {
                            url: LibraryBrowser.getHref(item)
                        }
                    }
                };

                var imageTags = item.ImageTags || {};

                if (imageTags.Primary) {

                    notification.icon = ApiClient.getScaledImageUrl(item.Id, {
                        width: 60,
                        tag: imageTags.Primary,
                        type: "Primary"
                    });
                }

                var notif = new Notification(notification.title, notification);

                if (notif.show) {
                    notif.show();
                }

                if (notification.timeout) {
                    setTimeout(function () {

                        if (notif.close) {
                            notif.close();
                        }
                        else if (notif.cancel) {
                            notif.cancel();
                        }
                    }, notification.timeout);
                }
            }
        });
    },

    setPageTitle: function (title, documentTitle) {

        LibraryMenu.setTitle(title || 'Emby');

        documentTitle = documentTitle || title;
        if (documentTitle) {
            document.title = documentTitle;
        }
    },

    getDisplayTime: function (ticks) {

        var ticksPerHour = 36000000000;
        var ticksPerMinute = 600000000;
        var ticksPerSecond = 10000000;

        var parts = [];

        var hours = ticks / ticksPerHour;
        hours = Math.floor(hours);

        if (hours) {
            parts.push(hours);
        }

        ticks -= (hours * ticksPerHour);

        var minutes = ticks / ticksPerMinute;
        minutes = Math.floor(minutes);

        ticks -= (minutes * ticksPerMinute);

        if (minutes < 10 && hours) {
            minutes = '0' + minutes;
        }
        parts.push(minutes);

        var seconds = ticks / ticksPerSecond;
        seconds = Math.floor(seconds);

        if (seconds < 10) {
            seconds = '0' + seconds;
        }
        parts.push(seconds);

        return parts.join(':');
    },

    getSupportedRemoteCommands: function () {

        // Full list
        // https://github.com/MediaBrowser/MediaBrowser/blob/master/MediaBrowser.Model/Session/GeneralCommand.cs
        return [
            "GoHome",
            "GoToSettings",
            "VolumeUp",
            "VolumeDown",
            "Mute",
            "Unmute",
            "ToggleMute",
            "SetVolume",
            "SetAudioStreamIndex",
            "SetSubtitleStreamIndex",
            "DisplayContent",
            "GoToSearch",
            "DisplayMessage",
            "SetRepeatMode"
        ];

    },

    isServerlessPage: function () {
        var url = window.location.href.toLowerCase();
        return url.indexOf('connectlogin.html') != -1 || url.indexOf('selectserver.html') != -1 || url.indexOf('login.html') != -1 || url.indexOf('forgotpassword.html') != -1 || url.indexOf('forgotpasswordpin.html') != -1;
    },

    capabilities: function () {

        var caps = {
            PlayableMediaTypes: ['Audio', 'Video'],

            SupportedCommands: Dashboard.getSupportedRemoteCommands(),

            // Need to use this rather than AppInfo.isNativeApp because the property isn't set yet at the time we call this
            SupportsPersistentIdentifier: Dashboard.isRunningInCordova(),

            SupportsMediaControl: true,
            SupportedLiveMediaTypes: ['Audio', 'Video']
        };

        if (Dashboard.isRunningInCordova() && !browserInfo.safari) {
            caps.SupportsOfflineAccess = true;
            caps.SupportsSync = true;
            caps.SupportsContentUploading = true;
        }

        return caps;
    },

    getDefaultImageQuality: function (imageType) {

        var quality = 90;
        var isBackdrop = imageType.toLowerCase() == 'backdrop';

        if (isBackdrop) {
            quality -= 10;
        }

        if (AppInfo.hasLowImageBandwidth) {

            // The native app can handle a little bit more than safari
            if (AppInfo.isNativeApp) {

                quality -= 10;

            } else {

                quality -= 40;
            }
        }

        return quality;
    },

    normalizeImageOptions: function (options) {

        if (AppInfo.hasLowImageBandwidth) {

            options.enableImageEnhancers = false;
        }

        if (AppInfo.forcedImageFormat && options.type != 'Logo') {
            options.format = AppInfo.forcedImageFormat;
            options.backgroundColor = '#1c1c1c';
        }
    },

    loadExternalPlayer: function () {

        return new Promise(function (resolve, reject) {

            require(['scripts/externalplayer.js'], function () {

                if (Dashboard.isRunningInCordova()) {
                    require(['cordova/externalplayer.js'], resolve);
                } else {
                    resolve();
                }
            });
        });
    },

    exitOnBack: function () {
        return $($.mobile.activePage).is('#indexPage');
    },

    exit: function () {
        Dashboard.logout();
    }
};

var AppInfo = {};

(function () {

    function isTouchDevice() {
        return (('ontouchstart' in window)
             || (navigator.MaxTouchPoints > 0)
             || (navigator.msMaxTouchPoints > 0));
    }

    function setAppInfo() {

        if (isTouchDevice()) {
            AppInfo.isTouchPreferred = true;
        }

        var isCordova = Dashboard.isRunningInCordova();

        AppInfo.enableDetailPageChapters = true;
        AppInfo.enableDetailsMenuImages = true;
        AppInfo.enableMovieHomeSuggestions = true;
        AppInfo.enableNavDrawer = true;
        AppInfo.enableSearchInTopMenu = true;
        AppInfo.enableHomeFavorites = true;
        AppInfo.enableNowPlayingBar = true;
        AppInfo.enableHomeTabs = true;
        AppInfo.enableNowPlayingPageBottomTabs = true;
        AppInfo.enableAutoSave = browserInfo.mobile;
        AppInfo.enableHashBang = Dashboard.isRunningInCordova();

        AppInfo.enableAppStorePolicy = isCordova;

        var isIOS = browserInfo.ipad || browserInfo.iphone;
        var isAndroid = browserInfo.android;
        var isMobile = browserInfo.mobile;

        if (isIOS) {

            AppInfo.hasLowImageBandwidth = true;

            if (isCordova) {
                //AppInfo.enableSectionTransitions = true;
                AppInfo.enableNavDrawer = false;
                AppInfo.enableSearchInTopMenu = false;
                AppInfo.enableHomeFavorites = false;
                AppInfo.enableHomeTabs = false;
                AppInfo.enableNowPlayingPageBottomTabs = false;

                // Disable the now playing bar for the iphone since we already have the now playing tab at the bottom
                if (navigator.userAgent.toString().toLowerCase().indexOf('iphone') != -1) {
                    AppInfo.enableNowPlayingBar = false;
                }

            } else {
                AppInfo.enableDetailPageChapters = false;
                AppInfo.enableDetailsMenuImages = false;
                AppInfo.enableMovieHomeSuggestions = false;

                AppInfo.forcedImageFormat = 'jpg';
            }
        }

        if (!AppInfo.hasLowImageBandwidth) {
            AppInfo.enableStudioTabs = true;
            AppInfo.enableTvEpisodesTab = true;
        }

        AppInfo.supportsExternalPlayers = true;

        if (isCordova) {
            AppInfo.enableAppLayouts = true;
            AppInfo.supportsExternalPlayerMenu = true;
            AppInfo.isNativeApp = true;

            if (isIOS) {
                AppInfo.supportsExternalPlayers = false;
            }
        }
        else {
            AppInfo.enableSupporterMembership = true;

            if (!isAndroid && !isIOS) {
                AppInfo.enableAppLayouts = true;
            }
        }

        // This doesn't perform well on iOS
        AppInfo.enableHeadRoom = !isIOS;

        AppInfo.supportsDownloading = !(AppInfo.isNativeApp && isIOS);

        // This currently isn't working on android, unfortunately
        AppInfo.supportsFileInput = !(AppInfo.isNativeApp && isAndroid);

        AppInfo.hasPhysicalVolumeButtons = isCordova || isMobile;
        AppInfo.enableBackButton = isIOS && (window.navigator.standalone || AppInfo.isNativeApp);

        AppInfo.supportsSyncPathSetting = isCordova && isAndroid;

        if (isCordova && isIOS) {
            AppInfo.moreIcon = 'more-horiz';
        } else {
            AppInfo.moreIcon = 'more-vert';
        }
    }

    function initializeApiClient(apiClient) {

        if (AppInfo.enableAppStorePolicy) {
            apiClient.getAvailablePlugins = function () {
                return Promise.resolve([]);
            };
            apiClient.getInstalledPlugins = function () {
                return Promise.resolve([]);
            };
        }

        apiClient.getDefaultImageQuality = Dashboard.getDefaultImageQuality;
        apiClient.normalizeImageOptions = Dashboard.normalizeImageOptions;

        Events.off(apiClient, 'websocketmessage', Dashboard.onWebSocketMessageReceived);
        Events.on(apiClient, 'websocketmessage', Dashboard.onWebSocketMessageReceived);

        Events.off(apiClient, 'requestfail', Dashboard.onRequestFail);
        Events.on(apiClient, 'requestfail', Dashboard.onRequestFail);
    }

    function getSyncProfile() {

        return getRequirePromise(['scripts/mediaplayer']).then(function () {
            return MediaPlayer.getDeviceProfile(Math.max(screen.height, screen.width));
        });
    }

    function onApiClientCreated(e, newApiClient) {
        initializeApiClient(newApiClient);

        // This is not included in jQuery slim
        if (window.$) {
            $.ajax = newApiClient.ajax;
        }
    }

    function defineConnectionManager(connectionManager) {
        define('connectionManager', [], function () {
            return connectionManager;
        });
    }

    var localApiClient;
    function bindConnectionManagerEvents(connectionManager, events) {

        connectionManager.currentApiClient = function () {

            if (!localApiClient) {
                var server = connectionManager.getLastUsedServer();
                if (server) {
                    localApiClient = connectionManager.getApiClient(server.Id);
                }
            }
            return localApiClient;
        };

        //events.on(connectionManager, 'apiclientcreated', function (e, newApiClient) {

        //    //$(newApiClient).on("websocketmessage", Dashboard.onWebSocketMessageReceived).on('requestfail', Dashboard.onRequestFail);
        //    newApiClient.normalizeImageOptions = normalizeImageOptions;
        //});

        events.on(connectionManager, 'localusersignedin', function (e, user) {
            localApiClient = connectionManager.getApiClient(user.ServerId);
            window.ApiClient = localApiClient;
        });
    }

    //localStorage.clear();
    function createConnectionManager(credentialProviderFactory, capabilities) {

        var credentialKey = Dashboard.isConnectMode() ? null : 'servercredentials4';
        var credentialProvider = new credentialProviderFactory(credentialKey);

        return getSyncProfile().then(function (deviceProfile) {

            capabilities.DeviceProfile = deviceProfile;

            window.ConnectionManager = new MediaBrowser.ConnectionManager(credentialProvider, AppInfo.appName, AppInfo.appVersion, AppInfo.deviceName, AppInfo.deviceId, capabilities, window.devicePixelRatio);

            defineConnectionManager(window.ConnectionManager);
            bindConnectionManagerEvents(window.ConnectionManager, Events);

            console.log('binding to apiclientcreated');
            Events.on(ConnectionManager, 'apiclientcreated', onApiClientCreated);

            if (Dashboard.isConnectMode()) {

                return Promise.resolve();

            } else {

                console.log('loading ApiClient singleton');

                return getRequirePromise(['apiclient']).then(function (apiClientFactory) {

                    console.log('creating ApiClient singleton');

                    var apiClient = new apiClientFactory(Dashboard.serverAddress(), AppInfo.appName, AppInfo.appVersion, AppInfo.deviceName, AppInfo.deviceId, window.devicePixelRatio);
                    apiClient.enableAutomaticNetworking = false;
                    ConnectionManager.addApiClient(apiClient);
                    Dashboard.importCss(apiClient.getUrl('Branding/Css'));
                    window.ApiClient = apiClient;
                    localApiClient = apiClient;
                    console.log('loaded ApiClient singleton');
                });
            }
        });
    }

    function initFastClick() {

        require(["fastclick"], function (FastClick) {

            FastClick.attach(document.body, {
                tapDelay: 0
            });

            function parentWithClass(elem, className) {

                while (!elem.classList || !elem.classList.contains(className)) {
                    elem = elem.parentNode;

                    if (!elem) {
                        return null;
                    }
                }

                return elem;
            }

            // Have to work around this issue of fast click breaking the panel dismiss
            document.body.addEventListener('touchstart', function (e) {

                var tgt = parentWithClass(e.target, 'ui-panel-dismiss');
                if (tgt) {
                    $(tgt).click();
                }
            });
        });

    }

    function setDocumentClasses(browser) {

        var elem = document.documentElement;

        if (!browser.android && !browser.mobile) {
            elem.classList.add('smallerDefault');
        }

        if (AppInfo.isTouchPreferred) {
            elem.classList.add('touch');
        }

        if (!AppInfo.enableStudioTabs) {
            elem.classList.add('studioTabDisabled');
        }

        if (!AppInfo.enableTvEpisodesTab) {
            elem.classList.add('tvEpisodesTabDisabled');
        }

        if (!AppInfo.enableSupporterMembership) {
            elem.classList.add('supporterMembershipDisabled');
        }

        if (AppInfo.isNativeApp) {
            elem.classList.add('nativeApp');
        }

        if (!AppInfo.enableHomeFavorites) {
            elem.classList.add('homeFavoritesDisabled');
        }
    }

    function loadTheme() {

        var name = getParameterByName('theme');
        if (name) {
            require(['themes/' + name + '/theme']);
            return;
        }

        var date = new Date();
        var month = date.getMonth();
        var day = date.getDate();

        if (month == 9 && day >= 30) {
            require(['themes/halloween/theme']);
            return;
        }

        if (month == 11 && day >= 21 && day <= 26) {
            require(['themes/holiday/theme']);
            return;
        }
    }

    function returnFirstDependency(obj) {
        return obj;
    }

    function getBowerPath() {

        var bowerPath = "bower_components";

        // Put the version into the bower path since we can't easily put a query string param on html imports
        // Emby server will handle this
        if (Dashboard.isConnectMode() && !Dashboard.isRunningInCordova()) {
            //bowerPath += window.dashboardVersion;
        }

        return bowerPath;
    }

    function initRequire() {

        var urlArgs = "v=" + (window.dashboardVersion || new Date().getDate());

        var bowerPath = getBowerPath();

        var apiClientBowerPath = bowerPath + "/emby-apiclient";
        var embyWebComponentsBowerPath = bowerPath + '/emby-webcomponents';

        var paths = {
            velocity: bowerPath + "/velocity/velocity.min",
            tvguide: 'components/tvguide/tvguide',
            directorybrowser: 'components/directorybrowser/directorybrowser',
            collectioneditor: 'components/collectioneditor/collectioneditor',
            playlisteditor: 'components/playlisteditor/playlisteditor',
            medialibrarycreator: 'components/medialibrarycreator/medialibrarycreator',
            medialibraryeditor: 'components/medialibraryeditor/medialibraryeditor',
            howler: bowerPath + '/howler.js/howler.min',
            sortable: bowerPath + '/Sortable/Sortable.min',
            isMobile: bowerPath + '/isMobile/isMobile.min',
            headroom: bowerPath + '/headroom.js/dist/headroom.min',
            masonry: bowerPath + '/masonry/dist/masonry.pkgd.min',
            humanedate: 'components/humanedate',
            libraryBrowser: 'scripts/librarybrowser',
            chromecasthelpers: 'components/chromecasthelpers',
            fastclick: bowerPath + '/fastclick/lib/fastclick',
            events: apiClientBowerPath + '/events',
            credentialprovider: apiClientBowerPath + '/credentials',
            apiclient: apiClientBowerPath + '/apiclient',
            connectionmanagerfactory: apiClientBowerPath + '/connectionmanager',
            visibleinviewport: embyWebComponentsBowerPath + "/visibleinviewport",
            browserdeviceprofile: embyWebComponentsBowerPath + "/browserdeviceprofile",
            browser: embyWebComponentsBowerPath + "/browser",
            qualityoptions: embyWebComponentsBowerPath + "/qualityoptions",
            connectservice: apiClientBowerPath + '/connectservice',
            hammer: bowerPath + "/hammerjs/hammer.min",
            layoutManager: embyWebComponentsBowerPath + "/layoutmanager",
            pageJs: embyWebComponentsBowerPath + '/page.js/page',
            focusManager: embyWebComponentsBowerPath + "/focusmanager",
            globalize: embyWebComponentsBowerPath + "/globalize",
            imageLoader: embyWebComponentsBowerPath + "/images/imagehelper"
        };

        if (navigator.webkitPersistentStorage) {
            paths.imageFetcher = embyWebComponentsBowerPath + "/images/persistentimagefetcher";
        } else if (Dashboard.isRunningInCordova()) {
            paths.imageFetcher = 'cordova/imagestore';
        } else {
            paths.imageFetcher = embyWebComponentsBowerPath + "/images/basicimagefetcher";
        }

        paths.hlsjs = bowerPath + "/hls.js/dist/hls.min";

        if (Dashboard.isRunningInCordova()) {
            paths.sharingwidget = "cordova/sharingwidget";
            paths.serverdiscovery = "cordova/serverdiscovery";
            paths.wakeonlan = "cordova/wakeonlan";
            paths.actionsheet = "cordova/actionsheet";
        } else {
            paths.sharingwidget = "components/sharingwidget";
            paths.serverdiscovery = apiClientBowerPath + "/serverdiscovery";
            paths.wakeonlan = apiClientBowerPath + "/wakeonlan";

            define("actionsheet", [embyWebComponentsBowerPath + "/actionsheet/actionsheet"], returnFirstDependency);
        }

        define("libjass", [bowerPath + "/libjass/libjass", "css!" + bowerPath + "/libjass/libjass"], returnFirstDependency);

        define("backdrop", [embyWebComponentsBowerPath + "/backdrop/backdrop"], returnFirstDependency);
        define("fetchHelper", [embyWebComponentsBowerPath + "/fetchhelper"], returnFirstDependency);

        define("viewManager", [embyWebComponentsBowerPath + "/viewmanager"], function (viewManager) {
            viewManager.dispatchPageEvents(true);
            return viewManager;
        });

        // hack for an android test before browserInfo is loaded
        if (Dashboard.isRunningInCordova() && window.MainActivity) {
            paths.appStorage = "cordova/android/appstorage";
        } else {
            paths.appStorage = apiClientBowerPath + "/appstorage";
        }

        paths.playlistManager = "scripts/playlistmanager";
        paths.syncDialog = "scripts/sync";

        var sha1Path = bowerPath + "/cryptojslib/components/sha1-min";
        var md5Path = bowerPath + "/cryptojslib/components/md5-min";
        var shim = {};

        shim[sha1Path] = {
            deps: [bowerPath + "/cryptojslib/components/core-min"]
        };

        shim[md5Path] = {
            deps: [bowerPath + "/cryptojslib/components/core-min"]
        };

        requirejs.config({
            waitSeconds: 0,
            map: {
                '*': {
                    'css': bowerPath + '/emby-webcomponents/requirecss',
                    'html': bowerPath + '/emby-webcomponents/requirehtml'
                }
            },
            urlArgs: urlArgs,

            paths: paths,
            shim: shim
        });

        define("cryptojs-sha1", [sha1Path]);
        define("cryptojs-md5", [md5Path]);

        // Done
        define("emby-icons", ["html!" + bowerPath + "/emby-icons/emby-icons.html"]);

        define("paper-spinner", ["html!" + bowerPath + "/paper-spinner/paper-spinner.html"]);
        define("paper-toast", ["html!" + bowerPath + "/paper-toast/paper-toast.html"]);
        define("paper-slider", ["html!" + bowerPath + "/paper-slider/paper-slider.html"]);
        define("paper-tabs", ["html!" + bowerPath + "/paper-tabs/paper-tabs.html"]);
        define("paper-menu", ["html!" + bowerPath + "/paper-menu/paper-menu.html"]);
        define("paper-material", ["html!" + bowerPath + "/paper-material/paper-material.html"]);
        define("paper-dialog", ["html!" + bowerPath + "/paper-dialog/paper-dialog.html"]);
        define("paper-dialog-scrollable", ["html!" + bowerPath + "/paper-dialog-scrollable/paper-dialog-scrollable.html"]);
        define("paper-button", ["html!" + bowerPath + "/paper-button/paper-button.html"]);
        define("paper-icon-button", ["html!" + bowerPath + "/paper-icon-button/paper-icon-button.html"]);
        define("paper-drawer-panel", ["html!" + bowerPath + "/paper-drawer-panel/paper-drawer-panel.html"]);
        define("paper-radio-group", ["html!" + bowerPath + "/paper-radio-group/paper-radio-group.html"]);
        define("paper-radio-button", ["html!" + bowerPath + "/paper-radio-button/paper-radio-button.html"]);
        define("neon-animated-pages", ["html!" + bowerPath + "/neon-animation/neon-animated-pages.html"]);
        define("paper-toggle-button", ["html!" + bowerPath + "/paper-toggle-button/paper-toggle-button.html"]);

        define("slide-right-animation", ["html!" + bowerPath + "/neon-animation/animations/slide-right-animation.html"]);
        define("slide-left-animation", ["html!" + bowerPath + "/neon-animation/animations/slide-left-animation.html"]);
        define("slide-from-right-animation", ["html!" + bowerPath + "/neon-animation/animations/slide-from-right-animation.html"]);
        define("slide-from-left-animation", ["html!" + bowerPath + "/neon-animation/animations/slide-from-left-animation.html"]);
        define("paper-textarea", ["html!" + bowerPath + "/paper-input/paper-textarea.html"]);
        define("paper-item", ["html!" + bowerPath + "/paper-item/paper-item.html"]);
        define("paper-checkbox", ["html!" + bowerPath + "/paper-checkbox/paper-checkbox.html"]);
        define("fade-in-animation", ["html!" + bowerPath + "/neon-animation/animations/fade-in-animation.html"]);
        define("fade-out-animation", ["html!" + bowerPath + "/neon-animation/animations/fade-out-animation.html"]);
        define("scale-up-animation", ["html!" + bowerPath + "/neon-animation/animations/scale-up-animation.html"]);
        define("paper-fab", ["html!" + bowerPath + "/paper-fab/paper-fab.html"]);
        define("paper-progress", ["html!" + bowerPath + "/paper-progress/paper-progress.html"]);
        define("paper-input", ["html!" + bowerPath + "/paper-input/paper-input.html"]);
        define("paper-icon-item", ["html!" + bowerPath + "/paper-item/paper-icon-item.html"]);
        define("paper-item-body", ["html!" + bowerPath + "/paper-item/paper-item-body.html"]);

        define("paper-collapse-item", ["html!" + bowerPath + "/paper-collapse-item/paper-collapse-item.html"]);
        define("emby-collapsible", ["html!" + bowerPath + "/emby-collapsible/emby-collapsible.html"]);

        define("jstree", [bowerPath + "/jstree/dist/jstree", "css!thirdparty/jstree/themes/default/style.min.css"]);

        define('jqm', ['thirdparty/jquerymobile-1.4.5/jquery.mobile.custom.js']);

        define("jqmbase", ['css!thirdparty/jquerymobile-1.4.5/jquery.mobile.custom.theme.css']);
        define("jqmicons", ['jqmbase', 'css!thirdparty/jquerymobile-1.4.5/jquery.mobile.custom.icons.css']);
        define("jqmtable", ['jqmbase', "thirdparty/jquerymobile-1.4.5/jqm.table", 'css!thirdparty/jquerymobile-1.4.5/jqm.table.css']);

        define("jqmwidget", ['jqmbase', "thirdparty/jquerymobile-1.4.5/jqm.widget"]);

        define("jqmslider", ['jqmbase', "thirdparty/jquerymobile-1.4.5/jqm.slider", 'css!thirdparty/jquerymobile-1.4.5/jqm.slider.css']);

        define("jqmpopup", ['jqmbase', "thirdparty/jquerymobile-1.4.5/jqm.popup", 'css!thirdparty/jquerymobile-1.4.5/jqm.popup.css']);

        define("jqmlistview", ['jqmbase', 'css!thirdparty/jquerymobile-1.4.5/jqm.listview.css']);

        define("jqmcontrolgroup", ['jqmbase', 'css!thirdparty/jquerymobile-1.4.5/jqm.controlgroup.css']);

        define("jqmcollapsible", ['jqmbase', "jqmicons", "thirdparty/jquerymobile-1.4.5/jqm.collapsible", 'css!thirdparty/jquerymobile-1.4.5/jqm.collapsible.css']);

        define("jqmcheckbox", ['jqmbase', "jqmicons", "thirdparty/jquerymobile-1.4.5/jqm.checkbox", 'css!thirdparty/jquerymobile-1.4.5/jqm.checkbox.css']);

        define("jqmpanel", ['jqmbase', "thirdparty/jquerymobile-1.4.5/jqm.panel", 'css!thirdparty/jquerymobile-1.4.5/jqm.panel.css']);

        define("iron-icon-set", ["html!" + bowerPath + "/iron-icon/iron-icon.html", "html!" + bowerPath + "/iron-iconset-svg/iron-iconset-svg.html"]);
        define("slideshow", [embyWebComponentsBowerPath + "/slideshow/slideshow"], returnFirstDependency);

        define('fetch', [bowerPath + '/fetch/fetch']);
        define('objectassign', [embyWebComponentsBowerPath + '/objectassign']);
        define('webcomponentsjs', [bowerPath + '/webcomponentsjs/webcomponents-lite.min.js']);
        define('native-promise-only', [bowerPath + '/native-promise-only/lib/npo.src']);

        if (Dashboard.isRunningInCordova()) {
            define('registrationservices', ['cordova/registrationservices']);

        } else {
            define('registrationservices', ['scripts/registrationservices']);
        }

        if (Dashboard.isRunningInCordova()) {
            define("localassetmanager", ["cordova/localassetmanager"]);
            define("fileupload", ["cordova/fileupload"]);
        } else {
            define("localassetmanager", [apiClientBowerPath + "/localassetmanager"]);
            define("fileupload", [apiClientBowerPath + "/fileupload"]);
        }
        define("connectionmanager", [apiClientBowerPath + "/connectionmanager"]);

        define("contentuploader", [apiClientBowerPath + "/sync/contentuploader"]);
        define("serversync", [apiClientBowerPath + "/sync/serversync"]);
        define("multiserversync", [apiClientBowerPath + "/sync/multiserversync"]);
        define("offlineusersync", [apiClientBowerPath + "/sync/offlineusersync"]);
        define("mediasync", [apiClientBowerPath + "/sync/mediasync"]);

        define("swiper", [bowerPath + "/Swiper/dist/js/swiper.min", "css!" + bowerPath + "/Swiper/dist/css/swiper.min"], returnFirstDependency);

        define("dialogHelper", [embyWebComponentsBowerPath + "/dialoghelper/dialoghelper"], returnFirstDependency);
        define("toast", [embyWebComponentsBowerPath + "/toast/toast"], returnFirstDependency);
        define("scrollHelper", [embyWebComponentsBowerPath + "/scrollhelper"], returnFirstDependency);

        define("appSettings", [embyWebComponentsBowerPath + "/appsettings"], updateAppSettings);
        define("userSettings", [embyWebComponentsBowerPath + "/usersettings"], returnFirstDependency);

        define("robotoFont", ['css!' + embyWebComponentsBowerPath + '/fonts/roboto/style']);
        define("opensansFont", ['css!' + embyWebComponentsBowerPath + '/fonts/opensans/style']);
        define("montserratFont", ['css!' + embyWebComponentsBowerPath + '/fonts/montserrat/style']);
        define("scrollStyles", ['css!' + embyWebComponentsBowerPath + '/scrollstyles']);

        define("viewcontainer", ['components/viewcontainer-lite'], returnFirstDependency);
        define('queryString', [bowerPath + '/query-string/index'], function () {
            return queryString;
        });

        define("material-design-lite", [bowerPath + "/material-design-lite/material.min", "css!" + bowerPath + "/material-design-lite/material"]);
        define("MaterialSpinner", ["material-design-lite"]);

        define("jQuery", [bowerPath + '/jquery/dist/jquery.slim.min'], function () {

            require(['legacy/fnchecked']);
            if (window.ApiClient) {
                jQuery.ajax = ApiClient.ajax;
            }
            return jQuery;
        });

        // alias
        define("historyManager", [], function () {
            return Emby.Page;
        });

        // mock this for now. not used in this app
        define("inputManager", [], function () {
            return {
                on: function () {
                },
                off: function () {
                }
            };
        });

        // mock this for now. not used in this app
        define("skinManager", [], function () {

            return {
                loadUserSkin: function () {

                    Emby.Page.show('/home.html');
                }
            };
        });

        // mock this for now. not used in this app
        define("pluginManager", [], function () {
            return {
            };
        });

        define("connectionManager", [], function () {
            return ConnectionManager;
        });

        define('apiClientResolver', [], function () {
            return function () {
                return window.ApiClient;
            };
        });

        define('dialogText', ['globalize'], getDialogText());

        define("router", [embyWebComponentsBowerPath + '/router'], function (embyRouter) {

            embyRouter.showLocalLogin = function (apiClient, serverId, manualLogin) {
                Dashboard.navigate('login.html?serverid=' + serverId);
            };

            embyRouter.showSelectServer = function () {
                Dashboard.navigate('selectserver.html');
            };

            embyRouter.showWelcome = function () {

                if (Dashboard.isConnectMode()) {
                    Dashboard.navigate('connectlogin.html?mode=welcome');
                } else {
                    Dashboard.navigate('login.html');
                }
            };

            embyRouter.showSettings = function () {
                Dashboard.navigate('mypreferencesmenu.html?userId=' + ApiClient.getCurrentUserId());
            };

            return embyRouter;
        });
    }

    function updateAppSettings(appSettings) {

        appSettings.enableExternalPlayers = function (val) {

            if (val != null) {
                appSettings.set('externalplayers', val.toString());
            }

            return appSettings.get('externalplayers') == 'true';
        };

        return appSettings;
    }

    function getDialogText() {
        return function (globalize) {
            return {
                get: function (text) {
                    return globalize.translate('Button' + text);
                }
            };
        };
    }

    function initRequireWithBrowser(browser) {

        var bowerPath = getBowerPath();

        var embyWebComponentsBowerPath = bowerPath + '/emby-webcomponents';

        if (browser.mobile) {
            define("prompt", [embyWebComponentsBowerPath + "/prompt/nativeprompt"], returnFirstDependency);
            define("confirm", [embyWebComponentsBowerPath + "/confirm/nativeconfirm"], returnFirstDependency);
            define("alert", [embyWebComponentsBowerPath + "/alert/nativealert"], returnFirstDependency);
        } else {
            define("prompt", [embyWebComponentsBowerPath + "/prompt/prompt"], returnFirstDependency);
            define("confirm", [embyWebComponentsBowerPath + "/confirm/confirm"], returnFirstDependency);
            define("alert", [embyWebComponentsBowerPath + "/alert/alert"], returnFirstDependency);
        }

        if (browser.animate) {
            define("loading", [embyWebComponentsBowerPath + "/loading/loading"], returnFirstDependency);
        } else if (browser.tv) {
            define("loading", [embyWebComponentsBowerPath + "/loading/loading-smarttv"], returnFirstDependency);
        } else {
            define("loading", [embyWebComponentsBowerPath + "/loading/loading-lite"], returnFirstDependency);
        }

        if (Dashboard.isRunningInCordova() && browser.android) {
            define("fileDownloader", ['cordova/android/filedownloader'], returnFirstDependency);
        } else {
            define("fileDownloader", ['components/filedownloader'], returnFirstDependency);
        }
    }

    function init(hostingAppInfo) {

        if (Dashboard.isRunningInCordova() && browserInfo.android) {
            define("nativedirectorychooser", ["cordova/android/nativedirectorychooser"]);
        }

        if (Dashboard.isRunningInCordova() && browserInfo.android) {
            if (MainActivity.getChromeVersion() >= 48) {
                define("audiorenderer", ["scripts/htmlmediarenderer"]);
                //define("audiorenderer", ["cordova/android/vlcplayer"]);
            } else {
                window.VlcAudio = true;
                define("audiorenderer", ["cordova/android/vlcplayer"]);
            }
            define("videorenderer", ["cordova/android/vlcplayer"]);
        }
        else if (Dashboard.isRunningInCordova() && browserInfo.safari) {
            define("audiorenderer", ["cordova/ios/vlcplayer"]);
            define("videorenderer", ["scripts/htmlmediarenderer"]);
        }
        else {
            define("audiorenderer", ["scripts/htmlmediarenderer"]);
            define("videorenderer", ["scripts/htmlmediarenderer"]);
        }

        if (Dashboard.isRunningInCordova() && browserInfo.android) {
            define("localsync", ["cordova/android/localsync"]);
        }
        else {
            define("localsync", ["scripts/localsync"]);
        }

        define("livetvcss", [], function () {
            Dashboard.importCss('css/livetv.css');
            return {};
        });
        define("detailtablecss", [], function () {
            Dashboard.importCss('css/detailtable.css');
            return {};
        });
        define("tileitemcss", ['css!css/tileitem.css']);

        define("sharingmanager", ["scripts/sharingmanager"]);

        if (Dashboard.isRunningInCordova() && browserInfo.safari) {
            define("searchmenu", ["cordova/searchmenu"]);
        } else {
            define("searchmenu", ["scripts/searchmenu"]);
        }

        define("buttonenabled", ["legacy/buttonenabled"]);

        var deps = [];
        deps.push('events');

        deps.push('scripts/mediacontroller');

        deps.push('paper-drawer-panel');

        require(deps, function (events) {

            window.Events = events;

            for (var i in hostingAppInfo) {
                AppInfo[i] = hostingAppInfo[i];
            }

            initAfterDependencies();
        });
    }

    function getRequirePromise(deps) {

        return new Promise(function (resolve, reject) {

            require(deps, resolve);
        });
    }

    function initAfterDependencies() {

        var drawer = document.querySelector('.mainDrawerPanel');
        drawer.classList.remove('mainDrawerPanelPreInit');
        drawer.forceNarrow = true;

        var drawerWidth = screen.availWidth - 50;
        // At least 240
        drawerWidth = Math.max(drawerWidth, 240);
        // But not exceeding 270
        drawerWidth = Math.min(drawerWidth, 270);

        drawer.drawerWidth = drawerWidth + "px";

        if (browserInfo.safari) {
            drawer.disableEdgeSwipe = true;
        }

        var deps = [];
        deps.push('connectionmanagerfactory');
        deps.push('credentialprovider');

        deps.push('scripts/extensions');

        if (!window.fetch) {
            deps.push('fetch');
        }

        if (typeof Object.assign != 'function') {
            deps.push('objectassign');
        }

        require(deps, function (connectionManagerExports, credentialProviderFactory) {

            window.MediaBrowser = window.MediaBrowser || {};
            for (var i in connectionManagerExports) {
                MediaBrowser[i] = connectionManagerExports[i];
            }

            createConnectionManager(credentialProviderFactory, Dashboard.capabilities()).then(function () {

                console.log('initAfterDependencies promises resolved');
                MediaController.init();

                require(['globalize'], function (globalize) {

                    window.Globalize = globalize;

                    loadCoreDictionary(globalize).then(onGlobalizeInit);
                });
            });
        });
    }

    function loadCoreDictionary(globalize) {

        var baseUrl = 'strings/';

        var languages = ['ar', 'bg-BG', 'ca', 'cs', 'da', 'de', 'el', 'en-GB', 'en-US', 'es-AR', 'es-MX', 'es', 'fi', 'fr', 'gsw', 'he', 'hr', 'hu', 'id', 'it', 'kk', 'ko', 'ms', 'nb', 'nl', 'pl', 'pt-BR', 'pt-PT', 'ro', 'ru', 'sl-SI', 'sv', 'tr', 'uk', 'vi', 'zh-CN', 'zh-HK', 'zh-TW'];

        var translations = languages.map(function (i) {
            return {
                lang: i,
                path: baseUrl + i + '.json'
            };
        });

        globalize.defaultModule('core');

        return globalize.loadStrings({
            name: 'core',
            translations: translations
        });
    }

    function onGlobalizeInit() {

        document.title = Globalize.translateDocument(document.title, 'core');

        onAppReady();
    }

    function defineRoute(newRoute, dictionary) {

        var baseRoute = Emby.Page.baseUrl();

        var path = newRoute.path;

        path = path.replace(baseRoute, '');

        console.log('Defining route: ' + path);

        newRoute.dictionary = newRoute.dictionary || dictionary || 'core';
        Emby.Page.addRoute(path, newRoute);
    }

    function defineCoreRoutes() {

        console.log('Defining core routes');

        defineRoute({
            path: '/about.html',
            dependencies: [],
            autoFocus: false,
            controller: 'scripts/aboutpage',
            roles: 'admin'
        });

        defineRoute({
            path: '/addplugin.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/appservices.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/autoorganizelog.html',
            dependencies: [],
            roles: 'admin'
        });

        defineRoute({
            path: '/autoorganizesmart.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/autoorganizetv.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/channelitems.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/channels.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/channelsettings.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/cinemamodeconfiguration.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/collections.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/connectlogin.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/dashboard.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/dashboardgeneral.html',
            dependencies: ['emby-collapsible', 'paper-textarea', 'paper-input', 'paper-checkbox'],
            controller: 'scripts/dashboardgeneral',
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/dashboardhosting.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/device.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/devices.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/devicesupload.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/dlnaprofile.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/dlnaprofiles.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/dlnaserversettings.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/dlnasettings.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/edititemmetadata.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/encodingsettings.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/favorites.html',
            dependencies: [],
            autoFocus: false,
            controller: 'scripts/favorites'
        });

        defineRoute({
            path: '/forgotpassword.html',
            dependencies: [],
            anonymous: true
        });

        defineRoute({
            path: '/forgotpasswordpin.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/gamegenres.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/games.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/gamesrecommended.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/gamestudios.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/gamesystems.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/home.html',
            dependencies: ['paper-tabs'],
            autoFocus: false,
            controller: 'scripts/indexpage'
        });

        defineRoute({
            path: '/index.html',
            dependencies: [],
            autoFocus: false,
            isDefaultRoute: true
        });

        defineRoute({
            path: '/itemdetails.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/itemlist.html',
            dependencies: ['paper-checkbox', 'scripts/alphapicker'],
            autoFocus: false,
            controller: 'scripts/itemlistpage'
        });

        defineRoute({
            path: '/kids.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/library.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/librarypathmapping.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/librarysettings.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/livetv.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/livetvguideprovider.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/livetvitems.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/livetvrecordinglist.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/livetvseriestimer.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/livetvsettings.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/livetvstatus.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/livetvtimer.html',
            dependencies: ['scrollStyles'],
            autoFocus: false
        });

        defineRoute({
            path: '/livetvtunerprovider-hdhomerun.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/livetvtunerprovider-m3u.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/livetvtunerprovider-satip.html',
            dependencies: ['paper-input', 'paper-checkbox'],
            autoFocus: false,
            roles: 'admin',
            controller: 'scripts/livetvtunerprovider-satip'
        });

        defineRoute({
            path: '/log.html',
            dependencies: ['paper-toggle-button'],
            roles: 'admin',
            controller: 'scripts/logpage'
        });

        defineRoute({
            path: '/login.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/metadata.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/metadataadvanced.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/metadataimages.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/metadatanfo.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/metadatasubtitles.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/movies.html',
            dependencies: ['paper-tabs', 'paper-checkbox', 'paper-fab', 'scripts/alphapicker'],
            autoFocus: false,
            controller: 'scripts/moviesrecommended'
        });

        defineRoute({
            path: '/music.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/mypreferencesdisplay.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/mypreferenceshome.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/mypreferenceslanguages.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/mypreferencesmenu.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/myprofile.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/mysync.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/mysyncjob.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/mysyncsettings.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/notificationlist.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/notificationsetting.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/notificationsettings.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/nowplaying.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/photos.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/playbackconfiguration.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/playlists.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/plugincatalog.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/plugins.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/reports.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/scheduledtask.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/scheduledtasks.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/search.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/secondaryitems.html',
            dependencies: [],
            autoFocus: false,
            controller: 'scripts/secondaryitems'
        });

        defineRoute({
            path: '/selectserver.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/serversecurity.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/shared.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/streamingsettings.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/support.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/supporterkey.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/syncactivity.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/syncjob.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/syncsettings.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/tv.html',
            dependencies: ['paper-tabs', 'paper-checkbox'],
            autoFocus: false,
            controller: 'scripts/tvrecommended'
        });

        defineRoute({
            path: '/useredit.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/userlibraryaccess.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/usernew.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/userparentalcontrol.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/userpassword.html',
            dependencies: [],
            autoFocus: false
        });

        defineRoute({
            path: '/userprofiles.html',
            dependencies: [],
            autoFocus: false,
            roles: 'admin'
        });

        defineRoute({
            path: '/wizardagreement.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/wizardfinish.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/wizardlibrary.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/wizardlivetvguide.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/wizardlivetvtuner.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/wizardservice.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/wizardsettings.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/wizardstart.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/wizarduser.html',
            dependencies: [],
            autoFocus: false,
            anonymous: true
        });

        defineRoute({
            path: '/configurationpage',
            dependencies: ['jQuery'],
            autoFocus: false,
            enableCache: false,
            enableContentQueryString: true,
            roles: 'admin'
        });

        defineRoute({
            path: '/',
            isDefaultRoute: true,
            autoFocus: false,
            dependencies: []
        });
    }

    function onAppReady() {

        require(['scripts/mediaplayer'], function () {

            MediaPlayer.init();
        });

        console.log('Begin onAppReady');

        var deps = [];

        deps.push('imageLoader');
        deps.push('router');
        deps.push('layoutManager');

        if (!(AppInfo.isNativeApp && browserInfo.android)) {
            document.documentElement.classList.add('minimumSizeTabs');
        }

        // Do these now to prevent a flash of content
        if (AppInfo.isNativeApp && browserInfo.android) {
            deps.push('css!devices/android/android.css');
        } else if (AppInfo.isNativeApp && browserInfo.safari) {
            deps.push('css!devices/ios/ios.css');
        } else if (AppInfo.isNativeApp && browserInfo.edge) {
            deps.push('css!devices/windowsphone/wp.css');
        } else if (!browserInfo.android) {
            deps.push('css!devices/android/android.css');
        }

        loadTheme();

        if (browserInfo.safari && browserInfo.mobile) {
            initFastClick();
        }

        if (Dashboard.isRunningInCordova()) {
            deps.push('registrationservices');

            deps.push('cordova/back');

            if (browserInfo.android) {
                deps.push('cordova/android/androidcredentials');
                deps.push('cordova/android/links');
            }
        }

        if (browserInfo.msie) {
            deps.push('devices/ie/ie');
        }

        deps.push('scripts/search');
        deps.push('scripts/librarylist');
        deps.push('scripts/librarymenu');

        deps.push('css!css/card.css');

        require(deps, function (imageLoader, pageObjects, layoutManager) {

            console.log('Loaded dependencies in onAppReady');

            imageLoader.enableFade = browserInfo.animate && !browserInfo.mobile;
            window.ImageLoader = imageLoader;

            layoutManager.init();

            //$.mobile.initializePage();
            window.Emby = {};
            window.Emby.Page = pageObjects;
            window.Emby.TransparencyLevel = pageObjects.TransparencyLevel;
            defineCoreRoutes();
            Emby.Page.start({
                click: true,
                hashbang: AppInfo.enableHashBang
            });

            var postInitDependencies = [];

            postInitDependencies.push('scripts/thememediaplayer');
            postInitDependencies.push('scripts/remotecontrol');
            postInitDependencies.push('css!css/notifications.css');
            postInitDependencies.push('css!css/chromecast.css');
            postInitDependencies.push('scripts/autobackdrops');

            if (Dashboard.isRunningInCordova()) {

                if (browserInfo.android) {
                    postInitDependencies.push('cordova/android/mediasession');
                    postInitDependencies.push('cordova/android/chromecast');

                } else {
                    postInitDependencies.push('cordova/volume');
                }

                if (browserInfo.safari) {

                    postInitDependencies.push('cordova/ios/chromecast');

                    postInitDependencies.push('cordova/ios/orientation');

                    if (Dashboard.capabilities().SupportsSync) {

                        postInitDependencies.push('cordova/ios/backgroundfetch');
                    }
                }

            } else if (browserInfo.chrome) {
                postInitDependencies.push('scripts/chromecast');
            }

            if (AppInfo.enableNowPlayingBar) {
                postInitDependencies.push('scripts/nowplayingbar');
            }

            if (AppInfo.isNativeApp && browserInfo.safari) {

                postInitDependencies.push('cordova/ios/tabbar');
            }

            postInitDependencies.push('components/remotecontrolautoplay');

            // Prefer custom font over Segoe if on desktop windows
            if (!browserInfo.mobile && navigator.userAgent.toLowerCase().indexOf('windows') != -1) {
                //postInitDependencies.push('opensansFont');
                postInitDependencies.push('robotoFont');
            }

            require(postInitDependencies);
        });
    }

    function getCordovaHostingAppInfo() {

        return new Promise(function (resolve, reject) {

            document.addEventListener("deviceready", function () {

                cordova.getAppVersion.getVersionNumber(function (appVersion) {

                    require(['appStorage'], function (appStorage) {

                        var name = browserInfo.android ? "Emby for Android Mobile" : (browserInfo.safari ? "Emby for iOS" : "Emby Mobile");

                        // Remove special characters
                        var cleanDeviceName = device.model.replace(/[^\w\s]/gi, '');

                        var deviceId = null;

                        if (window.MainActivity) {

                            deviceId = appStorage.getItem('legacyDeviceId');

                            if (!deviceId) {
                                deviceId = MainActivity.getLegacyDeviceId();
                                appStorage.setItem('legacyDeviceId', deviceId);
                            }
                        }

                        resolve({
                            deviceId: deviceId || device.uuid,
                            deviceName: cleanDeviceName,
                            appName: name,
                            appVersion: appVersion
                        });
                    });

                });

            }, false);
        });
    }

    function getWebHostingAppInfo() {

        return new Promise(function (resolve, reject) {

            require(['appStorage'], function (appStorage) {
                var deviceName;

                if (browserInfo.chrome) {
                    deviceName = "Chrome";
                } else if (browserInfo.edge) {
                    deviceName = "Edge";
                } else if (browserInfo.firefox) {
                    deviceName = "Firefox";
                } else if (browserInfo.msie) {
                    deviceName = "Internet Explorer";
                } else {
                    deviceName = "Web Browser";
                }

                if (browserInfo.version) {
                    deviceName += " " + browserInfo.version;
                }

                if (browserInfo.ipad) {
                    deviceName += " Ipad";
                } else if (browserInfo.iphone) {
                    deviceName += " Iphone";
                } else if (browserInfo.android) {
                    deviceName += " Android";
                }

                function onDeviceAdAcquired(id) {

                    resolve({
                        deviceId: id,
                        deviceName: deviceName,
                        appName: "Emby Web Client",
                        appVersion: window.dashboardVersion
                    });
                }

                var deviceIdKey = '_deviceId1';
                var deviceId = appStorage.getItem(deviceIdKey);

                if (deviceId) {
                    onDeviceAdAcquired(deviceId);
                } else {
                    require(['cryptojs-sha1'], function () {
                        var keys = [];
                        keys.push(navigator.userAgent);
                        keys.push((navigator.cpuClass || ""));
                        keys.push(new Date().getTime());
                        var randomId = CryptoJS.SHA1(keys.join('|')).toString();
                        appStorage.setItem(deviceIdKey, randomId);
                        onDeviceAdAcquired(randomId);
                    });
                }
            });
        });
    }

    function getHostingAppInfo() {

        if (Dashboard.isRunningInCordova()) {
            return getCordovaHostingAppInfo();
        }

        return getWebHostingAppInfo();
    }

    initRequire();

    function onWebComponentsReady() {

        var initialDependencies = [];

        initialDependencies.push('browser');

        if (!window.Promise) {
            initialDependencies.push('native-promise-only');
        }

        require(initialDependencies, function (browser) {

            initRequireWithBrowser(browser);

            window.browserInfo = browser;

            setAppInfo();
            setDocumentClasses(browser);

            getHostingAppInfo().then(init);
        });
    }

    if ('registerElement' in document && 'content' in document.createElement('template')) {
        // Native web components support
        onWebComponentsReady();
    } else {
        document.addEventListener('WebComponentsReady', onWebComponentsReady);
        require(['webcomponentsjs']);
    }

})();

function pageClassOn(eventName, className, fn) {

    document.addEventListener(eventName, function (e) {

        var target = e.target;
        if (target.classList.contains(className)) {
            fn.call(target, e);
        }
    });
}

function pageIdOn(eventName, id, fn) {

    document.addEventListener(eventName, function (e) {

        var target = e.target;
        if (target.id == id) {
            fn.call(target, e);
        }
    });
}

pageClassOn('viewinit', "page", function () {

    var page = this;

    var current = page.getAttribute('data-theme');

    if (!current) {

        var newTheme;

        if (page.classList.contains('libraryPage')) {
            newTheme = 'b';
        } else {
            newTheme = 'a';
        }

        page.setAttribute("data-theme", newTheme);
        current = newTheme;
    }

    page.classList.add("ui-page");
    page.classList.add("ui-page-theme-" + current);

    var contents = page.querySelectorAll("div[data-role='content']");

    for (var i = 0, length = contents.length; i < length; i++) {
        var content = contents[i];
        //var theme = content.getAttribute("theme") || undefined;

        //content.classList.add("ui-content");
        //if (self.options.contentTheme) {
        //    content.classList.add("ui-body-" + (self.options.contentTheme));
        //}
        // Add ARIA role
        content.setAttribute("role", "main");
        content.classList.add("ui-content");
    }
});

pageClassOn('viewshow', "page", function () {

    var page = this;

    var currentTheme = page.classList.contains('ui-page-theme-a') ? 'a' : 'b';
    var docElem = document.documentElement;

    if (currentTheme == 'a') {
        docElem.classList.add('background-theme-a');
        docElem.classList.remove('background-theme-b');
        page.classList.add('ui-body-a');
        page.classList.remove('ui-body-b');
    } else {
        docElem.classList.add('background-theme-b');
        docElem.classList.remove('background-theme-a');
        page.classList.add('ui-body-b');
        page.classList.remove('ui-body-a');
    }

    if (currentTheme != 'a' && !browserInfo.mobile) {
        document.documentElement.classList.add('darkScrollbars');
    } else {
        document.documentElement.classList.remove('darkScrollbars');
    }

    var apiClient = window.ApiClient;

    Dashboard.ensureHeader(page);

    if (apiClient && apiClient.isLoggedIn() && !apiClient.isWebSocketOpen()) {
        Dashboard.refreshSystemInfoFromServer();
    }

});

window.addEventListener("beforeunload", function () {

    var apiClient = window.ApiClient;

    // Close the connection gracefully when possible
    if (apiClient && apiClient.isWebSocketOpen()) {

        var localActivePlayers = MediaController.getPlayers().filter(function (p) {

            return p.isLocalPlayer && p.isPlaying();
        });

        if (!localActivePlayers.length) {
            console.log('Sending close web socket command');
            apiClient.closeWebSocket();
        }
    }
});
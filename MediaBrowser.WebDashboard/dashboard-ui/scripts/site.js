(function () {

    $.ajaxSetup({
        crossDomain: true
    });

    if ($.browser.msie) {

        // This is unfortunately required due to IE's over-aggressive caching. 
        // https://github.com/MediaBrowser/MediaBrowser/issues/179
        $.ajaxSetup({
            cache: false
        });
    }
})();

// TODO: Deprecated in 1.9
$.support.cors = true;

$(document).one('click', WebNotifications.requestPermission);

var Dashboard = {

    jQueryMobileInit: function () {

        // Page
        //$.mobile.page.prototype.options.theme = "a";
        //$.mobile.page.prototype.options.headerTheme = "a";
        //$.mobile.page.prototype.options.contentTheme = "a";
        //$.mobile.page.prototype.options.footerTheme = "a";

        //$.mobile.button.prototype.options.theme = "c";
        //$.mobile.listview.prototype.options.dividerTheme = "b";

        //$.mobile.popup.prototype.options.theme = "c";
        $.mobile.popup.prototype.options.transition = "fade";

        if ($.browser.mobile) {
            $.mobile.defaultPageTransition = "slide";
        } else {
            $.mobile.defaultPageTransition = "none";
        }
        //$.mobile.collapsible.prototype.options.contentTheme = "a";

        // Make panels a little larger than the defaults
        $.mobile.panel.prototype.options.classes.modalOpen = "largePanelModalOpen ui-panel-dismiss-open";
        $.mobile.panel.prototype.options.classes.panel = "largePanel ui-panel";
    },

    isConnectMode: function () {

        if (AppInfo.isNativeApp) {
            return true;
        }

        var url = getWindowUrl().toLowerCase();

        return url.indexOf('mediabrowser.tv') != -1 ||
			url.indexOf('emby.media') != -1;
    },

    isRunningInCordova: function () {

        return window.appMode == 'cordova';
    },

    onRequestFail: function (e, data) {

        if (data.status == 401) {

            var url = data.url.toLowerCase();

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
        }
        Dashboard.hideLoadingMsg();

        if (!Dashboard.suppressAjaxErrors && data.type != 'GET') {

            setTimeout(function () {

                var msg = data.errorCode || Dashboard.defaultErrorMessage;

                Dashboard.showError(msg);

            }, 500);
        }
    },

    getCurrentUser: function () {

        if (!Dashboard.getUserPromise) {

            Dashboard.getUserPromise = window.ApiClient.getCurrentUser().fail(Dashboard.logout);
        }

        return Dashboard.getUserPromise;
    },

    validateCurrentUser: function () {

        Dashboard.getUserPromise = null;

        if (Dashboard.getCurrentUserId()) {
            Dashboard.getCurrentUser();
        }
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
        var urlLower = getWindowUrl().toLowerCase();
        var index = urlLower.indexOf('/web');
        if (index == -1) {
            index = urlLower.indexOf('/dashboard');
        }

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

        Dashboard.getUserPromise = null;
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
            ConnectionManager.logout().done(onLogoutDone);
        }
    },

    importCss: function (url) {
        if (document.createStyleSheet) {
            document.createStyleSheet(url);
        }
        else {
            $('<link rel="stylesheet" type="text/css" href="' + url + '" />').appendTo('head');
        }
    },

    showError: function (message) {

        $.mobile.loading('show', {
            text: message,
            textonly: true,
            textVisible: true
        });

        setTimeout(function () {
            $.mobile.loading('hide');
        }, 3000);
    },

    updateSystemInfo: function (info) {

        Dashboard.lastSystemInfo = info;

        Dashboard.ensureWebSocket();

        if (!Dashboard.initialServerVersion) {
            Dashboard.initialServerVersion = info.Version;
        }

        if (info.HasPendingRestart) {

            Dashboard.hideDashboardVersionWarning();

            Dashboard.getCurrentUser().done(function (currentUser) {

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

        ApiClient.cancelPackageInstallation(id).always(Dashboard.refreshSystemInfoFromServer);

    },

    showServerRestartWarning: function (systemInfo) {

        var html = '<span style="margin-right: 1em;">' + Globalize.translate('MessagePleaseRestart') + '</span>';

        if (systemInfo.CanSelfRestart) {
            html += '<button type="button" data-icon="refresh" onclick="$(this).buttonEnabled(false);Dashboard.restartServer();" data-theme="b" data-inline="true" data-mini="true">' + Globalize.translate('ButtonRestart') + '</button>';
        }

        Dashboard.showFooterNotification({ id: "serverRestartWarning", html: html, forceShow: true, allowHide: false });
    },

    hideServerRestartWarning: function () {

        $('#serverRestartWarning').remove();
    },

    showDashboardRefreshNotification: function () {

        var html = '<span style="margin-right: 1em;">' + Globalize.translate('MessagePleaseRefreshPage') + '</span>';

        html += '<button type="button" data-icon="refresh" onclick="$(this).buttonEnabled(false);Dashboard.reloadPage();" data-theme="b" data-inline="true" data-mini="true">' + Globalize.translate('ButtonRefresh') + '</button>';

        Dashboard.showFooterNotification({ id: "dashboardVersionWarning", html: html, forceShow: true, allowHide: false });
    },

    reloadPage: function () {

        var currentUrl = getWindowUrl().toLowerCase();
        var newUrl;

        // If they're on a plugin config page just go back to the dashboard
        // The plugin may not have been loaded yet, or could have been uninstalled
        if (currentUrl.indexOf('configurationpage') != -1) {
            newUrl = "dashboard.html";
        } else {
            newUrl = getWindowUrl();
        }

        window.location.href = newUrl;
    },

    hideDashboardVersionWarning: function () {

        $('#dashboardVersionWarning').remove();
    },

    showFooterNotification: function (options) {

        if (!AppInfo.enableFooterNotifications) {
            return;
        }

        var removeOnHide = !options.id;

        options.id = options.id || "notification" + new Date().getTime() + parseInt(Math.random());

        var footer = $(".footer").css("top", "initial").show();

        var parentElem = $('#footerNotifications', footer);

        var elem = $('#' + options.id, parentElem);

        if (!elem.length) {
            elem = $('<p id="' + options.id + '" class="footerNotification"></p>').appendTo(parentElem);
        }

        var onclick = removeOnHide ? "$(\"#" + options.id + "\").trigger(\"notification.remove\").remove();" : "$(\"#" + options.id + "\").trigger(\"notification.hide\").hide();";

        if (options.allowHide !== false) {
            options.html += "<span style='margin-left: 1em;'><button type='button' onclick='" + onclick + "' data-icon='delete' data-iconpos='notext' data-mini='true' data-inline='true' data-theme='b'>" + Globalize.translate('ButtonHide') + "</button></span>";
        }

        if (options.forceShow) {
            elem.slideDown(400);
        }

        elem.html(options.html).trigger("create");

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
                    footer.slideUp();
                }

            }, 50);

        });
    },

    getConfigurationPageUrl: function (name) {
        return "ConfigurationPage?name=" + encodeURIComponent(name);
    },

    navigate: function (url, preserveQueryString, transition) {

        if (!url) {
            throw new Error('url cannot be null or empty');
        }

        var queryString = getWindowLocationSearch();
        if (preserveQueryString && queryString) {
            url += queryString;
        }

        var options = {};

        if (transition) {
            options.transition = transition;
        }

        $.mobile.changePage(url, options);
    },

    showLoadingMsg: function () {
        $.mobile.loading("show");
    },

    hideLoadingMsg: function () {
        $.mobile.loading("hide");
    },

    getModalLoadingMsg: function () {

        var elem = $('.modalLoading');

        if (!elem.length) {

            elem = $('<div class="modalLoading"></div>').appendTo(document.body);

        }

        return elem;
    },

    showModalLoadingMsg: function () {
        Dashboard.showLoadingMsg();
        Dashboard.getModalLoadingMsg().show();
    },

    hideModalLoadingMsg: function () {
        Dashboard.getModalLoadingMsg().hide();
        Dashboard.hideLoadingMsg();
    },

    processPluginConfigurationUpdateResult: function () {

        Dashboard.hideLoadingMsg();

        Dashboard.alert("Settings saved.");
    },

    defaultErrorMessage: Globalize.translate('DefaultErrorMessage'),

    processServerConfigurationUpdateResult: function (result) {

        Dashboard.hideLoadingMsg();

        Dashboard.alert(Globalize.translate('MessageSettingsSaved'));
    },

    alert: function (options) {

        if (typeof options == "string") {

            var message = options;

            $.mobile.loading('show', {
                text: message,
                textonly: true,
                textVisible: true
            });

            setTimeout(function () {
                $.mobile.loading('hide');
            }, 3000);

            return;
        }

        // Cordova
        if (navigator.notification && navigator.notification.alert && options.message.indexOf('<') == -1) {

            navigator.notification.alert(options.message, options.callback || function () { }, options.title || Globalize.translate('HeaderAlert'));

        } else {
            Dashboard.confirmInternal(options.message, options.title || Globalize.translate('HeaderAlert'), false, options.callback);
        }
    },

    confirm: function (message, title, callback) {

        // Cordova
        if (navigator.notification && navigator.notification.alert && message.indexOf('<') == -1) {

            var buttonLabels = [Globalize.translate('ButtonOk'), Globalize.translate('ButtonCancel')];

            navigator.notification.confirm(message, function (index) {

                callback(index == 1);

            }, title || Globalize.translate('HeaderAlert'), buttonLabels.join(','));

        } else {
            Dashboard.confirmInternal(message, title, true, callback);
        }
    },

    confirmInternal: function (message, title, showCancel, callback) {

        $('.confirmFlyout').popup("close").remove();

        var html = '<div data-role="popup" class="confirmFlyout" style="max-width:500px;" data-theme="a">';

        html += '<div class="ui-bar-a" style="text-align:center;">';
        html += '<h3 style="padding: 0 1em;">' + title + '</h3>';
        html += '</div>';

        html += '<div style="padding: 1em;">';

        html += '<div style="padding: 1em .25em;margin: 0;">';
        html += message;
        html += '</div>';

        html += '<p><button type="button" data-icon="check" onclick="$(\'.confirmFlyout\')[0].confirm=true;$(\'.confirmFlyout\').popup(\'close\');" data-theme="b">' + Globalize.translate('ButtonOk') + '</button></p>';

        if (showCancel) {
            html += '<p><button type="button" data-icon="delete" onclick="$(\'.confirmFlyout\').popup(\'close\');" data-theme="a">' + Globalize.translate('ButtonCancel') + '</button></p>';
        }

        html += '</div>';

        html += '</div>';

        $(document.body).append(html);

        $('.confirmFlyout').popup({ history: false }).trigger('create').popup("open").on("popupafterclose", function () {

            if (callback) {
                callback(this.confirm == true);
            }

            $(this).off("popupafterclose").remove();
        });
    },

    refreshSystemInfoFromServer: function () {

        var apiClient = ApiClient;

        if (apiClient && apiClient.accessToken()) {
            if (apiClient.enableFooterNotifications) {
                apiClient.getSystemInfo().done(function (info) {

                    Dashboard.updateSystemInfo(info);
                });
            } else {
                Dashboard.ensureWebSocket();
            }
        }
    },

    restartServer: function () {

        Dashboard.suppressAjaxErrors = true;
        Dashboard.showLoadingMsg();

        ApiClient.restartServer().done(function () {

            setTimeout(function () {
                Dashboard.reloadPageWhenServerAvailable();
            }, 250);

        }).fail(function () {
            Dashboard.suppressAjaxErrors = false;
        });
    },

    reloadPageWhenServerAvailable: function (retryCount) {

        // Don't use apiclient method because we don't want it reporting authentication under the old version
        ApiClient.getJSON(ApiClient.getUrl("System/Info")).done(function (info) {

            // If this is back to false, the restart completed
            if (!info.HasPendingRestart) {
                Dashboard.reloadPage();
            } else {
                Dashboard.retryReload(retryCount);
            }

        }).fail(function () {
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

    getPluginSecurityInfo: function () {

        var apiClient = ApiClient;

        if (!apiClient) {

            var deferred = $.Deferred();
            deferred.reject();
            return deferred.promise();
        }

        if (!Dashboard.getPluginSecurityInfoPromise) {

            var deferred = $.Deferred();

            // Don't let this blow up the dashboard when it fails
            apiClient.ajax({
                type: "GET",
                url: apiClient.getUrl("Plugins/SecurityInfo"),
                dataType: 'json',

                error: function () {
                    // Don't show normal dashboard errors
                }

            }).done(function (result) {
                deferred.resolveWith(null, [result]);
            });

            Dashboard.getPluginSecurityInfoPromise = deferred;
        }

        return Dashboard.getPluginSecurityInfoPromise;
    },

    resetPluginSecurityInfo: function () {
        Dashboard.getPluginSecurityInfoPromise = null;
    },

    ensureHeader: function (page) {

        if (page.hasClass('standalonePage') && !page.hasClass('noHeaderPage')) {

            Dashboard.renderHeader(page);
        }
    },

    renderHeader: function (page) {

        var header = $('.header', page);

        if (!header.length) {
            var headerHtml = '';

            headerHtml += '<div class="header">';

            headerHtml += '<a class="logo" href="index.html" style="text-decoration:none;font-size: 22px;">';

            if (page.hasClass('standalonePage')) {

                headerHtml += '<img class="imgLogoIcon" src="css/images/mblogoicon.png" />';
                headerHtml += '<span class="logoLibraryMenuButtonText">EMBY</span>';
            }

            headerHtml += '</a>';

            headerHtml += '</div>';
            page.prepend(headerHtml);
        }
    },

    ensureToolsMenu: function (page) {

        var sidebar = $('.toolsSidebar', page);

        if (!sidebar.length) {

            var html = '<div class="content-secondary toolsSidebar">';

            html += '<div class="sidebarLinks">';

            var items = Dashboard.getToolsMenuLinks(page);

            var i, length, item;
            var menuHtml = '';

            for (i = 0, length = items.length; i < length; i++) {

                item = items[i];

                if (item.divider) {
                    menuHtml += "<div class='sidebarDivider'></div>";
                }

                if (item.href) {

                    var style = item.color ? ' style="color:' + item.color + '"' : '';

                    if (item.selected) {
                        menuHtml += '<a class="sidebarLink selectedSidebarLink" href="' + item.href + '">';
                    } else {
                        menuHtml += '<a data-transition="none" class="sidebarLink" href="' + item.href + '">';
                    }

                    var icon = item.icon;

                    if (icon) {
                        if (icon.indexOf('fa') == 0) {
                            menuHtml += '<span class="fa ' + icon + ' sidebarLinkIcon"' + style + '></span>';
                        } else {
                            menuHtml += '<i class="material-icons sidebarLinkIcon"' + style + '>' + icon + '</i>';
                        }
                    }

                    menuHtml += '<span class="sidebarLinkText">';
                    menuHtml += item.name;
                    menuHtml += '</span>';
                    menuHtml += '</a>';
                } else {

                    menuHtml += '<div class="sidebarHeader">';
                    menuHtml += item.name;
                    menuHtml += '</div>';
                }
            }

            html += menuHtml;
            // sidebarLinks
            html += '</div>';

            // content-secondary
            html += '</div>';

            html += '<div data-role="panel" id="dashboardPanel" class="dashboardPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="a">';

            html += '<p class="libraryPanelHeader" style="margin: 15px 0 15px 20px;"><a href="index.html" data-transition="none" class="imageLink"><img src="css/images/mblogoicon.png" /><span style="color:#333;">EMBY</span></a></p>';

            html += '<div class="sidebarLinks">';
            html += menuHtml;
            // sidebarLinks

            html += '<div class="sidebarDivider"></div>';
            html += '<div class="userMenuOptions">';

            if (Dashboard.isConnectMode()) {
                html += '<a class="sidebarLink" data-itemid="selectserver" href="selectserver.html"><span class="fa fa-globe sidebarLinkIcon"></span>';
                html += '<span class="sidebarLinkText">';
                html += Globalize.translate('ButtonSelectServer');
                html += '</span>';
                html += '</a>';
            }

            html += '<a class="sidebarLink" data-itemid="logout" href="#" onclick="Dashboard.logout();"><span class="fa fa-sign-out sidebarLinkIcon"></span>';
            html += '<span class="sidebarLinkText">';
            html += Globalize.translate('ButtonSignOut');
            html += '</span>';
            html += '</a>';

            html += '</div>';

            html += '</div>';
            html += '</div>';

            $('.content-primary', page).before(html);
            $(page).trigger('create');
        }
    },

    getToolsMenuLinks: function (page) {

        var pageElem = page[0];

        var isServicesPage = page.hasClass('appServicesPage');
        var context = getParameterByName('context');

        return [{
            name: Globalize.translate('TabServer'),
            href: "dashboard.html",
            selected: page.hasClass("dashboardHomePage"),
            icon: 'fa-dashboard',
            color: '#38c'
        }, {
            name: Globalize.translate('TabDevices'),
            href: "devices.html",
            selected: page.hasClass("devicesPage"),
            icon: 'fa-tablet',
            color: '#ECA403'
        }, {
            name: Globalize.translate('TabUsers'),
            href: "userprofiles.html",
            selected: page.hasClass("userProfilesPage"),
            icon: 'fa-users',
            color: '#679C34'
        }, {
            name: Globalize.translate('TabLibrary'),
            divider: true,
            href: "library.html",
            selected: page.hasClass("mediaLibraryPage"),
            icon: 'fa-film'
        }, {
            name: Globalize.translate('TabMetadata'),
            href: "metadata.html",
            selected: page.hasClass('metadataConfigurationPage'),
            icon: 'fa-file-text'
        }, {
            name: Globalize.translate('TabPlayback'),
            href: "playbackconfiguration.html",
            selected: page.hasClass('playbackConfigurationPage'),
            icon: 'fa-play-circle'
        }, {
            name: Globalize.translate('TabSync'),
            href: "syncactivity.html",
            selected: page.hasClass('syncConfigurationPage') || (isServicesPage && context == 'sync'),
            icon: 'fa-refresh'
        }, {
            divider: true,
            name: Globalize.translate('TabExtras')
        }, {
            name: Globalize.translate('TabAutoOrganize'),
            href: "autoorganizelog.html",
            selected: page.hasClass("organizePage"),
            icon: 'fa-files-o',
            color: '#01C0DD'
        }, {
            name: Globalize.translate('TabDLNA'),
            href: "dlnasettings.html",
            selected: page.hasClass("dlnaPage"),
            icon: 'fa-film',
            color: '#E5342E'
        }, {
            name: Globalize.translate('TabLiveTV'),
            href: "livetvstatus.html",
            selected: page.hasClass("liveTvSettingsPage") || (isServicesPage && context == 'livetv'),
            icon: 'fa-video-camera',
            color: '#293AAE'
        }, {
            name: Globalize.translate('TabNotifications'),
            href: "notificationsettings.html",
            selected: page.hasClass("notificationConfigurationPage"),
            icon: 'fa-wifi',
            color: 'brown'
        }, {
            name: Globalize.translate('TabPlugins'),
            href: "plugins.html",
            selected: page.hasClass("pluginConfigurationPage"),
            icon: 'fa-plus-circle',
            color: '#9D22B1'
        }, {
            divider: true,
            name: Globalize.translate('TabExpert')
        }, {
            name: Globalize.translate('TabAdvanced'),
            href: "advanced.html",
            selected: page.hasClass("advancedConfigurationPage"),
            icon: 'fa-gears',
            color: '#F16834'
        }, {
            name: Globalize.translate('TabScheduledTasks'),
            href: "scheduledtasks.html",
            selected: page.hasClass("scheduledTasksConfigurationPage"),
            icon: 'fa-clock-o',
            color: '#38c'
        }, {
            name: Globalize.translate('TabHelp'),
            divider: true,
            href: "support.html",
            selected: pageElem.id == "supportPage" || pageElem.id == "logPage" || pageElem.id == "supporterPage" || pageElem.id == "supporterKeyPage" || pageElem.id == "aboutPage",
            icon: 'fa-info-circle',
            color: '#679C34'
        }];

    },

    ensureWebSocket: function () {

        if (ApiClient.isWebSocketOpenOrConnecting() || !ApiClient.isWebSocketSupported()) {
            return;
        }

        ApiClient.openWebSocket();

        if (!Dashboard.isConnectMode()) {
            ApiClient.reportCapabilities(Dashboard.capabilities());
        }
    },

    processGeneralCommand: function (cmd) {

        // Full list
        // https://github.com/MediaBrowser/MediaBrowser/blob/master/MediaBrowser.Model/Session/GeneralCommand.cs#L23

        switch (cmd.Name) {

            case 'GoHome':
                Dashboard.navigate('index.html');
                break;
            case 'GoToSettings':
                Dashboard.navigate('dashboard.html');
                break;
            case 'DisplayContent':
                Dashboard.onBrowseCommand(cmd.Arguments);
                break;
            case 'GoToSearch':
                Search.showSearchPanel($.mobile.activePage);
                break;
            case 'DisplayMessage':
                {
                    var args = cmd.Arguments;

                    if (args.TimeoutMs) {
                        Dashboard.showFooterNotification({ html: "<div><b>" + args.Header + "</b></div>" + args.Text, timeout: args.TimeoutMs });
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
        else if (msg.MessageType === "UserDeleted") {
            Dashboard.validateCurrentUser();
        }
        else if (msg.MessageType === "SystemInfo") {
            Dashboard.updateSystemInfo(msg.Data);
        }
        else if (msg.MessageType === "RestartRequired") {
            Dashboard.updateSystemInfo(msg.Data);
        }
        else if (msg.MessageType === "UserUpdated" || msg.MessageType === "UserConfigurationUpdated") {
            var user = msg.Data;

            if (user.Id == Dashboard.getCurrentUserId()) {

                Dashboard.validateCurrentUser();
                $('.currentUsername').html(user.Name);
            }

        }
        else if (msg.MessageType === "PackageInstallationCompleted") {
            Dashboard.getCurrentUser().done(function (currentUser) {

                if (currentUser.Policy.IsAdministrator) {
                    Dashboard.showPackageInstallNotification(msg.Data, "completed");
                    Dashboard.refreshSystemInfoFromServer();
                }
            });
        }
        else if (msg.MessageType === "PackageInstallationFailed") {
            Dashboard.getCurrentUser().done(function (currentUser) {

                if (currentUser.Policy.IsAdministrator) {
                    Dashboard.showPackageInstallNotification(msg.Data, "failed");
                    Dashboard.refreshSystemInfoFromServer();
                }
            });
        }
        else if (msg.MessageType === "PackageInstallationCancelled") {
            Dashboard.getCurrentUser().done(function (currentUser) {

                if (currentUser.Policy.IsAdministrator) {
                    Dashboard.showPackageInstallNotification(msg.Data, "cancelled");
                    Dashboard.refreshSystemInfoFromServer();
                }
            });
        }
        else if (msg.MessaapiclientcgeType === "PackageInstalling") {
            Dashboard.getCurrentUser().done(function (currentUser) {

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
            url = "itembynamedetails.html?id=" + cmd.ItemId;
        }
        else if (type == "musicgenre") {
            url = "itembynamedetails.html?id=" + cmd.ItemId;
        }
        else if (type == "gamegenre") {
            url = "itembynamedetails.html?id=" + cmd.ItemId;
        }
        else if (type == "studio") {
            url = "itembynamedetails.html?id=" + cmd.ItemId;
        }
        else if (type == "person") {
            url = "itembynamedetails.html?id=" + cmd.ItemId;
        }
        else if (type == "musicartist") {
            url = "itembynamedetails.html?id=" + cmd.ItemId;
        }

        if (url) {
            Dashboard.navigate(url);
            return;
        }

        ApiClient.getItem(Dashboard.getCurrentUserId(), cmd.ItemId).done(function (item) {

            Dashboard.navigate(LibraryBrowser.getHref(item, null, ''));

        });

    },

    showPackageInstallNotification: function (installation, status) {

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
                var btnId = "btnCancel" + installation.Id;
                html += '<button id="' + btnId + '" type="button" data-icon="delete" onclick="$(\'' + btnId + '\').buttonEnabled(false);Dashboard.cancelInstallation(\'' + installation.Id + '\');" data-theme="b" data-inline="true" data-mini="true">' + Globalize.translate('ButtonCancel') + '</button>';
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

        if (!newItems.length) {
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

        }).done(function (result) {

            var items = result.Items;

            for (var i = 0, length = Math.min(items.length, 2) ; i < length; i++) {

                var item = items[i];

                var notification = {
                    title: "New " + item.Type,
                    body: item.Name,
                    timeout: 5000
                };

                var imageTags = item.ImageTags || {};

                if (imageTags.Primary) {

                    notification.icon = ApiClient.getScaledImageUrl(item.Id, {
                        width: 60,
                        tag: imageTags.Primary,
                        type: "Primary"
                    });
                }

                WebNotifications.show(notification);
            }
        });
    },

    ensurePageTitle: function (page) {

        if (!page.hasClass('type-interior')) {
            return;
        }

        if ($('.pageTitle', page).length) {
            return;
        }

        var parent = $('.content-primary', page);

        if (!parent.length) {
            parent = $('.ui-content', page)[0];
        }

        var helpUrl = page.attr('data-helpurl');

        var html = '<div>';
        html += '<h1 class="pageTitle" style="display:inline-block;">' + (document.title || '&nbsp;') + '</h1>';

        if (helpUrl) {
            html += '<a href="' + helpUrl + '" target="_blank" class="accentButton accentButton-g" style="margin-top:-10px;"><i class="fa fa-info-circle"></i>' + Globalize.translate('ButtonHelp') + '</a>';
        }

        html += '</div>';

        $(parent).prepend(html);
    },

    setPageTitle: function (title) {

        $('.pageTitle', $.mobile.activePage).html(title);

        if (title) {
            document.title = title;
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

    populateLanguages: function (select, languages) {

        var html = "";

        html += "<option value=''></option>";

        for (var i = 0, length = languages.length; i < length; i++) {

            var culture = languages[i];

            html += "<option value='" + culture.TwoLetterISOLanguageName + "'>" + culture.DisplayName + "</option>";
        }

        $(select).html(html).selectmenu("refresh");
    },

    populateCountries: function (select, allCountries) {

        var html = "";

        html += "<option value=''></option>";

        for (var i = 0, length = allCountries.length; i < length; i++) {

            var culture = allCountries[i];

            html += "<option value='" + culture.TwoLetterISORegionName + "'>" + culture.DisplayName + "</option>";
        }

        $(select).html(html).selectmenu("refresh");
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
            "DisplayMessage"
        ];

    },

    isServerlessPage: function () {
        var url = getWindowUrl().toLowerCase();
        return url.indexOf('connectlogin.html') != -1 || url.indexOf('selectserver.html') != -1 || url.indexOf('login.html') != -1 || url.indexOf('forgotpassword.html') != -1 || url.indexOf('forgotpasswordpin.html') != -1;
    },

    capabilities: function () {

        var caps = {
            PlayableMediaTypes: ['Audio', 'Video'],

            SupportedCommands: Dashboard.getSupportedRemoteCommands(),
            SupportsPersistentIdentifier: AppInfo.isNativeApp,
            SupportsMediaControl: true,
            SupportedLiveMediaTypes: ['Audio', 'Video']
        };

        if (Dashboard.isRunningInCordova() && $.browser.android) {
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

                quality -= 20;

                if (isBackdrop) {
                    quality -= 20;
                }

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
            options.backgroundColor = '#1f1f1f';
        }
    },

    getAppInfo: function (appName, deviceId, deviceName) {

        function generateDeviceName() {

            var name = "Web Browser";

            if ($.browser.chrome) {
                name = "Chrome";
            } else if ($.browser.safari) {
                name = "Safari";
            } else if ($.browser.msie) {
                name = "Internet Explorer";
            } else if ($.browser.opera) {
                name = "Opera";
            } else if ($.browser.mozilla) {
                name = "Firefox";
            }

            if ($.browser.version) {
                name += " " + $.browser.version;
            }

            if ($.browser.ipad) {
                name += " Ipad";
            } else if ($.browser.iphone) {
                name += " Iphone";
            } else if ($.browser.android) {
                name += " Android";
            }
            return name;
        }

        var appVersion = window.dashboardVersion;
        appName = appName || "Emby Web Client";

        deviceName = deviceName || generateDeviceName();

        var seed = [];
        var keyName = 'randomId';

        deviceId = deviceId || MediaBrowser.generateDeviceId(keyName, seed.join(','));

        return {
            appName: appName,
            appVersion: appVersion,
            deviceName: deviceName,
            deviceId: deviceId
        };
    },

    loadSwipebox: function () {

        var deferred = DeferredBuilder.Deferred();

        require([
            'thirdparty/swipebox-master/js/jquery.swipebox.min',
            'css!thirdparty/swipebox-master/css/swipebox.min'
        ], function () {

            deferred.resolve();
        });
        return deferred.promise();
    },

    loadLocalAssetManager: function () {

        var deferred = DeferredBuilder.Deferred();

        var file = 'thirdparty/apiclient/localassetmanager';

        if (AppInfo.isNativeApp && $.browser.android) {
            file = 'thirdparty/cordova/android/localassetmanager';
        }

        require([
            file
        ], function () {

            deferred.resolve();
        });
        return deferred.promise();
    },

    ready: function (fn) {

        if (Dashboard.initPromiseDone) {
            fn();
            return;
        }

        Dashboard.initPromise.done(fn);
    },

    firePageEvent: function (page, name) {

        Dashboard.ready(function () {
            $(page).trigger(name);
        });
    },

    loadExternalPlayer: function () {

        var deferred = DeferredBuilder.Deferred();

        require(['scripts/externalplayer.js'], function () {

            if (Dashboard.isRunningInCordova()) {
                require(['thirdparty/cordova/externalplayer.js'], function () {

                    deferred.resolve();
                });
            } else {
                deferred.resolve();
            }
        });

        return deferred.promise();
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

        AppInfo.enableAppStorePolicy = isCordova;

        var isIOS = $.browser.safari || $.browser.ipad || $.browser.iphone;
        var isAndroid = $.browser.android;
        var isMobile = $.browser.mobile;

        if (isIOS) {

            if (isMobile) {
                AppInfo.hasLowImageBandwidth = true;
            }

            if (isCordova) {
                AppInfo.enableBottomTabs = true;
                AppInfo.cardMargin = 'mediumCardMargin';

            } else {
                if (isMobile) {

                    AppInfo.enableDetailPageChapters = false;
                    AppInfo.enableDetailsMenuImages = false;
                    AppInfo.enableMovieHomeSuggestions = false;
                    AppInfo.cardMargin = 'largeCardMargin';

                    AppInfo.forcedImageFormat = 'jpg';
                }
            }
        }
        else {

            if (!$.browser.tv) {
                AppInfo.enableHeadRoom = true;
            }
        }

        AppInfo.enableMusicSongsTab = true;

        if (!AppInfo.hasLowImageBandwidth) {
            AppInfo.enableLatestChannelItems = true;
            AppInfo.enableStudioTabs = true;
            AppInfo.enablePeopleTabs = true;
            AppInfo.enableTvEpisodesTab = true;
            AppInfo.enableMusicArtistsTab = true;
            AppInfo.enableMovieTrailersTab = true;
        }

        if (isCordova) {
            AppInfo.enableAppLayouts = true;
            AppInfo.hasKnownExternalPlayerSupport = true;
            AppInfo.isNativeApp = true;
        }
        else {
            AppInfo.enableFooterNotifications = true;
            AppInfo.enableSupporterMembership = true;

            if (!isAndroid && !isIOS) {
                AppInfo.enableAppLayouts = true;
            }
        }

        AppInfo.enableUserImage = true;
        AppInfo.hasPhysicalVolumeButtons = isCordova || isMobile;

        AppInfo.enableBackButton = (isIOS && window.navigator.standalone) || (isCordova && isIOS);
        AppInfo.supportsFullScreen = isCordova && isAndroid;
    }

    function initializeApiClient(apiClient) {

        apiClient.enableAppStorePolicy = AppInfo.enableAppStorePolicy;

        $(apiClient).off('.dashboard')
            .on("websocketmessage.dashboard", Dashboard.onWebSocketMessageReceived)
            .on('requestfail.dashboard', Dashboard.onRequestFail);
    }

    //localStorage.clear();
    function createConnectionManager(capabilities) {

        var credentialProvider = new MediaBrowser.CredentialProvider();

        window.ConnectionManager = new MediaBrowser.ConnectionManager(Logger, credentialProvider, AppInfo.appName, AppInfo.appVersion, AppInfo.deviceName, AppInfo.deviceId, capabilities);

        $(ConnectionManager).on('apiclientcreated', function (e, newApiClient) {

            initializeApiClient(newApiClient);
        });

        var apiClient;

        if (Dashboard.isConnectMode()) {

            apiClient = ConnectionManager.getLastUsedApiClient();

            if (!Dashboard.isServerlessPage()) {

                if (apiClient && apiClient.serverAddress() && apiClient.getCurrentUserId() && apiClient.accessToken()) {

                    initializeApiClient(apiClient);
                }
            }

        } else {

            apiClient = new MediaBrowser.ApiClient(Logger, Dashboard.serverAddress(), AppInfo.appName, AppInfo.appVersion, AppInfo.deviceName, AppInfo.deviceId);
            apiClient.enableAutomaticNetworking = false;
            ConnectionManager.addApiClient(apiClient);
        }

        window.ApiClient = apiClient;

        if (window.ApiClient) {
            ApiClient.getDefaultImageQuality = Dashboard.getDefaultImageQuality;
            ApiClient.normalizeImageOptions = Dashboard.normalizeImageOptions;

            if (!AppInfo.isNativeApp) {
                Dashboard.importCss(ApiClient.getUrl('Branding/Css'));
            }
        }
    }

    function initFastClick() {

        requirejs(["thirdparty/fastclick"], function (FastClick) {

            FastClick.attach(document.body);

            // Have to work around this issue of fast click breaking the panel dismiss
            $(document.body).on('touchstart', '.ui-panel-dismiss', function () {
                $(this).trigger('click');
            });
        });

    }

    function onDocumentReady() {

        if (AppInfo.isTouchPreferred) {
            $(document.body).addClass('touch');
        }

        if ($.browser.safari && $.browser.mobile) {
            initFastClick();
        }

        if (AppInfo.cardMargin) {
            $(document.body).addClass(AppInfo.cardMargin);
        }

        if (!AppInfo.enableLatestChannelItems) {
            $(document.body).addClass('latestChannelItemsDisabled');
        }

        if (!AppInfo.enableStudioTabs) {
            $(document.body).addClass('studioTabDisabled');
        }

        if (!AppInfo.enablePeopleTabs) {
            $(document.body).addClass('peopleTabDisabled');
        }

        if (!AppInfo.enableTvEpisodesTab) {
            $(document.body).addClass('tvEpisodesTabDisabled');
        }

        if (!AppInfo.enableMusicSongsTab) {
            $(document.body).addClass('musicSongsTabDisabled');
        }

        if (!AppInfo.enableMusicArtistsTab) {
            $(document.body).addClass('musicArtistsTabDisabled');
        }

        if (!AppInfo.enableMovieTrailersTab) {
            $(document.body).addClass('movieTrailersTabDisabled');
        }

        if (AppInfo.enableBottomTabs) {
            $(document.body).addClass('bottomSecondaryNav');
        }

        if (!AppInfo.enableSupporterMembership) {
            $(document.body).addClass('supporterMembershipDisabled');
        }

        if (AppInfo.isNativeApp) {
            $(document).addClass('nativeApp');
        }

        if (AppInfo.enableBackButton) {
            $(document.body).addClass('enableBackButton');
        }

        var videoPlayerHtml = '<div id="mediaPlayer" data-theme="b" class="ui-bar-b" style="display: none;">';

        videoPlayerHtml += '<div class="videoBackdrop">';
        videoPlayerHtml += '<div id="videoPlayer">';

        videoPlayerHtml += '<div id="videoElement">';
        videoPlayerHtml += '<div id="play" class="status"></div>';
        videoPlayerHtml += '<div id="pause" class="status"></div>';
        videoPlayerHtml += '</div>';

        videoPlayerHtml += '<div class="videoTopControls hiddenOnIdle">';
        videoPlayerHtml += '<div class="videoTopControlsLogo"></div>';
        videoPlayerHtml += '<div class="videoAdvancedControls">';

        videoPlayerHtml += '<button class="mediaButton videoTrackControl previousTrackButton imageButton" title="Previous video" type="button" onclick="MediaPlayer.previousTrack();" data-role="none"><i class="fa fa-step-backward"></i></button>';
        videoPlayerHtml += '<button class="mediaButton videoTrackControl nextTrackButton imageButton" title="Next video" type="button" onclick="MediaPlayer.nextTrack();" data-role="none"><i class="fa fa-step-forward"></i></button>';

        // Embedding onclicks due to issues not firing in cordova safari
        videoPlayerHtml += '<button class="mediaButton videoAudioButton imageButton" title="Audio tracks" type="button" data-role="none" onclick="MediaPlayer.showAudioTracksFlyout();"><i class="fa fa-music"></i></button>';
        videoPlayerHtml += '<div data-role="popup" class="videoAudioPopup videoPlayerPopup" data-history="false" data-theme="b"></div>';

        videoPlayerHtml += '<button class="mediaButton videoSubtitleButton imageButton" title="Subtitles" type="button" data-role="none" onclick="MediaPlayer.showSubtitleMenu();"><i class="fa fa-text-width"></i></button>';
        videoPlayerHtml += '<div data-role="popup" class="videoSubtitlePopup videoPlayerPopup" data-history="false" data-theme="b"></div>';

        videoPlayerHtml += '<button class="mediaButton videoChaptersButton imageButton" title="Scenes" type="button" data-role="none" onclick="MediaPlayer.showChaptersFlyout();"><i class="fa fa-video-camera"></i></button>';
        videoPlayerHtml += '<div data-role="popup" class="videoChaptersPopup videoPlayerPopup" data-history="false" data-theme="b"></div>';

        videoPlayerHtml += '<button class="mediaButton videoQualityButton imageButton" title="Quality" type="button" data-role="none" onclick="MediaPlayer.showQualityFlyout();"><i class="fa fa-gear"></i></button>';
        videoPlayerHtml += '<div data-role="popup" class="videoQualityPopup videoPlayerPopup" data-history="false" data-theme="b"></div>';

        videoPlayerHtml += '<button class="mediaButton imageButton" title="Stop" type="button" onclick="MediaPlayer.stop();" data-role="none"><i class="fa fa-close"></i></button>';

        videoPlayerHtml += '</div>'; // videoAdvancedControls
        videoPlayerHtml += '</div>'; // videoTopControls

        // Create controls
        videoPlayerHtml += '<div class="videoControls hiddenOnIdle">';

        videoPlayerHtml += '<div class="nowPlayingInfo hiddenOnIdle">';
        videoPlayerHtml += '<div class="nowPlayingImage"></div>';
        videoPlayerHtml += '<div class="nowPlayingTabs"></div>';
        videoPlayerHtml += '</div>'; // nowPlayingInfo

        videoPlayerHtml += '<button id="video-previousTrackButton" class="mediaButton previousTrackButton videoTrackControl imageButton" title="Previous Track" type="button" onclick="MediaPlayer.previousTrack();" data-role="none"><i class="fa fa-step-backward"></i></button>';
        videoPlayerHtml += '<button id="video-playButton" class="mediaButton imageButton" title="Play" type="button" onclick="MediaPlayer.unpause();" data-role="none"><i class="fa fa-play"></i></button>';
        videoPlayerHtml += '<button id="video-pauseButton" class="mediaButton imageButton" title="Pause" type="button" onclick="MediaPlayer.pause();" data-role="none"><i class="fa fa-pause"></i></button>';
        videoPlayerHtml += '<button id="video-nextTrackButton" class="mediaButton nextTrackButton videoTrackControl imageButton" title="Next Track" type="button" onclick="MediaPlayer.nextTrack();" data-role="none"><i class="fa fa-step-forward"></i></button>';

        videoPlayerHtml += '<div class="positionSliderContainer sliderContainer">';
        videoPlayerHtml += '<input type="range" class="mediaSlider positionSlider slider" step=".001" min="0" max="100" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
        videoPlayerHtml += '</div>';

        videoPlayerHtml += '<div class="currentTime">--:--</div>';

        videoPlayerHtml += '<button id="video-muteButton" class="mediaButton muteButton imageButton" title="Mute" type="button" onclick="MediaPlayer.mute();" data-role="none"><i class="fa fa-volume-up"></i></button>';
        videoPlayerHtml += '<button id="video-unmuteButton" class="mediaButton unmuteButton imageButton" title="Unmute" type="button" onclick="MediaPlayer.unMute();" data-role="none"><i class="fa fa-volume-off"></i></button>';

        videoPlayerHtml += '<div class="volumeSliderContainer sliderContainer">';
        videoPlayerHtml += '<input type="range" class="mediaSlider volumeSlider slider" step=".05" min="0" max="1" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
        videoPlayerHtml += '</div>';

        videoPlayerHtml += '<button onclick="MediaPlayer.toggleFullscreen();" id="video-fullscreenButton" class="mediaButton fullscreenButton imageButton" title="Fullscreen" type="button" data-role="none"><i class="fa fa-expand"></i></button>';

        videoPlayerHtml += '</div>'; // videoControls

        videoPlayerHtml += '</div>'; // videoPlayer
        videoPlayerHtml += '</div>'; // videoBackdrop
        videoPlayerHtml += '</div>'; // mediaPlayer

        $(document.body).append(videoPlayerHtml);

        var mediaPlayerElem = $('#mediaPlayer', document.body);
        mediaPlayerElem.trigger('create');

        var footerHtml = '<div id="footer" class="footer" data-theme="b" class="ui-bar-b">';

        footerHtml += '<div id="footerNotifications"></div>';
        footerHtml += '</div>';

        $(document.body).append(footerHtml);

        $(window).on("beforeunload", function () {

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

        $(document).on('contextmenu', '.ui-popup-screen', function (e) {

            $('.ui-popup').popup('close');

            e.preventDefault();
            return false;
        });

        if (Dashboard.isRunningInCordova()) {
            requirejs(['thirdparty/cordova/connectsdk', 'scripts/registrationservices']);

            if ($.browser.android) {
                requirejs(['thirdparty/cordova/android/androidcredentials', 'thirdparty/cordova/android/immersive', 'thirdparty/cordova/android/filesystem']);
            } else {
                requirejs(['thirdparty/cordova/filesystem']);
            }

            if ($.browser.safari) {
                requirejs(['thirdparty/cordova/remotecontrols', 'thirdparty/cordova/ios/orientation']);
            }

        } else {
            if ($.browser.chrome) {
                requirejs(['scripts/chromecast']);
            }
            requirejs(['thirdparty/filesystem']);
        }
    }

    function init(deferred, capabilities, appName, deviceId, deviceName, resolveOnReady) {

        requirejs.config({
            map: {
                '*': {
                    'css': 'thirdparty/requirecss' // or whatever the path to require-css is
                }
            },
            urlArgs: "v=" + window.dashboardVersion
        });

        // Required since jQuery is loaded before requireJs
        define('jquery', [], function () {
            return jQuery;
        });

        //requirejs(['http://viblast.com/player/free-version/qy2fdwajo1/viblast.js']);

        setAppInfo();

        $.extend(AppInfo, Dashboard.getAppInfo(appName, deviceId, deviceName));

        createConnectionManager(capabilities);

        if (!resolveOnReady) {

            Dashboard.initPromiseDone = true;
            deferred.resolve();
        }

        $(function () {
            onDocumentReady();
            if (resolveOnReady) {
                Dashboard.initPromiseDone = true;
                deferred.resolve();
            }
        });
    }

    function initCordovaWithDeviceId(deferred, deviceId) {

        var screenWidth = Math.max(screen.height, screen.width);
        initCordovaWithDeviceProfile(deferred, deviceId, MediaPlayer.getDeviceProfile(screenWidth));
    }

    function initCordovaWithDeviceProfile(deferred, deviceId, deviceProfile) {

        if ($.browser.android) {
            requirejs(['thirdparty/cordova/android/imagestore.js']);
        } else {
            requirejs(['thirdparty/cordova/imagestore.js']);
        }

        var capablities = Dashboard.capabilities();

        capablities.DeviceProfile = deviceProfile;

        init(deferred, capablities, "Emby Mobile", deviceId, device.model, true);
    }

    function initCordova(deferred) {

        document.addEventListener("deviceready", function () {

            window.plugins.uniqueDeviceID.get(function (uuid) {

                initCordovaWithDeviceId(deferred, uuid);

            }, function () {

                // Failure. Use cordova uuid
                initCordovaWithDeviceId(deferred, device.uuid);
            });
        }, false);
    }

    var initDeferred = $.Deferred();
    Dashboard.initPromise = initDeferred.promise();

    if (Dashboard.isRunningInCordova()) {
        initCordova(initDeferred);
    } else {
        init(initDeferred, Dashboard.capabilities());
    }

})();

Dashboard.jQueryMobileInit();

$(document).on('pagecreate', ".page", function () {

    var page = $(this);

    var current = page.data('theme');
    if (!current) {

        var newTheme;

        if (page.hasClass('libraryPage')) {
            newTheme = 'b';
        } else {
            newTheme = 'a';
        }

        current = page.page("option", "theme");

        if (current && current != newTheme) {
            page.page("option", "theme", newTheme);
        }

        current = newTheme;
    }

    if (current == 'b') {
        $(document.body).addClass('darkScrollbars');
    } else {
        $(document.body).removeClass('darkScrollbars');
    }

}).on('pageinit', ".page", function () {

    var page = this;

    var require = this.getAttribute('data-require');

    if (require) {
        requirejs(require.split(','), function () {

            Dashboard.firePageEvent(page, 'pageinitdepends');
        });
    } else {
        Dashboard.firePageEvent(page, 'pageinitdepends');
    }

    $('.localnav a, .libraryViewNav a').attr('data-transition', 'none');

}).on('pagebeforeshow', ".page", function () {

    var page = this;
    var require = this.getAttribute('data-require');

    if (require) {
        requirejs(require.split(','), function () {

            Dashboard.firePageEvent(page, 'pagebeforeshowready');
        });
    } else {
        Dashboard.firePageEvent(page, 'pagebeforeshowready');
    }

}).on('pageshow', ".page", function () {

    var page = this;
    var require = this.getAttribute('data-require');

    if (require) {
        requirejs(require.split(','), function () {

            Dashboard.firePageEvent(page, 'pageshowbeginready');
        });
    } else {
        Dashboard.firePageEvent(page, 'pageshowbeginready');
    }

}).on('pageshowbeginready', ".page", function () {

    var page = $(this);

    var apiClient = window.ApiClient;

    if (apiClient && apiClient.accessToken() && Dashboard.getCurrentUserId()) {

        var isSettingsPage = page.hasClass('type-interior');

        if (isSettingsPage) {
            Dashboard.ensureToolsMenu(page);

            Dashboard.getCurrentUser().done(function (user) {

                if (!user.Policy.IsAdministrator) {
                    Dashboard.logout();
                    return;
                }
            });
        }
    }

    else {

        var isConnectMode = Dashboard.isConnectMode();

        if (isConnectMode) {

            if (!Dashboard.isServerlessPage()) {
                Dashboard.logout();
                return;
            }
        }

        if (!isConnectMode && this.id !== "loginPage" && !page.hasClass('forgotPasswordPage') && !page.hasClass('wizardPage')) {

            console.log('Not logged into server. Redirecting to login.');
            Dashboard.logout();
            return;
        }
    }

    Dashboard.firePageEvent(page, 'pageshowready');

    Dashboard.ensureHeader(page);
    Dashboard.ensurePageTitle(page);

    if (apiClient && !apiClient.isWebSocketOpen()) {
        Dashboard.refreshSystemInfoFromServer();
    }
});
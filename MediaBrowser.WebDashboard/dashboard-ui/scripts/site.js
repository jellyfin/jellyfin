(function () {

    function onOneDocumentClick() {

        document.removeEventListener('click', onOneDocumentClick);

        if (window.Notification) {
            Notification.requestPermission();
        }
    }
    document.addEventListener('click', onOneDocumentClick);

})();

var Dashboard = {

    filterHtml: function (html) {

        // replace the first instance
        html = html.replace('<!--', '');

        // replace the last instance
        var lastIndex = html.lastIndexOf('-->');

        if (lastIndex != -1) {
            html = html.substring(0, lastIndex) + html.substring(lastIndex + 3);
        }

        return Globalize.translateDocument(html, 'html');
    },

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

        Dashboard.ensureWebSocket();

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

        var html = '<span style="margin-right: 1em;">' + Globalize.translate('MessagePleaseRefreshPage') + '</span>';

        html += '<paper-button raised class="submit mini" onclick="this.disabled=\'disabled\';Dashboard.reloadPage();"><iron-icon icon="refresh"></iron-icon><span>' + Globalize.translate('ButtonRefresh') + '</span></paper-button>';

        Dashboard.showFooterNotification({ id: "dashboardVersionWarning", html: html, forceShow: true, allowHide: false });
    },

    reloadPage: function () {

        var currentUrl = window.location.href.toLowerCase();
        var newUrl;

        // If they're on a plugin config page just go back to the dashboard
        // The plugin may not have been loaded yet, or could have been uninstalled
        if (currentUrl.indexOf('configurationpage') != -1) {
            newUrl = "dashboard.html";
        } else {
            newUrl = window.location.href;
        }

        window.location.href = newUrl;
    },

    hideDashboardVersionWarning: function () {

        var elem = document.getElementById('dashboardVersionWarning');

        if (elem) {

            elem.parentNode.removeChild(elem);
        }
    },

    showFooterNotification: function (options) {

        if (!AppInfo.enableFooterNotifications) {
            return;
        }

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
            elem.slideDown(400);
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
                    footer.slideUp();
                }

            }, 50);

        });
    },

    getConfigurationPageUrl: function (name) {
        return "ConfigurationPage?name=" + encodeURIComponent(name);
    },

    navigate: function (url, preserveQueryString) {

        if (!url) {
            throw new Error('url cannot be null or empty');
        }

        var queryString = getWindowLocationSearch();
        if (preserveQueryString && queryString) {
            url += queryString;
        }

        var options = {};

        $.mobile.changePage(url, options);
    },

    showLoadingMsg: function () {

        Dashboard.loadingVisible = true;
        var elem = document.querySelector('.docspinner');

        if (elem) {

            // This is just an attempt to prevent the fade-in animation from running repeating and causing flickering
            elem.active = true;
            elem.classList.remove('hide');

        } else if (!Dashboard.loadingAdded) {

            Dashboard.loadingAdded = true;

            require(['paper-spinner'], function () {
                elem = document.createElement("paper-spinner");
                elem.classList.add('docspinner');

                document.body.appendChild(elem);
                elem.active = Dashboard.loadingVisible == true;
            });
        }
    },

    hideLoadingMsg: function () {

        Dashboard.loadingVisible = false;

        var elem = document.querySelector('.docspinner');

        if (elem) {

            elem.active = false;
            elem.classList.add('hide');
        }
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

        Dashboard.alert(Globalize.translate('MessageSettingsSaved'));
    },

    processServerConfigurationUpdateResult: function (result) {

        Dashboard.hideLoadingMsg();

        Dashboard.alert(Globalize.translate('MessageSettingsSaved'));
    },

    alert: function (options) {

        if (typeof options == "string") {

            require(['paper-toast'], function () {
                var message = options;

                Dashboard.toastId = Dashboard.toastId || 0;

                var id = 'toast' + (Dashboard.toastId++);

                var elem = document.createElement("paper-toast");
                elem.setAttribute('text', message);
                elem.id = id;

                document.body.appendChild(elem);

                // This timeout is obviously messy but it's unclear how to determine when the webcomponent is ready for use
                // element onload never fires
                setTimeout(function () {
                    elem.show();
                }, 300);

                setTimeout(function () {
                    elem.parentNode.removeChild(elem);
                }, 5300);

            });

            return;
        }

        // Cordova
        if (navigator.notification && navigator.notification.alert && options.message.indexOf('<') == -1) {

            navigator.notification.alert(options.message, options.callback || function () { }, options.title || Globalize.translate('HeaderAlert'));

        } else {
            require(['paper-dialog', 'fade-in-animation', 'fade-out-animation'], function () {
                Dashboard.confirmInternal(options.message, options.title || Globalize.translate('HeaderAlert'), false, options.callback);
            });
        }
    },

    confirm: function (message, title, callback) {

        // Cordova
        if (navigator.notification && navigator.notification.confirm && message.indexOf('<') == -1) {

            var buttonLabels = [Globalize.translate('ButtonOk'), Globalize.translate('ButtonCancel')];

            navigator.notification.confirm(message, function (index) {

                callback(index == 1);

            }, title || Globalize.translate('HeaderConfirm'), buttonLabels.join(','));

        } else {

            require(['paper-dialog', 'fade-in-animation', 'fade-out-animation'], function () {
                Dashboard.confirmInternal(message, title, true, callback);
            });
        }
    },

    confirmInternal: function (message, title, showCancel, callback) {

        var dlg = document.createElement('paper-dialog');

        dlg.setAttribute('with-backdrop', 'with-backdrop');
        dlg.setAttribute('role', 'alertdialog');
        dlg.entryAnimation = 'fade-in-animation';
        dlg.exitAnimation = 'fade-out-animation';
        dlg.setAttribute('with-backdrop', 'with-backdrop');

        var html = '';
        html += '<h2>' + title + '</h2>';
        html += '<div>' + message + '</div>';
        html += '<div class="buttons">';

        html += '<paper-button class="btnConfirm" dialog-confirm autofocus>' + Globalize.translate('ButtonOk') + '</paper-button>';

        if (showCancel) {
            html += '<paper-button dialog-dismiss>' + Globalize.translate('ButtonCancel') + '</paper-button>';
        }

        html += '</div>';

        dlg.innerHTML = html;
        document.body.appendChild(dlg);

        // Has to be assigned a z-index after the call to .open() 
        dlg.addEventListener('iron-overlay-closed', function (e) {

            var confirmed = dlg.closingReason.confirmed;
            dlg.parentNode.removeChild(dlg);

            if (callback) {
                callback(confirmed);
            }
        });

        dlg.open();
    },

    refreshSystemInfoFromServer: function () {

        var apiClient = ApiClient;

        if (apiClient && apiClient.accessToken()) {
            if (AppInfo.enableFooterNotifications) {
                apiClient.getSystemInfo().then(function (info) {

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

        require(['jqmpanel'], function () {
            var html = '<div data-role="panel" data-position="right" data-display="overlay" id="userFlyout" data-position-fixed="true" data-theme="a">';

            html += '<h3 class="userHeader">';

            html += '</h3>';

            html += '<form>';

            html += '<p class="preferencesContainer"></p>';

            html += '<p><button data-mini="true" type="button" onclick="Dashboard.logout();" data-icon="lock">' + Globalize.translate('ButtonSignOut') + '</button></p>';

            html += '</form>';
            html += '</div>';

            $(document.body).append(html);

            var userFlyout = document.querySelector('#userFlyout');
            ImageLoader.lazyChildren(userFlyout);

            $(userFlyout).panel({}).panel("open").on("panelclose", function () {

                $(this).off("panelclose").remove();
            });

            ConnectionManager.user(window.ApiClient).then(function (user) {
                Dashboard.updateUserFlyout(userFlyout, user);
            });
        });
    },

    updateUserFlyout: function (elem, user) {

        var html = '';
        var imgWidth = 48;

        if (user.imageUrl && AppInfo.enableUserImage) {
            var url = user.imageUrl;

            if (user.supportsImageParams) {
                url += "&width=" + (imgWidth * Math.max(window.devicePixelRatio || 1, 2));
            }

            html += '<div style="background-image:url(\'' + url + '\');width:' + imgWidth + 'px;height:' + imgWidth + 'px;background-size:contain;background-repeat:no-repeat;background-position:center center;border-radius:1000px;vertical-align:middle;margin-right:.8em;display:inline-block;"></div>';
        }
        html += user.name;

        var userHeader = elem.querySelector('.userHeader');
        userHeader.innerHTML = html;
        ImageLoader.lazyChildren(userHeader);

        html = '';

        if (user.localUser) {
            html += '<p><a data-mini="true" data-role="button" href="mypreferencesmenu.html?userId=' + user.localUser.Id + '" data-icon="gear">' + Globalize.translate('ButtonSettings') + '</button></a>';
        }

        $('.preferencesContainer', elem).html(html);
    },

    getPluginSecurityInfo: function () {

        var apiClient = ApiClient;

        if (!apiClient) {

            return new Promise(function (resolve, reject) {

                reject();
            });
        }

        var cachedInfo = Dashboard.pluginSecurityInfo;
        if (cachedInfo) {
            return new Promise(function (resolve, reject) {

                resolve(cachedInfo);
            });
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

            headerHtml += '<a class="logo" href="index.html" style="text-decoration:none;font-size: 22px;">';

            if (page.classList.contains('standalonePage')) {

                headerHtml += '<img class="imgLogoIcon" src="css/images/mblogoicon.png" />';
                headerHtml += '<span class="logoLibraryMenuButtonText">EMBY</span>';
            }

            headerHtml += '</a>';

            headerHtml += '</div>';
            $(page).prepend(headerHtml);
        }
    },

    getToolsMenuHtml: function (page) {

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
                    menuHtml += '<a class="sidebarLink" href="' + item.href + '">';
                }

                var icon = item.icon;

                if (icon) {
                    menuHtml += '<iron-icon icon="' + icon + '" class="sidebarLinkIcon"' + style + '></iron-icon>';
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

        return menuHtml;
    },

    ensureToolsMenu: function (page) {

        var sidebar = page.querySelector('.toolsSidebar');

        if (!sidebar) {

            var html = '<div class="content-secondary toolsSidebar">';

            html += '<div class="sidebarLinks">';

            html += Dashboard.getToolsMenuHtml(page);
            // sidebarLinks
            html += '</div>';

            // content-secondary
            html += '</div>';

            $('.content-primary', page).before(html);
            Events.trigger(page, 'create');
        }
    },

    getToolsMenuLinks: function (page) {

        var pageElem = page;

        var isServicesPage = page.classList.contains('appServicesPage');
        var context = getParameterByName('context');

        return [{
            name: Globalize.translate('TabServer'),
            href: "dashboard.html",
            selected: page.classList.contains("dashboardHomePage"),
            icon: 'dashboard',
            color: '#38c'
        }, {
            name: Globalize.translate('TabDevices'),
            href: "devices.html",
            selected: page.classList.contains("devicesPage"),
            icon: 'tablet',
            color: '#ECA403'
        }, {
            name: Globalize.translate('TabUsers'),
            href: "userprofiles.html",
            selected: page.classList.contains("userProfilesPage"),
            icon: 'people',
            color: '#679C34'
        }, {
            name: Globalize.translate('TabLibrary'),
            divider: true,
            href: "library.html",
            selected: page.classList.contains("librarySectionPage"),
            icon: 'video-library'
        }, {
            name: Globalize.translate('TabMetadata'),
            href: "metadata.html",
            selected: page.classList.contains('metadataConfigurationPage'),
            icon: 'insert-drive-file'
        }, {
            name: Globalize.translate('TabPlayback'),
            href: "playbackconfiguration.html",
            selected: page.classList.contains('playbackConfigurationPage'),
            icon: 'play-circle-filled'
        }, {
            name: Globalize.translate('TabSync'),
            href: "syncactivity.html",
            selected: page.classList.contains('syncConfigurationPage') || (isServicesPage && context == 'sync'),
            icon: 'sync'
        }, {
            divider: true,
            name: Globalize.translate('TabExtras')
        }, {
            name: Globalize.translate('TabAutoOrganize'),
            href: "autoorganizelog.html",
            selected: page.classList.contains("organizePage"),
            icon: 'folder',
            color: '#01C0DD'
        }, {
            name: Globalize.translate('TabDLNA'),
            href: "dlnasettings.html",
            selected: page.classList.contains("dlnaPage"),
            icon: 'tv',
            color: '#E5342E'
        }, {
            name: Globalize.translate('TabLiveTV'),
            href: "livetvstatus.html",
            selected: page.classList.contains("liveTvSettingsPage") || (isServicesPage && context == 'livetv'),
            icon: 'live-tv',
            color: '#293AAE'
        }, {
            name: Globalize.translate('TabNotifications'),
            href: "notificationsettings.html",
            selected: page.classList.contains("notificationConfigurationPage"),
            icon: 'notifications',
            color: 'brown'
        }, {
            name: Globalize.translate('TabPlugins'),
            href: "plugins.html",
            selected: page.classList.contains("pluginConfigurationPage"),
            icon: 'add-shopping-cart',
            color: '#9D22B1'
        }, {
            divider: true,
            name: Globalize.translate('TabExpert')
        }, {
            name: Globalize.translate('TabAdvanced'),
            href: "advanced.html",
            selected: page.classList.contains("advancedConfigurationPage"),
            icon: 'settings',
            color: '#F16834'
        }, {
            name: Globalize.translate('TabScheduledTasks'),
            href: "scheduledtasks.html",
            selected: page.classList.contains("scheduledTasksConfigurationPage"),
            icon: 'schedule',
            color: '#38c'
        }, {
            name: Globalize.translate('TabHelp'),
            divider: true,
            href: "support.html",
            selected: pageElem.id == "supportPage" || pageElem.id == "logPage" || pageElem.id == "supporterPage" || pageElem.id == "supporterKeyPage" || pageElem.id == "aboutPage",
            icon: 'help',
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
                Search.showSearchPanel();
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
            case 'SetRepeatMode':
                break;
            default:
                Logger.log('Unrecognized command: ' + cmd.Name);
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

        if (!newItems.length || AppInfo.isNativeApp || !window.Notification) {
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
                    timeout: 5000,
                    vibrate: true
                };

                var imageTags = item.ImageTags || {};

                if (imageTags.Primary) {

                    notification.icon = ApiClient.getScaledImageUrl(item.Id, {
                        width: 60,
                        tag: imageTags.Primary,
                        type: "Primary"
                    });
                }

                if (Notification.permission === "granted") {

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
            }
        });
    },

    ensurePageTitle: function (page) {

        if (!page.classList.contains('type-interior')) {
            return;
        }

        var pageElem = page;

        if (pageElem.querySelector('.pageTitle')) {
            return;
        }

        var parent = pageElem.querySelector('.content-primary');

        if (!parent) {
            parent = pageElem.getElementsByClassName('ui-content')[0];
        }

        var helpUrl = pageElem.getAttribute('data-helpurl');

        var html = '<div>';
        html += '<h1 class="pageTitle" style="display:inline-block;">' + (document.title || '&nbsp;') + '</h1>';

        if (helpUrl) {
            html += '<a href="' + helpUrl + '" target="_blank" class="clearLink" style="margin-top:-10px;display:inline-block;vertical-align:middle;margin-left:1em;"><paper-button raised class="secondary mini"><iron-icon icon="info"></iron-icon><span>' + Globalize.translate('ButtonHelp') + '</span></paper-button></a>';
        }

        html += '</div>';

        $(parent).prepend(html);
    },

    setPageTitle: function (title) {

        var page = $.mobile.activePage;

        if (page) {
            var elem = $(page)[0].querySelector('.pageTitle');

            if (elem) {
                elem.innerHTML = title;
            }
        }

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
                AppInfo.cardMargin = 'largeCardMargin';

                AppInfo.forcedImageFormat = 'jpg';
            }
        }

        if (!AppInfo.hasLowImageBandwidth) {
            AppInfo.enableStudioTabs = true;
            AppInfo.enableTvEpisodesTab = true;
            AppInfo.enableMovieTrailersTab = true;
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
            AppInfo.enableFooterNotifications = true;
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

        AppInfo.enableUserImage = true;
        AppInfo.hasPhysicalVolumeButtons = isCordova || isMobile;
        AppInfo.enableBackButton = isIOS && (window.navigator.standalone || AppInfo.isNativeApp);

        AppInfo.supportsSyncPathSetting = isCordova && isAndroid;
        AppInfo.supportsUserDisplayLanguageSetting = Dashboard.isConnectMode() && !isCordova;

        AppInfo.directPlayAudioContainers = [];
        AppInfo.directPlayVideoContainers = [];

        if (isCordova && isIOS) {
            AppInfo.moreIcon = 'more-horiz';
        } else {
            AppInfo.moreIcon = 'more-vert';
        }
    }

    function initializeApiClient(apiClient) {

        apiClient.enableAppStorePolicy = AppInfo.enableAppStorePolicy;
        apiClient.getDefaultImageQuality = Dashboard.getDefaultImageQuality;
        apiClient.normalizeImageOptions = Dashboard.normalizeImageOptions;

        $(apiClient).off("websocketmessage", Dashboard.onWebSocketMessageReceived).off('requestfail', Dashboard.onRequestFail);

        $(apiClient).on("websocketmessage", Dashboard.onWebSocketMessageReceived).on('requestfail', Dashboard.onRequestFail);
    }

    //localStorage.clear();
    function createConnectionManager(capabilities) {

        var credentialKey = Dashboard.isConnectMode() ? null : 'servercredentials4';
        var credentialProvider = new MediaBrowser.CredentialProvider(credentialKey);

        window.ConnectionManager = new MediaBrowser.ConnectionManager(Logger, credentialProvider, AppInfo.appName, AppInfo.appVersion, AppInfo.deviceName, AppInfo.deviceId, capabilities);

        if (window.location.href.toLowerCase().indexOf('wizardstart.html') != -1) {
            window.ConnectionManager.clearData();
        }

        Events.on(ConnectionManager, 'apiclientcreated', function (e, newApiClient) {

            initializeApiClient(newApiClient);
        });

        return new Promise(function (resolve, reject) {

            if (Dashboard.isConnectMode()) {

                var server = ConnectionManager.getLastUsedServer();

                if (!Dashboard.isServerlessPage()) {

                    if (server && server.UserId && server.AccessToken) {
                        Dashboard.showLoadingMsg();

                        ConnectionManager.connectToServer(server).then(function (result) {
                            Dashboard.showLoadingMsg();

                            if (result.State == MediaBrowser.ConnectionState.SignedIn) {
                                window.ApiClient = result.ApiClient;
                            }
                            resolve();
                        });
                        return;
                    }
                }
                resolve();

            } else {

                var apiClient = new MediaBrowser.ApiClient(Logger, Dashboard.serverAddress(), AppInfo.appName, AppInfo.appVersion, AppInfo.deviceName, AppInfo.deviceId);
                apiClient.enableAutomaticNetworking = false;
                ConnectionManager.addApiClient(apiClient);
                Dashboard.importCss(apiClient.getUrl('Branding/Css'));
                window.ApiClient = apiClient;
                resolve();
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

    function setDocumentClasses() {

        var elem = document.documentElement;

        if (AppInfo.isTouchPreferred) {
            elem.classList.add('touch');
        }

        if (AppInfo.cardMargin) {
            elem.classList.add(AppInfo.cardMargin);
        }

        if (!AppInfo.enableStudioTabs) {
            elem.classList.add('studioTabDisabled');
        }

        if (!AppInfo.enableTvEpisodesTab) {
            elem.classList.add('tvEpisodesTabDisabled');
        }

        if (!AppInfo.enableMovieTrailersTab) {
            elem.classList.add('movieTrailersTabDisabled');
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
    }

    function initRequire() {

        var urlArgs = "v=" + (window.dashboardVersion || new Date().getDate());

        var bowerPath = "bower_components";

        // Put the version into the bower path since we can't easily put a query string param on html imports
        // Emby server will handle this
        if (!Dashboard.isRunningInCordova()) {
            bowerPath += window.dashboardVersion;
        }

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
            jQuery: bowerPath + '/jquery/dist/jquery.min',
            fastclick: bowerPath + '/fastclick/lib/fastclick'
        };

        if (Dashboard.isRunningInCordova()) {
            paths.dialog = "cordova/dialog";
            paths.prompt = "cordova/prompt";
            paths.sharingwidget = "cordova/sharingwidget";
            paths.serverdiscovery = "cordova/serverdiscovery";
            paths.wakeonlan = "cordova/wakeonlan";
        } else {
            paths.dialog = "components/dialog";
            paths.prompt = "components/prompt";
            paths.sharingwidget = "components/sharingwidget";
            paths.serverdiscovery = "apiclient/serverdiscovery";
            paths.wakeonlan = "apiclient/wakeonlan";
        }

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
            map: {
                '*': {
                    'css': 'components/requirecss',
                    'html': 'components/requirehtml'
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
        define("paper-dialog", ["html!" + bowerPath + "/paper-dialog/paper-dialog.html"]);
        define("paper-dialog-scrollable", ["html!" + bowerPath + "/paper-dialog-scrollable/paper-dialog-scrollable.html"]);
        define("paper-button", ["html!" + bowerPath + "/paper-button/paper-button.html"]);
        define("paper-icon-button", ["html!" + bowerPath + "/paper-icon-button/paper-icon-button.html"]);
        define("paper-drawer-panel", ["html!" + bowerPath + "/paper-drawer-panel/paper-drawer-panel.html"]);
        define("paper-radio-group", ["html!" + bowerPath + "/paper-radio-group/paper-radio-group.html"]);
        define("paper-radio-button", ["html!" + bowerPath + "/paper-radio-button/paper-radio-button.html"]);
        define("neon-animated-pages", ["html!" + bowerPath + "/neon-animation/neon-animated-pages.html"]);

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

        define("jstree", [bowerPath + "/jstree/dist/jstree.min", "css!thirdparty/jstree/themes/default/style.min.css"]);

        define("jqmicons", ['css!thirdparty/jquerymobile-1.4.5/jquery.mobile.custom.icons.css']);
        define("jqmtable", ["thirdparty/jquerymobile-1.4.5/jqm.table", 'css!thirdparty/jquerymobile-1.4.5/jqm.table.css']);

        define("jqmwidget", ["thirdparty/jquerymobile-1.4.5/jqm.widget"]);

        define("jqmslider", ["thirdparty/jquerymobile-1.4.5/jqm.slider", 'css!thirdparty/jquerymobile-1.4.5/jqm.slider.css']);

        define("jqmpopup", ["thirdparty/jquerymobile-1.4.5/jqm.popup", 'css!thirdparty/jquerymobile-1.4.5/jqm.popup.css']);

        define("jqmlistview", ["thirdparty/jquerymobile-1.4.5/jqm.listview", 'css!thirdparty/jquerymobile-1.4.5/jqm.listview.css']);

        define("jqmcontrolgroup", ["thirdparty/jquerymobile-1.4.5/jqm.controlgroup", 'css!thirdparty/jquerymobile-1.4.5/jqm.controlgroup.css']);

        define("jqmcollapsible", ["jqmicons", "thirdparty/jquerymobile-1.4.5/jqm.collapsible", 'css!thirdparty/jquerymobile-1.4.5/jqm.collapsible.css']);

        define("jqmcheckbox", ["jqmicons", "thirdparty/jquerymobile-1.4.5/jqm.checkbox", 'css!thirdparty/jquerymobile-1.4.5/jqm.checkbox.css']);

        define("jqmpanel", ["thirdparty/jquerymobile-1.4.5/jqm.panel", 'css!thirdparty/jquerymobile-1.4.5/jqm.panel.css']);

        define("hammer", [bowerPath + "/hammerjs/hammer.min"], function (Hammer) {
            return Hammer;
        });

        define("swipebox", [bowerPath + '/swipebox/src/js/jquery.swipebox.min', "css!" + bowerPath + "/swipebox/src/css/swipebox.min.css"]);

        define('fetch', [bowerPath + '/fetch/fetch']);
        define('webcomponentsjs', [bowerPath + '/webcomponentsjs/webcomponents-lite.min.js']);
        define('native-promise-only', [bowerPath + '/native-promise-only/lib/npo.src']);

        if (Dashboard.isRunningInCordova()) {
            define('registrationservices', ['cordova/registrationservices']);

        } else {
            define('registrationservices', ['scripts/registrationservices']);
        }
    }

    function init(hostingAppInfo) {

        if (Dashboard.isRunningInCordova() && browserInfo.android) {
            define("appstorage", ["cordova/android/appstorage"]);
        } else {
            define('appstorage', [], function () {
                return appStorage;
            });
        }

        if (Dashboard.isRunningInCordova()) {
            define("localassetmanager", ["cordova/localassetmanager"]);
        } else {
            define("localassetmanager", ["apiclient/localassetmanager"]);
        }

        if (Dashboard.isRunningInCordova() && browserInfo.android) {
            define("nativedirectorychooser", ["cordova/android/nativedirectorychooser"]);
        }

        if (Dashboard.isRunningInCordova() && browserInfo.android) {
            define("audiorenderer", ["cordova/android/vlcplayer"]);
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

        define("connectservice", ["apiclient/connectservice"]);

        define("livetvcss", [], function () {
            Dashboard.importCss('css/livetv.css');
            return {};
        });
        define("detailtablecss", [], function () {
            Dashboard.importCss('css/detailtable.css');
            return {};
        });
        define("tileitemcss", ['css!css/tileitem.css']);

        if (Dashboard.isRunningInCordova()) {
            define("actionsheet", ["cordova/actionsheet"]);
        } else {
            define("actionsheet", ["scripts/actionsheet"]);
        }

        define("sharingmanager", ["scripts/sharingmanager"]);

        if (Dashboard.isRunningInCordova() && browserInfo.safari) {
            define("searchmenu", ["cordova/searchmenu"]);
        } else {
            define("searchmenu", ["scripts/searchmenu"]);
        }

        define("contentuploader", ["apiclient/sync/contentuploader"]);
        define("serversync", ["apiclient/sync/serversync"]);
        define("multiserversync", ["apiclient/sync/multiserversync"]);
        define("offlineusersync", ["apiclient/sync/offlineusersync"]);
        define("mediasync", ["apiclient/sync/mediasync"]);

        if (Dashboard.isRunningInCordova()) {
            define("fileupload", ["cordova/fileupload"]);
        } else {
            define("fileupload", ["apiclient/fileupload"]);
        }

        define("buttonenabled", ["legacy/buttonenabled"]);

        var deps = [];

        if (!window.fetch) {
            deps.push('fetch');
        }

        deps.push('scripts/mediacontroller');
        deps.push('scripts/globalize');
        deps.push('apiclient/events');

        deps.push('jQuery');

        deps.push('paper-drawer-panel');

        require(deps, function () {

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
        // But not exceeding 310
        drawerWidth = Math.min(drawerWidth, 310);

        drawer.drawerWidth = drawerWidth + "px";

        if (browserInfo.safari) {
            drawer.disableEdgeSwipe = true;
        }

        var deps = [];

        if (AppInfo.isNativeApp && browserInfo.android) {
            require(['cordova/android/logging']);
        }

        deps.push('appstorage');
        deps.push('scripts/mediaplayer');
        deps.push('scripts/appsettings');
        deps.push('apiclient/apiclient');
        deps.push('apiclient/connectionmanager');
        deps.push('apiclient/credentials');

        require(deps, function () {

            // TODO: This needs to be deprecated, but it's used heavily
            $.fn.checked = function (value) {
                if (value === true || value === false) {
                    // Set the value of the checkbox
                    return $(this).each(function () {
                        this.checked = value;
                    });
                } else {
                    // Return check state
                    return this.length && this[0].checked;
                }
            };

            if (Dashboard.isRunningInCordova() && browserInfo.android) {
                AppInfo.directPlayAudioContainers = "aac,mp3,mpa,wav,wma,mp2,ogg,oga,webma,ape,opus".split(',');

                // TODO: This is going to exclude it from both playback and sync, so improve on this
                if (AppSettings.syncLosslessAudio()) {
                    AppInfo.directPlayAudioContainers.push('flac');
                }

                AppInfo.directPlayVideoContainers = "m4v,3gp,ts,mpegts,mov,xvid,vob,mkv,wmv,asf,ogm,ogv,m2v,avi,mpg,mpeg,mp4,webm".split(',');
            }
            else if (Dashboard.isRunningInCordova() && browserInfo.safari) {

                AppInfo.directPlayAudioContainers = "aac,mp3,mpa,wav,wma,mp2,ogg,oga,webma,ape,opus".split(',');

                // TODO: This is going to exclude it from both playback and sync, so improve on this
                if (AppSettings.syncLosslessAudio()) {
                    AppInfo.directPlayAudioContainers.push('flac');
                }
            }

            var capabilities = Dashboard.capabilities();

            capabilities.DeviceProfile = MediaPlayer.getDeviceProfile(Math.max(screen.height, screen.width));

            var promises = [];
            deps = [];
            deps.push('thirdparty/jquery.unveil-custom.js');
            deps.push('emby-icons');
            deps.push('paper-icon-button');
            deps.push('paper-button');
            deps.push('thirdparty/jquerymobile-1.4.5/jquery.mobile.custom.js');
            deps.push('scripts/librarybrowser');
            promises.push(getRequirePromise(deps));

            promises.push(Globalize.ensure());
            promises.push(createConnectionManager(capabilities));


            Promise.all(promises).then(function () {

                MediaController.init();

                document.title = Globalize.translateDocument(document.title, 'html');

                var mainDrawerPanelContent = document.querySelector('.mainDrawerPanelContent');

                if (mainDrawerPanelContent) {

                    var newHtml = mainDrawerPanelContent.innerHTML.substring(4);
                    newHtml = newHtml.substring(0, newHtml.length - 3);

                    var srch = 'data-require=';
                    var index = newHtml.indexOf(srch);
                    var depends;

                    if (index != -1) {

                        var requireAttribute = newHtml.substring(index + srch.length + 1);

                        requireAttribute = requireAttribute.substring(0, requireAttribute.indexOf('"'));
                        depends = requireAttribute.split(',');
                    }

                    depends = depends || [];

                    if (newHtml.indexOf('type-interior') != -1) {
                        depends.push('jqmpopup');
                        depends.push('jqmlistview');
                        depends.push('jqmcollapsible');
                        depends.push('jqmcontrolgroup');
                        depends.push('jqmcheckbox');
                        depends.push('scripts/notifications');
                    }

                    require(depends, function () {

                        // Don't like having to use jQuery here, but it takes care of making sure that embedded script executes
                        $(mainDrawerPanelContent).html(Globalize.translateDocument(newHtml, 'html'));
                        onAppReady();
                    });
                    return;
                }

                onAppReady();
            });
        });
    }

    function onAppReady() {

        var deps = [];

        if (!(AppInfo.isNativeApp && browserInfo.android)) {
            document.documentElement.classList.add('minimumSizeTabs');
        }

        // Do these now to prevent a flash of content
        if (AppInfo.isNativeApp && browserInfo.android) {
            deps.push('css!devices/android/android.css');
        } else if (AppInfo.isNativeApp && browserInfo.safari) {
            deps.push('css!devices/ios/ios.css');
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
            }
        }

        if (browserInfo.msie) {
            deps.push('devices/ie/ie');
        }

        deps.push('scripts/search');
        deps.push('scripts/librarylist');
        deps.push('scripts/alphapicker');
        deps.push('scripts/playlistmanager');
        deps.push('scripts/sync');
        deps.push('scripts/backdrops');
        deps.push('scripts/librarymenu');
        deps.push('apiclient/deferred');

        deps.push('css!css/card.css');

        require(deps, function () {

            $.mobile.filterHtml = Dashboard.filterHtml;

            $.mobile.initializePage();

            var postInitDependencies = [];

            if (navigator.webkitPersistentStorage) {
                postInitDependencies.push('components/imagestore');
            }
            else if (Dashboard.isRunningInCordova()) {
                postInitDependencies.push('cordova/imagestore');
            }

            postInitDependencies.push('scripts/thememediaplayer');
            postInitDependencies.push('scripts/remotecontrol');
            postInitDependencies.push('css!css/notifications.css');
            postInitDependencies.push('css!css/chromecast.css');

            if (Dashboard.isRunningInCordova()) {

                postInitDependencies.push('cordova/connectsdk');

                if (browserInfo.android) {
                    postInitDependencies.push('cordova/android/mediasession');
                } else {
                    postInitDependencies.push('cordova/volume');
                }

                if (browserInfo.safari) {

                    postInitDependencies.push('cordova/ios/orientation');

                    if (Dashboard.capabilities().SupportsSync) {

                        postInitDependencies.push('cordova/ios/backgroundfetch');
                    }
                }

                //postInitDependencies.push('components/testermessage');

            } else if (browserInfo.chrome) {
                postInitDependencies.push('scripts/chromecast');
            }

            if (AppInfo.enableNowPlayingBar) {
                postInitDependencies.push('scripts/nowplayingbar');
            }

            if (AppInfo.isNativeApp && browserInfo.safari) {

                postInitDependencies.push('cordova/ios/tabbar');
            }

            require(postInitDependencies);
        });
    }

    function getCordovaHostingAppInfo() {

        return new Promise(function (resolve, reject) {

            document.addEventListener("deviceready", function () {

                cordova.getAppVersion.getVersionNumber(function (appVersion) {

                    var name = browserInfo.android ? "Emby for Android Mobile" : (browserInfo.safari ? "Emby for iOS" : "Emby Mobile");

                    // Remove special characters
                    var cleanDeviceName = device.model.replace(/[^\w\s]/gi, '');

                    var deviceId = window.MainActivity ? MainActivity.getLegacyDeviceId() : null;
                    deviceId = deviceId || device.uuid;

                    resolve({
                        deviceId: deviceId,
                        deviceName: cleanDeviceName,
                        appName: name,
                        appVersion: appVersion
                    });

                });

            }, false);
        });
    }

    function getWebHostingAppInfo() {

        return new Promise(function (resolve, reject) {

            var deviceName;

            if (browserInfo.chrome) {
                deviceName = "Chrome";
            } else if (browserInfo.edge) {
                deviceName = "Edge";
            } else if (browserInfo.mozilla) {
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

            var deviceId = appStorage.getItem('_deviceId');

            if (deviceId) {
                onDeviceAdAcquired(deviceId);
            } else {
                require(['cryptojs-sha1'], function () {
                    var keys = [];
                    keys.push(navigator.userAgent);
                    keys.push((navigator.cpuClass || ""));

                    var randomId = CryptoJS.SHA1(keys.join('|')).toString();
                    appStorage.setItem('_deviceId', randomId);
                    onDeviceAdAcquired(randomId);
                });
            }
        });
    }

    function getHostingAppInfo() {

        if (Dashboard.isRunningInCordova()) {
            return getCordovaHostingAppInfo();
        }

        return getWebHostingAppInfo();
    }

    function setBrowserInfo(isMobile) {

        var uaMatch = function (ua) {
            ua = ua.toLowerCase();

            var match = /(edge)[ \/]([\w.]+)/.exec(ua) ||
                /(chrome)[ \/]([\w.]+)/.exec(ua) ||
                /(safari)[ \/]([\w.]+)/.exec(ua) ||
                /(opera)(?:.*version|)[ \/]([\w.]+)/.exec(ua) ||
                /(msie) ([\w.]+)/.exec(ua) ||
                ua.indexOf("compatible") < 0 && /(mozilla)(?:.*? rv:([\w.]+)|)/.exec(ua) ||
                [];

            var platform_match = /(ipad)/.exec(ua) ||
                /(iphone)/.exec(ua) ||
                /(android)/.exec(ua) ||
                [];

            var browser = match[1] || "";

            if (ua.indexOf("windows phone") != -1 || ua.indexOf("iemobile") != -1) {

                // http://www.neowin.net/news/ie11-fakes-user-agent-to-fool-gmail-in-windows-phone-81-gdr1-update
                browser = "msie";
            }
            else if (ua.indexOf("like gecko") != -1 && ua.indexOf('webkit') == -1 && ua.indexOf('opera') == -1 && ua.indexOf('chrome') == -1 && ua.indexOf('safari') == -1) {
                browser = "msie";
            }

            return {
                browser: browser,
                version: match[2] || "0",
                platform: platform_match[0] || ""
            };
        };

        var userAgent = window.navigator.userAgent;
        var matched = uaMatch(userAgent);
        var browser = {};

        if (matched.browser) {
            browser[matched.browser] = true;
            browser.version = matched.version;
        }

        if (matched.platform) {
            browser[matched.platform] = true;
        }

        if (!browser.chrome && !browser.msie && !browser.edge && !browser.opera && userAgent.toLowerCase().indexOf("webkit") != -1) {
            browser.safari = true;
        }

        if (isMobile.any) {
            browser.mobile = true;
        }

        browser.animate = document.documentElement.animate != null;

        window.browserInfo = browser;
    }

    initRequire();

    var initialDependencies = [];

    initialDependencies.push('isMobile');
    initialDependencies.push('apiclient/logger');
    initialDependencies.push('apiclient/store');
    initialDependencies.push('scripts/extensions');

    var supportsNativeWebComponents = 'registerElement' in document && 'content' in document.createElement('template');

    if (!supportsNativeWebComponents) {
        initialDependencies.push('webcomponentsjs');
    }

    if (!window.Promise) {
        initialDependencies.push('native-promise-only');
    }

    require(initialDependencies, function (isMobile) {

        function onWebComponentsReady() {

            var polymerDependencies = [];

            require(polymerDependencies, function () {

                getHostingAppInfo().then(function (hostingAppInfo) {
                    init(hostingAppInfo);
                });
            });
        }

        setBrowserInfo(isMobile);
        setAppInfo();
        setDocumentClasses();

        if (supportsNativeWebComponents) {
            onWebComponentsReady();
        } else {
            document.addEventListener('WebComponentsReady', onWebComponentsReady);
        }
    });

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

pageClassOn('pagecreate', "page", function () {

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
    }

});

pageClassOn('pageshow', "page", function () {

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
        document.body.classList.add('darkScrollbars');
    } else {
        document.body.classList.remove('darkScrollbars');
    }

    Dashboard.ensurePageTitle(page);

    var apiClient = window.ApiClient;

    if (apiClient && apiClient.accessToken() && Dashboard.getCurrentUserId()) {

        var isSettingsPage = page.classList.contains('type-interior');

        if (isSettingsPage) {

            Dashboard.ensureToolsMenu(page);

            Dashboard.getCurrentUser().then(function (user) {

                if (!user.Policy.IsAdministrator) {
                    Dashboard.logout();
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

        if (!isConnectMode && this.id !== "loginPage" && !page.classList.contains('forgotPasswordPage') && !page.classList.contains('forgotPasswordPinPage') && !page.classList.contains('wizardPage') && this.id !== 'publicSharedItemPage') {

            Logger.log('Not logged into server. Redirecting to login.');
            Dashboard.logout();
            return;
        }
    }

    Dashboard.ensureHeader(page);

    if (apiClient && !apiClient.isWebSocketOpen()) {
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
            Logger.log('Sending close web socket command');
            apiClient.closeWebSocket();
        }
    }
});
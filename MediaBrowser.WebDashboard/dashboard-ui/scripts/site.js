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
        $.mobile.popup.prototype.options.transition = "none";

        //$.mobile.keepNative = "textarea";

        if ($.browser.mobile) {
            $.mobile.defaultPageTransition = "none";
        } else {
            $.mobile.defaultPageTransition = "none";
        }
        //$.mobile.collapsible.prototype.options.contentTheme = "a";

        // Make panels a little larger than the defaults
        $.mobile.panel.prototype.options.classes.modalOpen = "largePanelModalOpen ui-panel-dismiss-open";
        $.mobile.panel.prototype.options.classes.panel = "largePanel ui-panel";

        $.event.special.swipe.verticalDistanceThreshold = 40;
        $.mobile.loader.prototype.options.disabled = true;


        $.mobile.page.prototype.options.domCache = true;

        $.mobile.loadingMessage = false;
        $.mobile.loader.prototype.options.html = "";
        $.mobile.loader.prototype.options.textVisible = false;
        $.mobile.loader.prototype.options.textOnly = true;
        $.mobile.loader.prototype.options.text = "";

        $.mobile.hideUrlBar = false;
        $.mobile.autoInitializePage = false;
        $.mobile.changePage.defaults.showLoadMsg = false;

        // These are not needed. Nulling them out can help reduce dom querying when pages are loaded
        $.mobile.nojs = null;
        $.mobile.degradeInputsWithin = null;
        $.mobile.keepNative = ":jqmData(role='none'),.paper-input";
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
            Dashboard.hideLoadingMsg();
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

        if (!Dashboard.importedCss) {
            Dashboard.importedCss = [];
        }

        if (Dashboard.importedCss.indexOf(url) != -1) {
            return;
        }

        Dashboard.importedCss.push(url);

        if (document.createStyleSheet) {
            document.createStyleSheet(url);
        }
        else {
            var link = document.createElement('link');
            link.setAttribute('rel', 'stylesheet');
            link.setAttribute('type', 'text/css');
            link.setAttribute('href', url);
            document.head.appendChild(link);
        }
    },

    showError: function (message) {

        Dashboard.alert(message);
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
            html += '<button type="button" data-icon="refresh" onclick="this.disabled=\'disabled\';Dashboard.restartServer();" data-theme="b" data-inline="true" data-mini="true">' + Globalize.translate('ButtonRestart') + '</button>';
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

        html += '<button type="button" data-icon="refresh" onclick="this.disabled=\'disabled\';Dashboard.reloadPage();" data-theme="b" data-inline="true" data-mini="true">' + Globalize.translate('ButtonRefresh') + '</button>';

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

        elem.html(options.html).trigger('create');

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

        console.log('showLoadingMsg');

        require(['paperbuttonstyle'], function () {
            var elem = document.querySelector('.docspinner');

            if (elem) {

                // This is just an attempt to prevent the fade-in animation from running repeating and causing flickering
                elem.active = true;

            } else {

                elem = document.createElement("paper-spinner");
                elem.classList.add('docspinner');

                document.body.appendChild(elem);
                elem.active = true;
            }
        });
    },

    hideLoadingMsg: function () {

        console.log('hideLoadingMsg');

        require(['paperbuttonstyle'], function () {

            var elem = document.querySelector('.docspinner');

            if (elem) {

                elem.active = false;

                setTimeout(function () {
                    elem.active = false;
                }, 100);
            }
        });
    },

    getModalLoadingMsg: function () {

        var elem = $('.modalLoading');

        if (!elem.length) {

            elem = $('<div class="modalLoading" style="display:none;"></div>').appendTo(document.body);

        }

        return elem;
    },

    showModalLoadingMsg: function () {
        Dashboard.getModalLoadingMsg().show();
        Dashboard.showLoadingMsg();
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

            require(['paperbuttonstyle'], function () {
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

                    setTimeout(function () {
                        elem.parentNode.removeChild(elem);
                    }, 5000);
                }, 300);
            });

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
        if (navigator.notification && navigator.notification.confirm && message.indexOf('<') == -1) {

            var buttonLabels = [Globalize.translate('ButtonOk'), Globalize.translate('ButtonCancel')];

            navigator.notification.confirm(message, function (index) {

                callback(index == 1);

            }, title || Globalize.translate('HeaderConfirm'), buttonLabels.join(','));

        } else {
            Dashboard.confirmInternal(message, title, true, callback);
        }
    },

    confirmInternal: function (message, title, showCancel, callback) {

        require(['paperbuttonstyle'], function () {

            var id = 'paperdlg' + new Date().getTime();

            var html = '<paper-dialog id="' + id + '" role="alertdialog" entry-animation="fade-in-animation" exit-animation="fade-out-animation" with-backdrop>';
            html += '<h2>' + title + '</h2>';
            html += '<div>' + message + '</div>';
            html += '<div class="buttons">';

            if (showCancel) {
                html += '<paper-button dialog-dismiss>' + Globalize.translate('ButtonCancel') + '</paper-button>';
            }

            html += '<paper-button class="btnConfirm" dialog-confirm autofocus>' + Globalize.translate('ButtonOk') + '</paper-button>';

            html += '</div>';
            html += '</paper-dialog>';

            $(document.body).append(html);

            // This timeout is obviously messy but it's unclear how to determine when the webcomponent is ready for use
            // element onload never fires
            setTimeout(function () {

                var dlg = document.getElementById(id);

                // Has to be assigned a z-index after the call to .open() 
                $(dlg).on('iron-overlay-closed', function (e) {
                    var confirmed = this.closingReason.confirmed;
                    this.parentNode.removeChild(this);

                    if (callback) {
                        callback(confirmed);
                    }
                });

                dlg.open();

            }, 300);
        });
    },

    refreshSystemInfoFromServer: function () {

        var apiClient = ApiClient;

        if (apiClient && apiClient.accessToken()) {
            if (AppInfo.enableFooterNotifications) {
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

    showUserFlyout: function () {

        var html = '<div data-role="panel" data-position="right" data-display="overlay" id="userFlyout" data-position-fixed="true" data-theme="a">';

        html += '<h3 class="userHeader">';

        html += '</h3>';

        html += '<form>';

        html += '<p class="preferencesContainer"></p>';

        html += '<p><button data-mini="true" type="button" onclick="Dashboard.logout();" data-icon="lock">' + Globalize.translate('ButtonSignOut') + '</button></p>';

        html += '</form>';
        html += '</div>';

        $(document.body).append(html);

        var elem = $('#userFlyout').panel({}).lazyChildren().trigger('create').panel("open").on("panelclose", function () {

            $(this).off("panelclose").remove();
        });

        ConnectionManager.user(window.ApiClient).done(function (user) {
            Dashboard.updateUserFlyout(elem, user);
        });

        require(['jqmicons']);
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

        $('.userHeader', elem).html(html).lazyChildren();

        html = '';

        if (user.localUser && user.localUser.Policy.EnableUserPreferenceAccess) {
            html += '<p><a data-mini="true" data-role="button" href="mypreferencesdisplay.html?userId=' + user.localUser.Id + '" data-icon="gear">' + Globalize.translate('ButtonSettings') + '</button></a>';
        }

        $('.preferencesContainer', elem).html(html).trigger('create');
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
            selected: page.classList.contains("mediaLibraryPage"),
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
            icon: 'refresh'
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

        if (!newItems.length || AppInfo.isNativeApp) {
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
            html += '<a href="' + helpUrl + '" target="_blank" class="clearLink" style="margin-top:-10px;display:inline-block;vertical-align:middle;margin-left:1em;"><paper-button raised class="secondary mini"><i class="fa fa-info-circle"></i>' + Globalize.translate('ButtonHelp') + '</paper-button></a>';
        }

        html += '</div>';

        $(parent).prepend(html);

        if (helpUrl) {
            require(['paperbuttonstyle']);
        }
    },

    setPageTitle: function (title) {

        var elem = $($.mobile.activePage)[0].querySelector('.pageTitle');

        if (elem) {
            elem.innerHTML = title;
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

            // Need to use this rather than AppInfo.isNativeApp because the property isn't set yet at the time we call this
            SupportsPersistentIdentifier: Dashboard.isRunningInCordova(),

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

                quality -= 15;

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
            options.backgroundColor = '#1c1c1c';
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

        Dashboard.importCss('bower_components/swipebox/src/css/swipebox.min.css');

        require([
            'bower_components/swipebox/src/js/jquery.swipebox.min'
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

    firePageEvent: function (page, name, dependencies) {

        Dashboard.ready(function () {

            if (dependencies && dependencies.length) {
                require(dependencies, function () {
                    Events.trigger(page, name);
                });
            } else {
                Events.trigger(page, name);
            }
        });
    },

    loadExternalPlayer: function () {

        var deferred = DeferredBuilder.Deferred();

        require(['scripts/externalplayer.js'], function () {

            if (Dashboard.isRunningInCordova()) {
                require(['cordova/externalplayer.js'], function () {

                    deferred.resolve();
                });
            } else {
                deferred.resolve();
            }
        });

        return deferred.promise();
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
                //AppInfo.enableSectionTransitions = true;

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

        if (!AppInfo.hasLowImageBandwidth) {
            AppInfo.enableStudioTabs = true;
            AppInfo.enablePeopleTabs = true;
            AppInfo.enableTvEpisodesTab = true;
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

        if (!$.browser.tv && !isIOS) {

            // Don't enable headroom on mobile chrome when the address bar is visible
            // With two bars hiding and showing it gets a little awkward
            if (AppInfo.isNativeApp || window.navigator.standalone || !$.browser.mobile) {
                AppInfo.enableHeadRoom = true;
            }
        }

        AppInfo.enableUserImage = true;
        AppInfo.hasPhysicalVolumeButtons = isCordova || isMobile;

        AppInfo.enableBackButton = isIOS && (window.navigator.standalone || AppInfo.isNativeApp);

        AppInfo.supportsFullScreen = isCordova && isAndroid;
        AppInfo.supportsSyncPathSetting = isCordova && isAndroid;

        if (isCordova && isAndroid) {
            AppInfo.directPlayAudioContainers = "flac,aac,mp3,mpa,wav,wma,mp2,ogg,oga,webma,ape".split(',');
            AppInfo.directPlayVideoContainers = "m4v,3gp,ts,mpegts,mov,xvid,vob,mkv,wmv,asf,ogm,ogv,m2v,avi,mpg,mpeg,mp4,webm".split(',');
        } else {
            AppInfo.directPlayAudioContainers = [];
            AppInfo.directPlayVideoContainers = [];
        }

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

        if (getWindowUrl().toLowerCase().indexOf('wizardstart.html') != -1) {
            window.ConnectionManager.clearData();
        }

        $(ConnectionManager).on('apiclientcreated', function (e, newApiClient) {

            initializeApiClient(newApiClient);
        });

        var deferred = DeferredBuilder.Deferred();

        if (Dashboard.isConnectMode()) {

            var server = ConnectionManager.getLastUsedServer();

            if (!Dashboard.isServerlessPage()) {

                if (server && server.UserId && server.AccessToken) {
                    ConnectionManager.connectToServer(server).done(function (result) {
                        if (result.State == MediaBrowser.ConnectionState.SignedIn) {
                            window.ApiClient = result.ApiClient;
                        }
                        deferred.resolve();
                    });
                    return deferred.promise();
                }
            }
            deferred.resolve();

        } else {

            var apiClient = new MediaBrowser.ApiClient(Logger, Dashboard.serverAddress(), AppInfo.appName, AppInfo.appVersion, AppInfo.deviceName, AppInfo.deviceId);
            apiClient.enableAutomaticNetworking = false;
            ConnectionManager.addApiClient(apiClient);
            Dashboard.importCss(apiClient.getUrl('Branding/Css'));
            window.ApiClient = apiClient;
        }
        return deferred.promise();
    }

    function initFastClick() {

        require(["bower_components/fastclick/lib/fastclick"], function (FastClick) {

            FastClick.attach(document.body, {
                tapDelay: 0
            });

            // Have to work around this issue of fast click breaking the panel dismiss
            $(document.body).on('touchstart', '.ui-panel-dismiss', function () {
                $(this).trigger('click');
            });
        });

    }

    function setDocumentClasses() {

        var elem = document.documentElement;

        if (AppInfo.enableBottomTabs) {
            elem.classList.add('bottomSecondaryNav');
        }

        if (AppInfo.isTouchPreferred) {
            elem.classList.add('touch');
        } else {
            elem.classList.add('pointerInput');
        }

        if (AppInfo.cardMargin) {
            elem.classList.add(AppInfo.cardMargin);
        }

        if (!AppInfo.enableStudioTabs) {
            elem.classList.add('studioTabDisabled');
        }

        if (!AppInfo.enablePeopleTabs) {
            elem.classList.add('peopleTabDisabled');
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
    }

    function onDocumentReady() {

        // Do these now to prevent a flash of content
        if (AppInfo.isNativeApp) {
            if ($.browser.android) {
                Dashboard.importCss('themes/android.css');
            }
            else if ($.browser.safari) {
                Dashboard.importCss('themes/ios.css');
            }
        }

        if ($.browser.msie && $.browser.tv && ($.browser.version || 11) <= 10) {
            Dashboard.importCss('thirdparty/paper-ie10.css');
        }

        if ($.browser.safari && $.browser.mobile) {
            initFastClick();
        }

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
                    Logger.log('Sending close web socket command');
                    apiClient.closeWebSocket();
                }
            }
        });

        $(document).on('contextmenu', '.ui-popup-screen', function (e) {

            $('.ui-popup').popup('close');

            e.preventDefault();
            return false;
        });

        require(['filesystem']);

        if (Dashboard.isRunningInCordova()) {
            require(['cordova/connectsdk', 'scripts/registrationservices', 'cordova/back']);

            if ($.browser.android) {
                require(['cordova/android/androidcredentials', 'cordova/android/immersive', 'cordova/android/mediasession']);
            } else {
                require(['cordova/volume']);
            }

            if ($.browser.safari) {
                require(['cordova/ios/orientation']);
            }

        } else {
            if ($.browser.chrome) {
                require(['scripts/chromecast']);
            }
        }

        if (navigator.splashscreen) {
            navigator.splashscreen.hide();
        }
    }

    function init(deferred, capabilities, appName, deviceId, deviceName) {

        requirejs.config({
            urlArgs: "v=" + window.dashboardVersion,

            paths: {
                "velocity": "bower_components/velocity/velocity.min"
            }
        });

        // Required since jQuery is loaded before requireJs
        define('jquery', [], function () {
            return jQuery;
        });

        if (Dashboard.isRunningInCordova() && $.browser.android) {
            define("appstorage", ["cordova/android/appstorage"]);
        } else {
            define('appstorage', [], function () {
                return appStorage;
            });
        }
        if (Dashboard.isRunningInCordova()) {
            define("serverdiscovery", ["cordova/serverdiscovery"]);
            define("wakeonlan", ["cordova/wakeonlan"]);
        } else {
            define("serverdiscovery", ["apiclient/serverdiscovery"]);
            define("wakeonlan", ["apiclient/wakeonlan"]);
        }

        if (Dashboard.isRunningInCordova() && $.browser.android) {
            define("localassetmanager", ["cordova/android/localassetmanager"]);
        } else {
            define("localassetmanager", ["apiclient/localassetmanager"]);
        }

        if (Dashboard.isRunningInCordova() && $.browser.android) {
            define("filesystem", ["cordova/android/filesystem"]);
        }
        else if (Dashboard.isRunningInCordova()) {
            define("filesystem", ["cordova/filesystem"]);
        }
        else {
            define("filesystem", ["thirdparty/filesystem"]);
        }

        if (Dashboard.isRunningInCordova() && $.browser.android) {
            define("nativedirectorychooser", ["cordova/android/nativedirectorychooser"]);
        }

        if (Dashboard.isRunningInCordova() && $.browser.android) {
            define("audiorenderer", ["cordova/android/vlcplayer"]);
            define("videorenderer", ["cordova/android/vlcplayer"]);
        }
        else {
            define("audiorenderer", ["scripts/htmlmediarenderer"]);
            define("videorenderer", ["scripts/htmlmediarenderer"]);
        }

        define("connectservice", ["apiclient/connectservice"]);
        define("paperbuttonstyle", [], function () {
            return {};
        });
        define("jqmicons", [], function () {
            Dashboard.importCss('thirdparty/jquerymobile-1.4.5/jquery.mobile.custom.icons.css');
            return {};
        });
        define("livetvcss", [], function () {
            Dashboard.importCss('css/livetv.css');
            return {};
        });
        define("detailtablecss", [], function () {
            Dashboard.importCss('css/detailtable.css');
            return {};
        });
        define("tileitemcss", [], function () {
            Dashboard.importCss('css/tileitem.css');
            return {};
        });

        if (Dashboard.isRunningInCordova()) {
            define("actionsheet", ["cordova/actionsheet"]);
        } else {
            define("actionsheet", ["scripts/actionsheet"]);
        }

        define("sharingmanager", ["scripts/sharingmanager"]);

        if (Dashboard.isRunningInCordova()) {
            define("sharingwidget", ["cordova/sharingwidget"]);
        } else {
            define("sharingwidget", ["scripts/sharingwidget"]);
        }

        if (Dashboard.isRunningInCordova() && $.browser.safari) {
            define("searchmenu", ["cordova/searchmenu"]);
        } else {
            define("searchmenu", ["scripts/searchmenu"]);
        }

        $.extend(AppInfo, Dashboard.getAppInfo(appName, deviceId, deviceName));

        var drawer = document.querySelector('.mainDrawerPanel');
        drawer.classList.remove('mainDrawerPanelPreInit');
        drawer.forceNarrow = true;
        var drawerWidth = screen.availWidth - 50;
        // At least 240
        drawerWidth = Math.max(drawerWidth, 240);
        // But not exceeding 310
        drawerWidth = Math.min(drawerWidth, 310);

        drawer.drawerWidth = drawerWidth + "px";

        if ($.browser.safari && !AppInfo.isNativeApp) {
            drawer.disableEdgeSwipe = true;
        }

        if (Dashboard.isConnectMode()) {

            if (AppInfo.isNativeApp && $.browser.android) {
                require(['cordova/android/logging']);
            }

            require(['appstorage'], function () {

                capabilities.DeviceProfile = MediaPlayer.getDeviceProfile(Math.max(screen.height, screen.width));
                createConnectionManager(capabilities).done(function () {
                    $(function () {
                        onDocumentReady();
                        Dashboard.initPromiseDone = true;
                        $.mobile.initializePage();
                        deferred.resolve();
                    });
                });
            });

        } else {
            createConnectionManager(capabilities);

            $(function () {

                onDocumentReady();
                Dashboard.initPromiseDone = true;
                $.mobile.initializePage();
                deferred.resolve();
            });
        }
    }

    function initCordovaWithDeviceId(deferred, deviceId) {

        require(['cordova/imagestore']);

        var capablities = Dashboard.capabilities();

        init(deferred, capablities, "Emby Mobile", deviceId, device.model);
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

    setAppInfo();
    setDocumentClasses();

    $(document).on('WebComponentsReady', function () {
        if (Dashboard.isRunningInCordova()) {
            initCordova(initDeferred);
        } else {
            init(initDeferred, Dashboard.capabilities());
        }
    });

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

    if (current != 'a' && !$.browser.mobile) {
        document.body.classList.add('darkScrollbars');
    } else {
        document.body.classList.remove('darkScrollbars');
    }

}).on('pageinit', ".page", function () {

    var page = this;

    var dependencies = this.getAttribute('data-require');
    dependencies = dependencies ? dependencies.split(',') : null;
    Dashboard.firePageEvent(page, 'pageinitdepends', dependencies);

}).on('pagebeforeshow', ".page", function () {

    var page = this;
    var dependencies = this.getAttribute('data-require');

    Dashboard.ensurePageTitle(page);
    dependencies = dependencies ? dependencies.split(',') : null;
    Dashboard.firePageEvent(page, 'pagebeforeshowready', dependencies);

}).on('pageshow', ".page", function () {

    var page = this;
    var dependencies = this.getAttribute('data-require');
    dependencies = dependencies ? dependencies.split(',') : null;
    Dashboard.firePageEvent(page, 'pageshowbeginready', dependencies);

}).on('pageshowbeginready', ".page", function () {

    var page = this;

    var apiClient = window.ApiClient;

    if (apiClient && apiClient.accessToken() && Dashboard.getCurrentUserId()) {

        var isSettingsPage = page.classList.contains('type-interior');

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

        if (!isConnectMode && this.id !== "loginPage" && !page.classList.contains('forgotPasswordPage') && !page.classList.contains('wizardPage') && this.id !== 'publicSharedItemPage') {

            Logger.log('Not logged into server. Redirecting to login.');
            Dashboard.logout();
            return;
        }
    }

    Dashboard.firePageEvent(page, 'pageshowready');

    Dashboard.ensureHeader(page);

    if (apiClient && !apiClient.isWebSocketOpen()) {
        Dashboard.refreshSystemInfoFromServer();
    }

    if (!page.classList.contains('libraryPage')) {
        require(['jqmicons']);
    }
});
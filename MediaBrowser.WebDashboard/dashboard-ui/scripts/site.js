$.ajaxSetup({
    crossDomain: true,

    error: function (event) {
        Dashboard.hideLoadingMsg();

        if (!Dashboard.suppressAjaxErrors) {
            setTimeout(function () {


                var msg = event.getResponseHeader("X-Application-Error-Code") || Dashboard.defaultErrorMessage;

                Dashboard.showError(msg);
            }, 500);
        }
    }
});

if ($.browser.msie) {

    // This is unfortuantely required due to IE's over-aggressive caching. 
    // https://github.com/MediaBrowser/MediaBrowser/issues/179
    $.ajaxSetup({
        cache: false
    });
}

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
        //$.mobile.popup.prototype.options.transition = "none";
        $.mobile.defaultPageTransition = "none";
        //$.mobile.collapsible.prototype.options.contentTheme = "a";
    },

    getCurrentUser: function () {

        if (!Dashboard.getUserPromise) {

            var userId = Dashboard.getCurrentUserId();

            Dashboard.getUserPromise = ApiClient.getUser(userId).fail(Dashboard.logout);
        }

        return Dashboard.getUserPromise;
    },

    validateCurrentUser: function (page) {

        Dashboard.getUserPromise = null;

        if (Dashboard.getCurrentUserId()) {
            Dashboard.getCurrentUser();
        }

        page = page || $.mobile.activePage;

        var header = $('.header', page);

        if (header.length) {
            // Re-render the header
            header.remove();

            if (Dashboard.getUserPromise) {
                Dashboard.getUserPromise.done(function (user) {
                    Dashboard.ensureHeader(page, user);
                });
            } else {
                Dashboard.ensureHeader(page);
            }
        }
    },

    getCurrentUserId: function () {

        if (!window.localStorage) {
            return null;
        }

        var autoLoginUserId = getParameterByName('u');
        var storedUserId = localStorage.getItem("userId");
        var userId;

        if (autoLoginUserId && autoLoginUserId != storedUserId) {

            localStorage.setItem("userId", autoLoginUserId);
            ApiClient.currentUserId(autoLoginUserId);
        }

        return autoLoginUserId || storedUserId;
    },

    setCurrentUser: function (userId) {

        if (window.localStorage) {
            localStorage.setItem("userId", userId);
        }

        ApiClient.currentUserId(userId);
        Dashboard.getUserPromise = null;
    },

    logout: function () {

        if (window.localStorage) {
            localStorage.removeItem("userId");
        }

        Dashboard.getUserPromise = null;
        ApiClient.currentUserId(null);
        window.location = "login.html";
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

        Dashboard.confirmInternal(options.message, options.title || 'Alert', false, options.callback);
    },

    updateSystemInfo: function (info) {

        Dashboard.lastSystemInfo = info;
        Dashboard.ensureWebSocket(info);

        if (!Dashboard.initialServerVersion) {
            Dashboard.initialServerVersion = info.Version;
        }

        if (info.HasPendingRestart) {

            Dashboard.hideDashboardVersionWarning();

            Dashboard.getCurrentUser().done(function (currentUser) {

                if (currentUser.Configuration.IsAdministrator) {
                    Dashboard.showServerRestartWarning(info);
                }
            });

        } else {

            Dashboard.hideServerRestartWarning();

            if (Dashboard.initialServerVersion != info.Version) {

                Dashboard.showDashboardVersionWarning();
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
                ApiClient.sendWebSocketMessage("SystemInfoStart", "0,350");
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

        var html = '<span style="margin-right: 1em;">Please restart to finish updating.</span>';

        if (systemInfo.CanSelfRestart) {
            html += '<button type="button" data-icon="refresh" onclick="$(this).buttonEnabled(false);Dashboard.restartServer();" data-theme="b" data-inline="true" data-mini="true">Restart Server</button>';
        }

        Dashboard.showFooterNotification({ id: "serverRestartWarning", html: html, forceShow: true, allowHide: false });
    },

    hideServerRestartWarning: function () {

        $('#serverRestartWarning').remove();
    },

    showDashboardVersionWarning: function () {

        var html = '<span style="margin-right: 1em;">Please refresh this page to receive new updates from the server.</span>';

        Dashboard.showFooterNotification({ id: "dashboardVersionWarning", html: html, forceShow: true, allowHide: false });
    },

    reloadPage: function () {

        var currentUrl = window.location.toString().toLowerCase();

        // If they're on a plugin config page just go back to the dashboard
        // The plugin may not have been loaded yet, or could have been uninstalled
        if (currentUrl.indexOf('configurationpage') != -1) {
            window.location.href = "dashboard.html";
        } else {
            window.location.href = window.location.href;
        }
    },

    hideDashboardVersionWarning: function () {

        $('#dashboardVersionWarning').remove();
    },

    showFooterNotification: function (options) {

        var removeOnHide = !options.id;

        options.id = options.id || "notification" + new Date().getTime() + parseInt(Math.random());

        var footer = $("#footer").css("top", "initial").show();

        var parentElem = $('#footerNotifications', footer);

        var elem = $('#' + options.id, parentElem);

        if (!elem.length) {
            elem = $('<p id="' + options.id + '" class="footerNotification"></p>').appendTo(parentElem);
        }

        var onclick = removeOnHide ? "$(\"#" + options.id + "\").trigger(\"notification.remove\").remove();" : "$(\"#" + options.id + "\").trigger(\"notification.hide\").hide();";

        if (options.allowHide !== false) {
            options.html += "<span style='margin-left: 1em;'><button type='button' onclick='" + onclick + "' data-icon='delete' data-iconpos='notext' data-mini='true' data-inline='true' data-theme='b'>Hide</button></span>";
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

    navigate: function (url, preserveQueryString) {

        var queryString = getWindowLocationSearch();
        if (preserveQueryString && queryString) {
            url += queryString;
        }
        $.mobile.changePage(url);
    },

    showLoadingMsg: function () {
        $.mobile.loading("show");
    },

    hideLoadingMsg: function () {
        $.mobile.loading("hide");
    },

    processPluginConfigurationUpdateResult: function () {

        Dashboard.hideLoadingMsg();

        Dashboard.alert("Settings saved.");
    },

    defaultErrorMessage: "There was an error processing the request.",

    processServerConfigurationUpdateResult: function (result) {

        Dashboard.hideLoadingMsg();

        Dashboard.alert("Settings saved.");
    },

    confirmInternal: function (message, title, showCancel, callback) {

        $('.confirmFlyout').popup("close").remove();

        var html = '<div data-role="popup" data-transition="slidefade" class="confirmFlyout" style="max-width:500px;" data-theme="a">';

        html += '<div class="ui-bar-a" style="text-align:center;">';
        html += '<h3>' + title + '</h3>';
        html += '</div>';

        html += '<div style="padding: 1em;">';

        html += '<div style="padding: 1em .25em;margin: 0;">';
        html += message;
        html += '</div>';

        html += '<p><button type="button" data-icon="check" onclick="$(\'.confirmFlyout\')[0].confirm=true;$(\'.confirmFlyout\').popup(\'close\');" data-theme="b">Ok</button></p>';

        if (showCancel) {
            html += '<p><button type="button" data-icon="delete" onclick="$(\'.confirmFlyout\').popup(\'close\');" data-theme="a">Cancel</button></p>';
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

    confirm: function (message, title, callback) {
        Dashboard.confirmInternal(message, title, true, callback);
    },

    refreshSystemInfoFromServer: function () {
        ApiClient.getSystemInfo().done(function (info) {

            Dashboard.updateSystemInfo(info);
        });
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
        $.getJSON(ApiClient.getUrl("System/Info")).done(function (info) {

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

    showUserFlyout: function (context) {

        Dashboard.getCurrentUser().done(function (user) {

            var html = '<div data-role="panel" data-position="right" data-display="overlay" id="userFlyout" data-position-fixed="true" data-theme="a">';

            html += '<h3>';

            if (user.PrimaryImageTag) {
                var imageUrl = ApiClient.getUserImageUrl(user.Id, {

                    width: 60,
                    tag: user.PrimaryImageTag,
                    type: "Primary"

                });

                html += '<img style="max-width:28px;vertical-align:middle;margin-right:5px;" src="' + imageUrl + '" />';
            }

            html += user.Name;
            html += '</h3>';

            html += '<p><a data-mini="true" data-role="button" href="useredit.html?userId=' + user.Id + '" data-icon="user">Preferences</button></a>';
            html += '<p><button data-mini="true" type="button" onclick="Dashboard.logout();" data-icon="lock">Sign Out</button></p>';

            html += '</div>';

            $(document.body).append(html);

            $('#userFlyout').panel({}).trigger('create').panel("open").on("panelafterclose", function () {

                $(this).off("panelafterclose").remove();
            });
        });
    },

    getPluginSecurityInfo: function () {

        if (!Dashboard.getPluginSecurityInfoPromise) {

            var deferred = $.Deferred();

            // Don't let this blow up the dashboard when it fails
            $.ajax({
                type: "GET",
                url: ApiClient.getUrl("Plugins/SecurityInfo"),
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
        Dashboard.validateCurrentUser();
    },

    ensureHeader: function (page, user) {

        if (!page.hasClass('libraryPage') && !$('.headerButtons', page).length) {

            Dashboard.renderHeader(page, user);
        }
    },

    renderHeader: function (page, user) {

        var headerHtml = '';

        var header = $('.header', page);

        if (!header.length) {
            headerHtml += '<div class="header">';

            headerHtml += '<a class="logo" href="index.html">';

            if (page.hasClass('standalonePage')) {

                headerHtml += '<img class="imgLogoIcon" src="css/images/mblogoicon.png" /><img class="imgLogoText" src="css/images/mblogotextblack.png" />';
            }

            headerHtml += '</a>';

            if (page.hasClass('type-interior')) {
                headerHtml += '<div>';
                headerHtml += '<button type="button" data-icon="bars" data-inline="true" data-iconpos="notext" class="ui-alt-icon" onclick="Dashboard.showDashboardMenu();">Menu</button>';
                headerHtml += '</div>';
            }

            headerHtml += '</div>';
            page.prepend(headerHtml);

            header = $('.header', page).trigger('create');
        }

        var imageColor = "black";

        headerHtml = '';
        headerHtml += '<div class="headerButtons">';

        if (user && !page.hasClass('wizardPage')) {

            headerHtml += '<a class="imageLink btnCurrentUser" href="#" onclick="Dashboard.showUserFlyout(this);"><span class="currentUsername" style="font-weight:normal;">' + user.Name + '</span>';

            if (user.PrimaryImageTag) {

                var url = ApiClient.getUserImageUrl(user.Id, {
                    width: 225,
                    tag: user.PrimaryImageTag,
                    type: "Primary"
                });

                headerHtml += '<img src="' + url + '" />';
            } else {
                headerHtml += '<img src="css/images/currentuserdefault' + imageColor + '.png" />';
            }
            headerHtml += '</a>';

            if (user.Configuration.IsAdministrator) {

                var href = window.location.toString().toLowerCase().indexOf('dashboard.html') == -1 ? 'dashboard.html' : '#';

                headerHtml += '<a class="imageLink btnTools" href="' + href + '" data-role="button" data-icon="gear" data-inline="true" data-iconpos="notext">Tools</a>';
            }

        }

        headerHtml += '</div>';

        header.append(headerHtml).trigger('create');

        if (!$('.supporterIcon', header).length) {

            Dashboard.getPluginSecurityInfo().done(function (pluginSecurityInfo) {

                if (pluginSecurityInfo.IsMBSupporter) {
                    $('<a class="imageLink supporterIcon" href="supporter.html" title="Thank you for supporting Media Browser."><img src="css/images/supporter/supporterbadge.png" /></a>').insertBefore($('.btnTools', header));
                } else {
                    $('<a class="imageLink supporterIcon" href="supporter.html" title="Become a Media Browser supporter!"><img src="css/images/supporter/nonsupporterbadge.png" /></a>').insertBefore($('.btnTools', header));
                }
            });
        }

        $(Dashboard).trigger('interiorheaderrendered', [header, user]);
    },

    ensureToolsMenu: function (page) {

        if (!page.hasClass('type-interior')) {
            return;
        }

        var sidebar = $('.toolsSidebar', page);

        if (!sidebar.length) {

            var html = '<div class="content-secondary ui-bar-a toolsSidebar">';

            html += '<p class="libraryPanelHeader" style="margin: 30px 0 20px 25px;"><a href="index.html" class="imageLink"><img src="css/images/mblogoicon.png" style="height:28px;" /><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></a></p>';

            html += '<div style="position:absolute;top:20px;right:20px;"><a data-role="button" data-theme="b" data-icon="edit" data-iconpos="notext" href="edititemmetadata.html" title="Metadata Manager">Metadata Manager</a></div>';

            html += '<div class="sidebarLinks">';

            var links = Dashboard.getToolsMenuLinks(page);

            var i, length, link;

            for (i = 0, length = links.length; i < length; i++) {

                link = links[i];

                if (link.divider) {
                    html += "<div class='sidebarDivider'></div>";
                }

                if (link.href) {

                    if (link.selected) {
                        html += '<a class="selectedSidebarLink" href="' + link.href + '">' + link.name + '</a>';
                    } else {
                        html += '<a href="' + link.href + '">' + link.name + '</a>';
                    }

                }
            }

            // collapsible
            html += '</div>';

            // content-secondary
            html += '</div>';

            html += '<div data-role="panel" id="dashboardPanel" class="dashboardPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<p class="libraryPanelHeader" style="margin: 20px 0 20px 15px;"><a href="index.html" class="imageLink"><img src="css/images/mblogoicon.png" /><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></a></p>';

            for (i = 0, length = links.length; i < length; i++) {

                link = links[i];

                if (link.divider) {
                    html += "<div class='dashboardPanelDivider'></div>";
                }

                if (link.href) {

                    if (link.selected) {
                        html += '<a class="selectedDashboardPanelLink dashboardPanelLink" href="' + link.href + '">' + link.name + '</a>';
                    } else {
                        html += '<a class="dashboardPanelLink" href="' + link.href + '">' + link.name + '</a>';
                    }

                }
            }

            html += '</div>';

            $(page).append(html).trigger('create');
        }
    },

    showDashboardMenu: function () {

        var page = $.mobile.activePage;

        $("#dashboardPanel", page).panel("open");
    },

    getToolsMenuLinks: function (page) {

        var pageElem = page[0];

        return [{
            name: "Dashboard",
            href: "dashboard.html",
            selected: page.hasClass("dashboardHomePage")
        }, {
            name: "Library",
            divider: true,
            href: "library.html",
            selected: page.hasClass("mediaLibraryPage")
        }, {
            name: "Metadata",
            href: "metadata.html",
            selected: pageElem.id == "metadataConfigurationPage" || pageElem.id == "advancedMetadataConfigurationPage" || pageElem.id == "metadataImagesConfigurationPage"
        }, {
            name: "Auto-Organize",
            href: "autoorganizelog.html",
            selected: page.hasClass("organizePage")
        }, {
            name: "DLNA",
            href: "dlnasettings.html",
            selected: page.hasClass("dlnaPage")
        }, {
            name: "Live TV",
            href: "livetvstatus.html",
            selected: page.hasClass("liveTvSettingsPage")
        }, {
            name: "Plugins",
            href: "plugins.html",
            selected: page.hasClass("pluginConfigurationPage")
        }, {
            name: "Users",
            divider: true,
            href: "userprofiles.html",
            selected: page.hasClass("userProfilesConfigurationPage") || (pageElem.id == "mediaLibraryPage" && getParameterByName('userId'))
        }, {
            name: "App Settings",
            href: "appsplayback.html",
            selected: page.hasClass("appsPage")
        }, {
            name: "Advanced",
            divider: true,
            href: "advanced.html",
            selected: page.hasClass("advancedConfigurationPage")
        }, {
            name: "Scheduled Tasks",
            href: "scheduledtasks.html",
            selected: pageElem.id == "scheduledTasksPage" || pageElem.id == "scheduledTaskPage"
        }, {
            name: "Help",
            divider: true,
            href: "support.html",
            selected: pageElem.id == "supportPage" || pageElem.id == "logPage" || pageElem.id == "supporterPage" || pageElem.id == "supporterKeyPage" || pageElem.id == "aboutPage"
        }];

    },

    ensureWebSocket: function (systemInfo) {

        if (!("WebSocket" in window)) {
            // Not supported by the browser
            return;
        }

        if (ApiClient.isWebSocketOpenOrConnecting()) {
            return;
        }

        systemInfo = systemInfo || Dashboard.lastSystemInfo;

        var location = window.location;

        var webSocketUrl = "ws://" + location.hostname;

        if (systemInfo.HttpServerPortNumber == systemInfo.WebSocketPortNumber) {

            if (location.port) {
                webSocketUrl += ':' + location.port;
            }

        } else {
            webSocketUrl += ':' + systemInfo.WebSocketPortNumber;
        }

        ApiClient.openWebSocket(webSocketUrl);
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
        else if (msg.MessageType === "UserUpdated") {
            Dashboard.validateCurrentUser();

            var user = msg.Data;

            if (user.Id == Dashboard.getCurrentUserId()) {

                $('.currentUsername').html(user.Name);
            }
        }
        else if (msg.MessageType === "PackageInstallationCompleted") {
            Dashboard.getCurrentUser().done(function (currentUser) {

                if (currentUser.Configuration.IsAdministrator) {
                    Dashboard.showPackageInstallNotification(msg.Data, "completed");
                    Dashboard.refreshSystemInfoFromServer();
                }
            });
        }
        else if (msg.MessageType === "PackageInstallationFailed") {
            Dashboard.getCurrentUser().done(function (currentUser) {

                if (currentUser.Configuration.IsAdministrator) {
                    Dashboard.showPackageInstallNotification(msg.Data, "failed");
                    Dashboard.refreshSystemInfoFromServer();
                }
            });
        }
        else if (msg.MessageType === "PackageInstallationCancelled") {
            Dashboard.getCurrentUser().done(function (currentUser) {

                if (currentUser.Configuration.IsAdministrator) {
                    Dashboard.showPackageInstallNotification(msg.Data, "cancelled");
                    Dashboard.refreshSystemInfoFromServer();
                }
            });
        }
        else if (msg.MessageType === "PackageInstalling") {
            Dashboard.getCurrentUser().done(function (currentUser) {

                if (currentUser.Configuration.IsAdministrator) {
                    Dashboard.showPackageInstallNotification(msg.Data, "progress");
                    Dashboard.refreshSystemInfoFromServer();
                }
            });
        }
        else if (msg.MessageType === "ScheduledTaskEnded") {

            Dashboard.getCurrentUser().done(function (currentUser) {

                if (currentUser.Configuration.IsAdministrator) {
                    Dashboard.showTaskCompletionNotification(msg.Data);
                }
            });
        }
        else if (msg.MessageType === "Browse") {

            Dashboard.onBrowseCommand(msg.Data);
        }
        else if (msg.MessageType === "GeneralCommand") {

            var cmd = msg.Data;

            if (cmd.Name === 'GoHome') {
                Dashboard.navigate('index.html');
            }
            else if (cmd.Name === 'GoToSettings') {
                Dashboard.navigate('dashboard.html');
            }
        }
        else if (msg.MessageType === "MessageCommand") {

            var cmd = msg.Data;

            if (cmd.TimeoutMs && WebNotifications.supported()) {
                var notification = {
                    title: cmd.Header,
                    body: cmd.Text,
                    timeout: cmd.TimeoutMs
                };

                WebNotifications.show(notification);
            }
            else {
                Dashboard.showFooterNotification({ html: "<b>" + cmd.Header + ":&nbsp;&nbsp;&nbsp;</b>" + cmd.Text, timeout: cmd.TimeoutMs });
            }

        }

    },

    onBrowseCommand: function (cmd) {

        var context = cmd.Context || "";

        var url;

        var type = (cmd.ItemType || "").toLowerCase();

        if (type == "genre") {
            url = "itembynamedetails.html?genre=" + ApiClient.encodeName(cmd.ItemName) + "&context=" + context;
        }
        else if (type == "musicgenre") {
            url = "itembynamedetails.html?musicgenre=" + ApiClient.encodeName(cmd.ItemName) + "&context=" + (context || "music");
        }
        else if (type == "gamegenre") {
            url = "itembynamedetails.html?gamegenre=" + ApiClient.encodeName(cmd.ItemName) + "&context=" + (context || "games");
        }
        else if (type == "studio") {
            url = "itembynamedetails.html?studio=" + ApiClient.encodeName(cmd.ItemName) + "&context=" + context;
        }
        else if (type == "person") {
            url = "itembynamedetails.html?person=" + ApiClient.encodeName(cmd.ItemName) + "&context=" + context;
        }
        else if (type == "musicartist") {
            url = "itembynamedetails.html?musicartist=" + ApiClient.encodeName(cmd.ItemName) + "&context=" + (context || "music");
        }

        if (url) {
            Dashboard.navigate(url);
            return;
        }

        ApiClient.getItem(Dashboard.getCurrentUserId(), cmd.ItemId).done(function (item) {

            Dashboard.navigate(LibraryBrowser.getHref(item, context));

        });

    },

    showTaskCompletionNotification: function (result) {

        var html = '';

        if (result.Status == "Completed") {
            html += '<img src="css/images/notifications/done.png" class="notificationIcon" />';
            return;
        }
        else if (result.Status == "Cancelled") {
            html += '<img src="css/images/notifications/info.png" class="notificationIcon" />';
            return;
        }
        else {
            html += '<img src="css/images/notifications/error.png" class="notificationIcon" />';
        }

        html += '<span>';
        html += result.Name + " " + result.Status;
        html += '</span>';

        var timeout = 0;

        if (result.Status == 'Cancelled') {
            timeout = 2000;
        }

        Dashboard.showFooterNotification({ html: html, id: result.Id, forceShow: true, timeout: timeout });
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
            html += installation.Name + ' ' + installation.Version + ' installation completed';
        }
        else if (status == 'cancelled') {
            html += installation.Name + ' ' + installation.Version + ' installation was cancelled';
        }
        else if (status == 'failed') {
            html += installation.Name + ' ' + installation.Version + ' installation failed';
        }
        else if (status == 'progress') {
            html += 'Installing ' + installation.Name + ' ' + installation.Version;
        }

        html += '</span>';

        if (status == 'progress') {

            var percentComplete = Math.round(installation.PercentComplete || 0);

            html += '<progress style="margin-right: 1em;" max="100" value="' + percentComplete + '" title="' + percentComplete + '%">';
            html += '' + percentComplete + '%';
            html += '</progress>';

            if (percentComplete < 100) {
                var btnId = "btnCancel" + installation.Id;
                html += '<button id="' + btnId + '" type="button" data-icon="delete" onclick="$(\'' + btnId + '\').buttonEnabled(false);Dashboard.cancelInstallation(\'' + installation.Id + '\');" data-theme="b" data-inline="true" data-mini="true">Cancel</button>';
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

                    notification.icon = ApiClient.getImageUrl(item.Id, {
                        width: 100,
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

        $(parent).prepend("<h2 class='pageTitle'>" + (document.title || "&nbsp;") + "</h2>");
    },

    setPageTitle: function (title) {

        $('.pageTitle', $.mobile.activePage).html(title);

        if (title) {
            document.title = title;
        }
    },

    getDisplayTime: function (ticks) {

        var ticksPerHour = 36000000000;

        var parts = [];

        var hours = ticks / ticksPerHour;
        hours = Math.floor(hours);

        if (hours) {
            parts.push(hours);
        }

        ticks -= (hours * ticksPerHour);

        var ticksPerMinute = 600000000;

        var minutes = ticks / ticksPerMinute;
        minutes = Math.floor(minutes);

        ticks -= (minutes * ticksPerMinute);

        if (minutes < 10 && hours) {
            minutes = '0' + minutes;
        }
        parts.push(minutes);

        var ticksPerSecond = 10000000;

        var seconds = ticks / ticksPerSecond;
        seconds = Math.round(seconds);

        if (seconds < 10) {
            seconds = '0' + seconds;
        }
        parts.push(seconds);

        return parts.join(':');
    },

    ratePackage: function (link) {
        var id = link.getAttribute('data-id');
        var name = link.getAttribute('data-name');
        var rating = link.getAttribute('data-rating');

        var dialog = new RatingDialog($.mobile.activePage);
        dialog.show({
            header: "Rate and review " + name,
            id: id,
            rating: rating,
            callback: function (review) {
                console.log(review);
                dialog.close();

                ApiClient.createPackageReview(review).done(function () {
                    Dashboard.alert({
                        message: "Thank you for your review",
                        title: "Thank You"
                    });
                });
            }
        });
    },

    getStoreRatingHtml: function (rating, id, name, noLinks) {

        var html = "<div style='margin-left: 5px; margin-right: 5px; display: inline-block; vertical-align:middle;'>";
        if (!rating) rating = 0;

        for (var i = 1; i <= 5; i++) {
            var title = noLinks ? rating + " stars" : "Rate " + i + (i > 1 ? " stars" : " star");

            html += noLinks ? "" : "<span data-id=" + id + " data-name='" + name + "' data-rating=" + i + " onclick='Dashboard.ratePackage(this);return false;' >";
            if (rating <= i - 1) {
                html += "<div class='storeStarRating emptyStarRating' title='" + title + "'></div>";
            } else if (rating < i) {
                html += "<div class='storeStarRating halfStarRating' title='" + title + "'></div>";
            } else {
                html += "<div class='storeStarRating' title='" + title + "'></div>";
            }
            html += noLinks ? "" : "</span>";
        }

        html += "</div>";

        return html;
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
    }
};

if (!window.WebSocket) {

    alert("This browser does not support web sockets. For a better experience, try a newer browser such as Chrome, Firefox, IE10+, Safari (iOS) or Opera.");
}

else if (!IsStorageEnabled()) {
    alert("This browser does not support local storage or is running in private mode. For a better experience, try a newer browser such as Chrome, Firefox, IE10+, Safari (iOS) or Opera.");
}


var ApiClient = MediaBrowser.ApiClient.create("Dashboard", window.dashboardVersion);

$(ApiClient).on("websocketmessage", Dashboard.onWebSocketMessageReceived);


$(function () {

    var videoPlayerHtml = '<div id="mediaPlayer" data-theme="b" class="ui-bar-b" style="display: none;">';

    videoPlayerHtml += '<div id="videoBackdrop">';
    videoPlayerHtml += '<div id="videoPlayer">';
    videoPlayerHtml += '<div id="videoElement">';

    videoPlayerHtml += '<div id="play" class="status"></div>';
    videoPlayerHtml += '<div id="pause" class="status"></div>';

    videoPlayerHtml += '</div>';

    // Create controls
    videoPlayerHtml += '<div id="videoControls" class="videoControls">';

    videoPlayerHtml += '<div class="positionSliderContainer sliderContainer">';
    videoPlayerHtml += '<input type="range" class="mediaSlider positionSlider slider" step=".001" min="0" max="100" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
    videoPlayerHtml += '</div>';

    videoPlayerHtml += '<div id="video-basic-controls">';

    videoPlayerHtml += '<a id="video-playlistButton" class="mediaButton playlistButton" href="playlist.videoPlayerHtml" data-role="button" data-icon="bullets" data-iconpos="notext" data-inline="true" title="Playlist">Playlist</a>';
    videoPlayerHtml += '<button id="video-previousTrackButton" class="mediaButton previousTrackButton" title="Previous Track" type="button" onclick="MediaPlayer.previousTrack();" data-icon="previous-track" data-iconpos="notext" data-inline="true">Previous Track</button>';
    videoPlayerHtml += '<button id="video-playButton" class="mediaButton" title="Play" type="button" onclick="MediaPlayer.unpause();" data-icon="play" data-iconpos="notext" data-inline="true">Play</button>';
    videoPlayerHtml += '<button id="video-pauseButton" class="mediaButton" title="Pause" type="button" onclick="MediaPlayer.pause();" data-icon="pause" data-iconpos="notext" data-inline="true">Pause</button>';

    videoPlayerHtml += '<button id="video-stopButton" class="mediaButton" title="Stop" type="button" onclick="MediaPlayer.stop();" data-icon="stop" data-iconpos="notext" data-inline="true">Stop</button>';
    videoPlayerHtml += '<button id="video-nextTrackButton" class="mediaButton nextTrackButton" title="Next Track" type="button" onclick="MediaPlayer.nextTrack();" data-icon="next-track" data-iconpos="notext" data-inline="true">Next Track</button>';

    videoPlayerHtml += '<div class="currentTime"></div>';
    videoPlayerHtml += '<div class="nowPlayingMediaInfo"></div>';

    videoPlayerHtml += '<button id="video-muteButton" class="mediaButton muteButton" title="Mute" type="button" onclick="MediaPlayer.mute();" data-icon="audio" data-iconpos="notext" data-inline="true">Mute</button>';
    videoPlayerHtml += '<button id="video-unmuteButton" class="mediaButton unmuteButton" title="Unmute" type="button" onclick="MediaPlayer.unMute();" data-icon="volume-off" data-iconpos="notext" data-inline="true">Unmute</button>';

    videoPlayerHtml += '<div class="volumeSliderContainer sliderContainer">';
    videoPlayerHtml += '<input type="range" class="mediaSlider volumeSlider slider" step=".05" min="0" max="1" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
    videoPlayerHtml += '</div>';

    videoPlayerHtml += '</div>'; // video-basic-controls
    videoPlayerHtml += '<div id="video-advanced-controls">';

    videoPlayerHtml += '<button onclick="MediaPlayer.showQualityFlyout();" id="video-qualityButton" class="mediaButton qualityButton" title="Quality" type="button" data-icon="gear" data-iconpos="notext" data-inline="true">Quality</button>';
    videoPlayerHtml += '<div class="mediaFlyoutContainer"><div id="video-qualityFlyout" style="display:none;" class="mediaPlayerFlyout"></div></div>';

    videoPlayerHtml += '<button onclick="MediaPlayer.showAudioTracksFlyout();" id="video-audioTracksButton" class="imageButton mediaButton audioTracksButton" title="Audio tracks" type="button" data-icon="audiocd" data-iconpos="notext" data-inline="true">Audio Tracks</button>';
    videoPlayerHtml += '<div class="mediaFlyoutContainer"><div id="video-audioTracksFlyout" style="display:none;" class="mediaPlayerFlyout audioTracksFlyout"></div></div>';

    videoPlayerHtml += '<button onclick="MediaPlayer.showSubtitleMenu();" id="video-subtitleButton" class="imageButton mediaButton subtitleButton" title="Subtitles" type="button" data-icon="subtitles" data-iconpos="notext" data-inline="true">Subtitles</button>';
    videoPlayerHtml += '<div class="mediaFlyoutContainer"><div id="video-subtitleFlyout" style="display:none;" class="mediaPlayerFlyout subtitleFlyout"></div></div>';

    videoPlayerHtml += '<button onclick="MediaPlayer.showChaptersFlyout();" id="video-chaptersButton" class="mediaButton chaptersButton" title="Scenes" type="button" data-icon="video" data-iconpos="notext" data-inline="true">Scenes</button>';
    videoPlayerHtml += '<div class="mediaFlyoutContainer"><div id="video-chaptersFlyout" style="display:none;" class="mediaPlayerFlyout chaptersFlyout"></div></div>';

    videoPlayerHtml += '<button onclick="MediaPlayer.toggleFullscreen();" id="video-fullscreenButton" class="mediaButton fullscreenButton" title="Fullscreen" type="button" data-icon="action" data-iconpos="notext" data-inline="true">Fullscreen</button>';

    videoPlayerHtml += '</div>'; // video-advanced-controls

    videoPlayerHtml += '</div>'; // videoControls

    videoPlayerHtml += '</div>'; // videoPlayer
    videoPlayerHtml += '</div>'; // videoBackdrop
    videoPlayerHtml += '</div>'; // mediaPlayer

    $(document.body).append(videoPlayerHtml);

    var mediaPlayerElem = $('#mediaPlayer', document.body);
    mediaPlayerElem.trigger('create');

    var footerHtml = '<div id="footer" data-theme="b" class="ui-bar-b">';
    footerHtml += '<div id="nowPlayingBar" class="nowPlayingBar" style="display:none;">';
    footerHtml += '<div class="barBackground ui-bar-b"></div>';
    footerHtml += '<div style="display:inline-block;width:12px;"></div>';
    footerHtml += '<a id="playlistButton" class="mediaButton playlistButton" href="playlist.html" data-role="button" data-icon="bullets" data-iconpos="notext" data-inline="true" title="Playlist">Playlist</a>';
    footerHtml += '<button id="previousTrackButton" class="mediaButton previousTrackButton" title="Previous Track" type="button" onclick="MediaController.previousTrack();" data-icon="previous-track" data-iconpos="notext" data-inline="true">Previous Track</button>';
    footerHtml += '<button id="playButton" class="mediaButton" title="Play" type="button" onclick="MediaController.unpause();" data-icon="play" data-iconpos="notext" data-inline="true">Play</button>';
    footerHtml += '<button id="pauseButton" class="mediaButton" title="Pause" type="button" onclick="MediaController.pause();" data-icon="pause" data-iconpos="notext" data-inline="true">Pause</button>';

    footerHtml += '<div id="mediaElement"></div>';

    footerHtml += '<button id="stopButton" class="mediaButton" title="Stop" type="button" onclick="MediaController.stop();" data-icon="stop" data-iconpos="notext" data-inline="true">Stop</button>';
    footerHtml += '<button id="nextTrackButton" class="mediaButton nextTrackButton" title="Next Track" type="button" onclick="MediaController.nextTrack();" data-icon="next-track" data-iconpos="notext" data-inline="true">Next Track</button>';

    footerHtml += '<div class="positionSliderContainer sliderContainer">';
    footerHtml += '<input id="positionSlider" type="range" class="mediaSlider positionSlider slider" step=".001" min="0" max="100" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
    footerHtml += '</div>';

    footerHtml += '<div class="currentTime" id="currentTime"></div>';
    footerHtml += '<div class="nowPlayingMediaInfo"></div>';

    footerHtml += '<button id="muteButton" class="mediaButton muteButton" title="Mute" type="button" onclick="MediaController.mute();" data-icon="audio" data-iconpos="notext" data-inline="true">Mute</button>';
    footerHtml += '<button id="unmuteButton" class="mediaButton unmuteButton" title="Unmute" type="button" onclick="MediaController.unMute();" data-icon="volume-off" data-iconpos="notext" data-inline="true">Unmute</button>';

    footerHtml += '<div class="volumeSliderContainer sliderContainer">';
    footerHtml += '<input type="range" class="mediaSlider volumeSlider slider" step=".05" min="0" max="1" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
    footerHtml += '</div>';

    footerHtml += '<button onclick="MediaController.showQualityFlyout();" id="qualityButton" class="mediaButton qualityButton" title="Quality" type="button" data-icon="gear" data-iconpos="notext" data-inline="true">Quality</button>';
    footerHtml += '<div class="mediaFlyoutContainer"><div id="qualityFlyout" style="display:none;" class="mediaPlayerFlyout"></div></div>';

    footerHtml += '<button onclick="MediaController.showAudioTracksFlyout();" id="audioTracksButton" class="imageButton mediaButton audioTracksButton" title="Audio tracks" type="button" data-icon="audiocd" data-iconpos="notext" data-inline="true">Audio Tracks</button>';
    footerHtml += '<div class="mediaFlyoutContainer"><div id="audioTracksFlyout" style="display:none;" class="mediaPlayerFlyout audioTracksFlyout"></div></div>';

    footerHtml += '<button onclick="MediaController.showSubtitleMenu();" id="subtitleButton" class="imageButton mediaButton subtitleButton" title="Subtitles" type="button" data-icon="subtitles" data-iconpos="notext" data-inline="true">Subtitles</button>';
    footerHtml += '<div class="mediaFlyoutContainer"><div id="subtitleFlyout" style="display:none;" class="mediaPlayerFlyout subtitleFlyout"></div></div>';

    footerHtml += '<button onclick="MediaController.showChaptersFlyout();" id="chaptersButton" class="mediaButton chaptersButton" title="Scenes" type="button" data-icon="video" data-iconpos="notext" data-inline="true">Scenes</button>';
    footerHtml += '<div class="mediaFlyoutContainer"><div id="chaptersFlyout" style="display:none;" class="mediaPlayerFlyout chaptersFlyout"></div></div>';

    footerHtml += '<button onclick="MediaController.toggleFullscreen();" id="fullscreenButton" class="mediaButton fullscreenButton" title="Fullscreen" type="button" data-icon="action" data-iconpos="notext" data-inline="true">Fullscreen</button>';

    footerHtml += '</div>';

    footerHtml += '<div id="footerNotifications"></div>';
    footerHtml += '</div>';

    $(document.body).append(footerHtml);

    var footerElem = $('#footer', document.body);
    footerElem.trigger('create');

    $(window).on("beforeunload", function () {

        // Close the connection gracefully when possible
        if (ApiClient.isWebSocketOpen() && !MediaPlayer.isPlaying()) {

            console.log('Sending close web socket command');
            ApiClient.closeWebSocket();
        }
    });
});

Dashboard.jQueryMobileInit();

$(document).on('pagebeforeshow', ".page", function () {

    var page = $(this);

    var userId = Dashboard.getCurrentUserId();
    ApiClient.currentUserId(userId);

    if (!userId) {

        if (this.id !== "loginPage" && !page.hasClass('wizardPage')) {

            Dashboard.logout();
            return;
        }

        Dashboard.ensureHeader(page);
        Dashboard.ensurePageTitle(page);
    }

    else {

        Dashboard.getCurrentUser().done(function (user) {

            if (user.Configuration.IsAdministrator) {
                Dashboard.ensureToolsMenu(page);
            } else if (page.hasClass('adminPage')) {
                window.location.replace("index.html");
            }

            Dashboard.ensureHeader(page, user);
            Dashboard.ensurePageTitle(page);
        });
    }

    if (!ApiClient.isWebSocketOpen()) {
        Dashboard.refreshSystemInfoFromServer();
    }
});


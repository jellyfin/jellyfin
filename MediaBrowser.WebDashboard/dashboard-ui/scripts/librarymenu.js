define(['imageLoader', 'layoutManager', 'viewManager', 'libraryBrowser', 'apphost', 'embyRouter', 'paper-icon-button-light', 'material-icons'], function (imageLoader, layoutManager, viewManager, libraryBrowser, appHost, embyRouter) {

    var enableBottomTabs = AppInfo.isNativeApp;
    var enableLibraryNavDrawer = !enableBottomTabs;

    var navDrawerElement;
    var navDrawerScrollContainer;
    var navDrawerInstance;

    var mainDrawerButton;

    function renderHeader() {

        var html = '';

        html += '<div class="primaryIcons">';
        var backIcon = browserInfo.safari ? 'chevron_left' : '&#xE5C4;';

        html += '<button type="button" is="paper-icon-button-light" class="headerButton headerButtonLeft headerBackButton hide"><i class="md-icon">' + backIcon + '</i></button>';

        html += '<button type="button" is="paper-icon-button-light" class="headerButton mainDrawerButton barsMenuButton headerButtonLeft hide"><i class="md-icon">menu</i></button>';
        html += '<button type="button" is="paper-icon-button-light" class="headerButton headerAppsButton barsMenuButton headerButtonLeft"><i class="md-icon">home</i></button>';

        html += '<div class="libraryMenuButtonText headerButton">' + Globalize.translate('ButtonHome') + '</div>';

        html += '<div class="viewMenuSecondary">';

        html += '<span class="headerSelectedPlayer"></span>';
        html += '<button is="paper-icon-button-light" class="btnCast headerButton headerButtonRight hide autoSize"><i class="md-icon">cast</i></button>';

        html += '<button type="button" is="paper-icon-button-light" class="headerButton headerButtonRight headerSearchButton hide autoSize"><i class="md-icon">search</i></button>';

        html += '<button is="paper-icon-button-light" class="headerButton headerButtonRight headerVoiceButton hide autoSize"><i class="md-icon">mic</i></button>';

        html += '<button is="paper-icon-button-light" class="headerButton headerButtonRight btnNotifications"><div class="btnNotificationsInner">0</div></button>';

        html += '<button is="paper-icon-button-light" class="headerButton headerButtonRight headerUserButton autoSize"><i class="md-icon">person</i></button>';

        if (!browserInfo.mobile && !Dashboard.isConnectMode()) {
            html += '<button is="paper-icon-button-light" class="headerButton headerButtonRight dashboardEntryHeaderButton autoSize" onclick="return LibraryMenu.onSettingsClicked(event);"><i class="md-icon">settings</i></button>';
        }

        html += '</div>';
        html += '</div>';

        html += '<div class="viewMenuBarTabs">';
        html += '</div>';

        var viewMenuBar = document.createElement('div');
        viewMenuBar.classList.add('viewMenuBar');
        viewMenuBar.innerHTML = html;

        document.querySelector('.skinHeader').appendChild(viewMenuBar);

        imageLoader.lazyChildren(document.querySelector('.viewMenuBar'));

        document.dispatchEvent(new CustomEvent("headercreated", {}));
        bindMenuEvents();
    }

    function onBackClick() {

        embyRouter.back();
    }

    function updateUserInHeader(user) {

        var header = document.querySelector('.viewMenuBar');
        if (!header) {
            return;
        }

        var headerUserButton = header.querySelector('.headerUserButton');
        var hasImage;

        if (user && user.name) {
            if (user.imageUrl) {

                var userButtonHeight = 26;

                var url = user.imageUrl;

                if (user.supportsImageParams) {
                    url += "&height=" + Math.round((userButtonHeight * Math.max(window.devicePixelRatio || 1, 2)));
                }

                if (headerUserButton) {
                    updateHeaderUserButton(headerUserButton, url);
                    hasImage = true;
                }
            }
        }

        if (headerUserButton && !hasImage) {

            updateHeaderUserButton(headerUserButton, null);
        }
        if (user) {
            updateLocalUser(user.localUser);
        }

        requiresUserRefresh = false;
    }

    function updateHeaderUserButton(headerUserButton, src) {

        if (src) {
            headerUserButton.classList.add('headerUserButtonRound');
            headerUserButton.classList.remove('autoSize');
            headerUserButton.innerHTML = '<img src="' + src + '" />';
        } else {
            headerUserButton.classList.remove('headerUserButtonRound');
            headerUserButton.classList.add('autoSize');
            headerUserButton.innerHTML = '<i class="md-icon">person</i>';
        }
    }

    function updateLocalUser(user) {

        var header = document.querySelector('.viewMenuBar');

        var headerSearchButton = header.querySelector('.headerSearchButton');
        var btnCast = header.querySelector('.btnCast');
        var dashboardEntryHeaderButton = header.querySelector('.dashboardEntryHeaderButton');

        if (user) {
            btnCast.classList.remove('hide');

            if (headerSearchButton) {
                headerSearchButton.classList.remove('hide');
            }

            if (dashboardEntryHeaderButton) {
                if (user.Policy.IsAdministrator) {
                    dashboardEntryHeaderButton.classList.remove('hide');
                } else {
                    dashboardEntryHeaderButton.classList.add('hide');
                }
            }

            require(['apphost'], function (apphost) {

                if (apphost.supports('voiceinput')) {
                    header.querySelector('.headerVoiceButton').classList.remove('hide');
                } else {
                    header.querySelector('.headerVoiceButton').classList.add('hide');
                }

            });

        } else {
            btnCast.classList.add('hide');
            header.querySelector('.headerVoiceButton').classList.add('hide');
            if (headerSearchButton) {
                headerSearchButton.classList.add('hide');
            }

            if (dashboardEntryHeaderButton) {
                dashboardEntryHeaderButton.classList.add('hide');
            }
        }
    }

    function showVoice() {
        require(['voiceDialog'], function (voiceDialog) {
            voiceDialog.showDialog();
        });
    }

    function showSearch() {
        Dashboard.navigate('search.html');
    }

    function onHeaderUserButtonClick(e) {
        Dashboard.showUserFlyout(e.target);
    }

    function onHeaderAppsButtonClick() {

        Dashboard.navigate('home.html');
    }

    function bindMenuEvents() {

        mainDrawerButton = document.querySelector('.mainDrawerButton');

        if (mainDrawerButton) {
            mainDrawerButton.addEventListener('click', toggleMainDrawer);
        }

        var headerBackButton = document.querySelector('.headerBackButton');
        if (headerBackButton) {
            headerBackButton.addEventListener('click', onBackClick);
        }

        var headerVoiceButton = document.querySelector('.headerVoiceButton');
        if (headerVoiceButton) {
            headerVoiceButton.addEventListener('click', showVoice);
        }

        var headerSearchButton = document.querySelector('.headerSearchButton');
        if (headerSearchButton) {
            headerSearchButton.addEventListener('click', showSearch);
        }

        var headerUserButton = document.querySelector('.headerUserButton');
        if (headerUserButton) {
            headerUserButton.addEventListener('click', onHeaderUserButtonClick);
        }

        var headerAppsButton = document.querySelector('.headerAppsButton');
        if (headerAppsButton) {
            headerAppsButton.addEventListener('click', onHeaderAppsButtonClick);
        }

        var viewMenuBar = document.querySelector(".viewMenuBar");
        initHeadRoom(viewMenuBar);

        viewMenuBar.querySelector('.btnNotifications').addEventListener('click', function () {
            Dashboard.navigate('notificationlist.html');
        });
    }

    function getItemHref(item, context) {

        return libraryBrowser.getHref(item, context);
    }

    var requiresUserRefresh = true;
    var lastOpenTime = new Date().getTime();

    function toggleMainDrawer() {

        if (navDrawerInstance.isVisible) {
            closeMainDrawer();
        } else {
            openMainDrawer();
        }
    }

    function openMainDrawer() {

        navDrawerInstance.open();
        lastOpenTime = new Date().getTime();
    }

    function onMainDrawerOpened() {

        if (browserInfo.mobile) {
            document.body.classList.add('bodyWithPopupOpen');
        }
    }
    function closeMainDrawer() {

        navDrawerInstance.close();
    }
    function onMainDrawerSelect(e) {

        if (!navDrawerInstance.isVisible) {
            document.body.classList.remove('bodyWithPopupOpen');
        } else {
            onMainDrawerOpened();
        }
    }

    function refreshLibraryInfoInDrawer(user, drawer) {

        var html = '';

        html += '<div style="height:.5em;"></div>';

        var homeHref = window.ApiClient ? 'home.html' : 'selectserver.html?showuser=1';

        html += '<a class="lnkMediaFolder sidebarLink" href="' + homeHref + '" onclick="return LibraryMenu.onLinkClicked(event, this);">';
        html += '<div style="background-image:url(\'css/images/mblogoicon.png\');width:' + 28 + 'px;height:' + 28 + 'px;background-size:contain;background-repeat:no-repeat;background-position:center center;border-radius:1000px;vertical-align:middle;margin:0 1.25em 0 1.55em;display:inline-block;"></div>';
        html += Globalize.translate('ButtonHome');
        html += '</a>';

        html += '<a class="sidebarLink lnkMediaFolder" data-itemid="remote" href="nowplaying.html" onclick="return LibraryMenu.onLinkClicked(event, this);"><i class="md-icon sidebarLinkIcon">tablet_android</i><span class="sidebarLinkText">' + Globalize.translate('ButtonRemote') + '</span></a>';

        html += '<div class="sidebarDivider"></div>';

        html += '<div class="libraryMenuOptions">';
        html += '</div>';

        var localUser = user.localUser;
        if (localUser && localUser.Policy.IsAdministrator) {

            html += '<div class="adminMenuOptions">';
            html += '<div class="sidebarDivider"></div>';

            html += '<div class="sidebarHeader">';
            html += Globalize.translate('HeaderAdmin');
            html += '</div>';

            html += '<a class="sidebarLink lnkMediaFolder lnkManageServer" data-itemid="dashboard" href="#"><i class="md-icon sidebarLinkIcon">dashboard</i><span class="sidebarLinkText">' + Globalize.translate('ButtonManageServer') + '</span></a>';
            html += '<a class="sidebarLink lnkMediaFolder editorViewMenu" data-itemid="editor" onclick="return LibraryMenu.onLinkClicked(event, this);" href="edititemmetadata.html"><i class="md-icon sidebarLinkIcon">mode_edit</i><span class="sidebarLinkText">' + Globalize.translate('MetadataManager') + '</span></a>';

            if (!browserInfo.mobile) {
                html += '<a class="sidebarLink lnkMediaFolder" data-itemid="reports" onclick="return LibraryMenu.onLinkClicked(event, this);" href="reports.html"><i class="md-icon sidebarLinkIcon">insert_chart</i><span class="sidebarLinkText">' + Globalize.translate('ButtonReports') + '</span></a>';
            }
            html += '</div>';
        }

        html += '<div class="userMenuOptions">';

        html += '<div class="sidebarDivider"></div>';

        if (user.localUser && (AppInfo.isNativeApp && browserInfo.android)) {
            html += '<a class="sidebarLink lnkMediaFolder lnkMySettings" onclick="return LibraryMenu.onLinkClicked(event, this);" href="mypreferencesmenu.html?userId=' + user.localUser.Id + '"><i class="md-icon sidebarLinkIcon">settings</i><span class="sidebarLinkText">' + Globalize.translate('ButtonSettings') + '</span></a>';
        }

        html += '<a class="sidebarLink lnkMediaFolder lnkManageOffline" data-itemid="manageoffline" onclick="return LibraryMenu.onLinkClicked(event, this);" href="mysync.html?mode=offline"><i class="md-icon sidebarLinkIcon">file_download</i><span class="sidebarLinkText">' + Globalize.translate('ManageOfflineDownloads') + '</span></a>';

        html += '<a class="sidebarLink lnkMediaFolder lnkSyncToOtherDevices" data-itemid="syncotherdevices" onclick="return LibraryMenu.onLinkClicked(event, this);" href="mysync.html"><i class="md-icon sidebarLinkIcon">sync</i><span class="sidebarLinkText">' + Globalize.translate('SyncToOtherDevices') + '</span></a>';

        if (Dashboard.isConnectMode()) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="selectserver" onclick="return LibraryMenu.onLinkClicked(event, this);" href="selectserver.html?showuser=1"><i class="md-icon sidebarLinkIcon">wifi</i><span class="sidebarLinkText">' + Globalize.translate('ButtonSelectServer') + '</span></a>';
        }

        if (user.localUser) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="logout" onclick="return LibraryMenu.onLogoutClicked(this);" href="#"><i class="md-icon sidebarLinkIcon">lock</i><span class="sidebarLinkText">' + Globalize.translate('ButtonSignOut') + '</span></a>';
        }

        html += '</div>';

        navDrawerScrollContainer.innerHTML = html;

        var lnkManageServer = navDrawerScrollContainer.querySelector('.lnkManageServer');
        if (lnkManageServer) {
            lnkManageServer.addEventListener('click', onManageServerClicked);
        }
    }

    function refreshDashboardInfoInDrawer(page, user) {

        loadNavDrawer().then(function () {
            if (!navDrawerScrollContainer.querySelector('.adminDrawerLogo')) {
                createDashboardMenu(page);
            } else {
                updateDashboardMenuSelectedItem();
            }
        });
    }

    function updateDashboardMenuSelectedItem() {

        var links = navDrawerScrollContainer.querySelectorAll('.sidebarLink');

        for (var i = 0, length = links.length; i < length; i++) {
            var link = links[i];

            var selected = false;

            var pageIds = link.getAttribute('data-pageids');
            if (pageIds) {
                selected = pageIds.split(',').indexOf(viewManager.currentView().id) != -1
            }

            if (selected) {
                link.classList.add('selectedSidebarLink');

                var title = '';

                link = link.querySelector('span') || link;
                var secondaryTitle = (link.innerText || link.textContent).trim();
                title += secondaryTitle;

                LibraryMenu.setTitle(title);

            } else {
                link.classList.remove('selectedSidebarLink');
            }
        }
    }

    function createDashboardMenu() {
        var html = '';

        html += '<a class="adminDrawerLogo clearLink" href="home.html">'
        html += '<img src="css/images/logo.png" />';
        html += '</a>';

        html += Dashboard.getToolsMenuHtml();

        html = html.split('href=').join('onclick="return LibraryMenu.onLinkClicked(event, this);" href=');

        navDrawerScrollContainer.innerHTML = html;

        updateDashboardMenuSelectedItem();
    }

    function onSidebarLinkClick() {
        var section = this.getElementsByClassName('sectionName')[0];
        var text = section ? section.innerHTML : this.innerHTML;

        LibraryMenu.setTitle(text);
    }

    function getUserViews(apiClient, userId) {

        return apiClient.getUserViews({}, userId).then(function (result) {

            var items = result.Items;

            var list = [];

            for (var i = 0, length = items.length; i < length; i++) {

                var view = items[i];

                list.push(view);

                if (view.CollectionType == 'livetv') {

                    view.ImageTags = {};
                    view.icon = 'live_tv';
                    view.onclick = "LibraryBrowser.showTab('livetv.html', 0);";

                    var guideView = Object.assign({}, view);
                    guideView.Name = Globalize.translate('ButtonGuide');
                    guideView.ImageTags = {};
                    guideView.icon = 'dvr';
                    guideView.url = 'livetv.html?tab=1';
                    guideView.onclick = "LibraryBrowser.showTab('livetv.html', 1);";
                    list.push(guideView);
                }
            }

            return list;
        });
    }

    function showBySelector(selector, show) {
        var elem = document.querySelector(selector);

        if (elem) {
            if (show) {
                elem.classList.remove('hide');
            } else {
                elem.classList.add('hide');
            }
        }
    }

    function updateLibraryMenu(user) {

        if (!user) {

            showBySelector('.lnkManageOffline', false);
            showBySelector('.lnkSyncToOtherDevices', false);
            showBySelector('.userMenuOptions', false);
            return;
        }

        if (user.Policy.EnableSync) {
            showBySelector('.lnkSyncToOtherDevices', true);
        } else {
            showBySelector('.lnkSyncToOtherDevices', false);
        }

        if (user.Policy.EnableSync && appHost.supports('sync')) {
            showBySelector('.lnkManageOffline', true);
        } else {
            showBySelector('.lnkManageOffline', false);
        }

        var userId = Dashboard.getCurrentUserId();

        var apiClient = window.ApiClient;

        var libraryMenuOptions = document.querySelector('.libraryMenuOptions');

        if (!libraryMenuOptions) {
            return;
        }

        getUserViews(apiClient, userId).then(function (result) {

            var items = result;

            var html = '';
            html += '<div class="sidebarHeader">';
            html += Globalize.translate('HeaderMedia');
            html += '</div>';

            html += items.map(function (i) {

                var icon = 'folder';
                var color = 'inherit';
                var itemId = i.Id;

                if (i.CollectionType == "channels") {
                    itemId = "channels";
                }
                else if (i.CollectionType == "livetv") {
                    itemId = "livetv";
                }

                if (i.CollectionType == "photos") {
                    icon = 'photo_library';
                    color = "#009688";
                }
                else if (i.CollectionType == "music" || i.CollectionType == "musicvideos") {
                    icon = 'library_music';
                    color = '#FB8521';
                }
                else if (i.CollectionType == "books") {
                    icon = 'library_books';
                    color = "#1AA1E1";
                }
                else if (i.CollectionType == "playlists") {
                    icon = 'view_list';
                    color = "#795548";
                }
                else if (i.CollectionType == "games") {
                    icon = 'games';
                    color = "#F44336";
                }
                else if (i.CollectionType == "movies") {
                    icon = 'video_library';
                    color = '#CE5043';
                }
                else if (i.CollectionType == "channels" || i.Type == 'Channel') {
                    icon = 'videocam';
                    color = '#E91E63';
                }
                else if (i.CollectionType == "tvshows") {
                    icon = 'tv';
                    color = "#4CAF50";
                }
                else if (i.CollectionType == "livetv") {
                    icon = 'live_tv';
                    color = "#293AAE";
                }

                icon = i.icon || icon;

                var onclick = i.onclick ? ' function(){' + i.onclick + '}' : 'null';
                return '<a data-itemid="' + itemId + '" class="lnkMediaFolder sidebarLink" onclick="return LibraryMenu.onLinkClicked(event, this, ' + onclick + ');" href="' + getItemHref(i, i.CollectionType) + '"><i class="md-icon sidebarLinkIcon" style="color:' + color + '">' + icon + '</i><span class="sectionName">' + i.Name + '</span></a>';

            }).join('');

            libraryMenuOptions.innerHTML = html;
            var elem = libraryMenuOptions;

            var sidebarLinks = elem.querySelectorAll('.sidebarLink');
            for (var i = 0, length = sidebarLinks.length; i < length; i++) {
                sidebarLinks[i].removeEventListener('click', onSidebarLinkClick);
                sidebarLinks[i].addEventListener('click', onSidebarLinkClick);
            }
        });
    }

    function onManageServerClicked() {

        closeMainDrawer();

        Dashboard.navigate('dashboard.html');
    }

    function getTopParentId() {

        return getParameterByName('topParentId') || null;
    }

    function getNavigateDelay() {
        // On mobile devices don't navigate until after the closing animation has completed or it may stutter
        return browserInfo.mobile ? 320 : 200;;
    }

    window.LibraryMenu = {
        getTopParentId: getTopParentId,

        onLinkClicked: function (event, link, action) {

            if (event.which != 1) {
                return true;
            }

            // There doesn't seem to be a way to detect if the drawer is in the process of opening, so try to handle that here
            if ((new Date().getTime() - lastOpenTime) > 200) {

                setTimeout(function () {
                    closeMainDrawer();


                    setTimeout(function () {
                        if (action) {
                            action();
                        } else {
                            Dashboard.navigate(link.href);
                        }
                    }, getNavigateDelay());

                }, 50);
            }

            event.stopPropagation();
            event.preventDefault();
            return false;
        },

        onLogoutClicked: function () {
            // There doesn't seem to be a way to detect if the drawer is in the process of opening, so try to handle that here
            if ((new Date().getTime() - lastOpenTime) > 200) {

                closeMainDrawer();

                setTimeout(function () {
                    Dashboard.logout();
                }, getNavigateDelay());
            }

            return false;
        },

        onHardwareMenuButtonClick: function () {
            toggleMainDrawer();
        },

        onSettingsClicked: function (event) {

            if (event.which != 1) {
                return true;
            }

            // There doesn't seem to be a way to detect if the drawer is in the process of opening, so try to handle that here
            Dashboard.navigate('dashboard.html');
            return false;
        },

        setTabs: function (type, selectedIndex, builder) {

            var viewMenuBarTabs;

            if (!type) {
                if (LibraryMenu.tabType) {

                    document.body.classList.remove('withTallToolbar');
                    viewMenuBarTabs = document.querySelector('.viewMenuBarTabs');
                    viewMenuBarTabs.innerHTML = '';
                    viewMenuBarTabs.classList.add('hide');
                    LibraryMenu.tabType = null;
                }
                return;
            }

            viewMenuBarTabs = document.querySelector('.viewMenuBarTabs');

            if (!LibraryMenu.tabType) {
                viewMenuBarTabs.classList.remove('hide');
            }

            require(['emby-tabs', 'emby-button'], function () {
                if (LibraryMenu.tabType != type) {

                    var index = 0;

                    viewMenuBarTabs.innerHTML = '<div is="emby-tabs"><div class="emby-tabs-slider">' + builder().map(function (t) {

                        var tabClass = selectedIndex == index ? 'emby-tab-button emby-tab-button-active' : 'emby-tab-button';

                        var tabHtml = '<button onclick="Dashboard.navigate(this.getAttribute(\'data-href\'));" data-href="' + t.href + '" is="emby-button" class="' + tabClass + '" data-index="' + index + '"><div class="emby-button-foreground">' + t.name + '</div></button>';
                        index++;
                        return tabHtml;

                    }).join('') + '</div></div>';

                    document.body.classList.add('withTallToolbar');
                    LibraryMenu.tabType = type;
                    return;
                }

                viewMenuBarTabs.querySelector('[is="emby-tabs"]').selectedIndex(selectedIndex);

                LibraryMenu.tabType = type;
            });
        },

        setTitle: function (title) {

            var html = title;

            var page = viewManager.currentView();
            if (page) {
                var helpUrl = page.getAttribute('data-helpurl');

                if (helpUrl) {
                    html += '<a href="' + helpUrl + '" target="_blank" class="clearLink" style="margin-left:2em;" title="' + Globalize.translate('ButtonHelp') + '"><button is="emby-button" type="button" class="button-accent-flat button-flat" style="margin:0;font-weight:normal;font-size:14px;padding:.25em;display:block;align-items:center;"><i class="md-icon">info</i><span>' + Globalize.translate('ButtonHelp') + '</span></button></a>';
                }
            }

            var libraryMenuButtonText = document.querySelector('.libraryMenuButtonText');
            if (libraryMenuButtonText) {
                libraryMenuButtonText.innerHTML = html;
            }

            document.title = title || 'Emby';
        },

        setTransparentMenu: function (transparent) {

            var viewMenuBar = document.querySelector('.viewMenuBar');

            if (viewMenuBar) {
                if (transparent) {
                    viewMenuBar.classList.add('semiTransparent');
                } else {
                    viewMenuBar.classList.remove('semiTransparent');
                }
            }
        }
    };

    function updateCastIcon() {

        var context = document;

        var btnCast = context.querySelector('.btnCast');

        if (!btnCast) {
            return;
        }

        var info = MediaController.getPlayerInfo();

        if (info.isLocalPlayer) {

            btnCast.querySelector('i').innerHTML = 'cast';
            btnCast.classList.remove('btnActiveCast');

            context.querySelector('.headerSelectedPlayer').innerHTML = '';

        } else {

            btnCast.querySelector('i').icon = 'cast_connected';
            btnCast.classList.add('btnActiveCast');
            context.querySelector('.headerSelectedPlayer').innerHTML = info.deviceName || info.name;
        }
    }

    function updateLibraryNavLinks(page) {

        var isLiveTvPage = page.classList.contains('liveTvPage');
        var isChannelsPage = page.classList.contains('channelsPage');
        var isEditorPage = page.classList.contains('metadataEditorPage');
        var isReportsPage = page.classList.contains('reportsPage');
        var isMySyncPage = page.classList.contains('mySyncPage');

        var id = isLiveTvPage || isChannelsPage || isEditorPage || isReportsPage || isMySyncPage || page.classList.contains('allLibraryPage') ?
            '' :
            getTopParentId() || '';

        var i, length;
        var elems = document.getElementsByClassName('lnkMediaFolder');

        for (i = 0, length = elems.length; i < length; i++) {

            var lnkMediaFolder = elems[i];
            var itemId = lnkMediaFolder.getAttribute('data-itemid');

            if (isChannelsPage && itemId == 'channels') {
                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else if (isLiveTvPage && itemId == 'livetv') {
                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else if (isEditorPage && itemId == 'editor') {
                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else if (isReportsPage && itemId == 'reports') {
                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else if (isMySyncPage && itemId == 'manageoffline' && window.location.href.toString().indexOf('mode=offline') != -1) {

                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else if (isMySyncPage && itemId == 'syncotherdevices' && window.location.href.toString().indexOf('mode=offline') == -1) {

                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else if (id && itemId == id) {
                lnkMediaFolder.classList.add('selectedMediaFolder');
            }
            else {
                lnkMediaFolder.classList.remove('selectedMediaFolder');
            }
        }
    }

    function onWebSocketMessage(e, data) {

        var msg = data;

        if (msg.MessageType === "UserConfigurationUpdated") {

            if (msg.Data.Id == Dashboard.getCurrentUserId()) {

                // refresh library menu
            }
        }
    }

    function updateViewMenuBar(page) {

        var viewMenuBar = document.querySelector('.viewMenuBar');

        if (viewMenuBar) {
            if (page.classList.contains('standalonePage')) {
                viewMenuBar.classList.add('hide');
            } else {
                viewMenuBar.classList.remove('hide');
            }

            if (page.classList.contains('type-interior') && !layoutManager.mobile) {
                viewMenuBar.classList.add('headroomDisabled');
            } else {
                viewMenuBar.classList.remove('headroomDisabled');
            }
        }

        if (requiresUserRefresh) {
            ConnectionManager.user(window.ApiClient).then(updateUserInHeader);
        }
    }

    pageClassOn('pagebeforeshow', 'page', function (e) {

        var page = this;

        if (!page.classList.contains('withTabs')) {
            LibraryMenu.setTabs(null);
        }
    });

    pageClassOn('pageshow', 'page', function (e) {

        var page = this;

        var isDashboardPage = page.classList.contains('type-interior');

        if (isDashboardPage) {
            if (mainDrawerButton) {
                mainDrawerButton.classList.remove('hide');
            }
            refreshDashboardInfoInDrawer(page);
        } else {

            if (mainDrawerButton) {
                if (enableLibraryNavDrawer) {
                    mainDrawerButton.classList.remove('hide');
                } else {
                    mainDrawerButton.classList.add('hide');
                }
            }

            if ((navDrawerElement && navDrawerElement.classList.contains('adminDrawer')) || (!navDrawerElement && enableLibraryNavDrawer)) {
                refreshLibraryDrawer();
            }
        }

        setDrawerClass(page);

        updateViewMenuBar(page);

        if (!e.detail.isRestored) {
            // Scroll back up so in case vertical scroll was messed with
            window.scrollTo(0, 0);
        }

        updateTitle(page);
        updateBackButton(page);

        if (page.classList.contains('libraryPage')) {

            document.body.classList.add('libraryDocument');
            document.body.classList.remove('dashboardDocument');
            document.body.classList.remove('hideMainDrawer');
        }
        else if (isDashboardPage) {

            document.body.classList.remove('libraryDocument');
            document.body.classList.add('dashboardDocument');
            document.body.classList.remove('hideMainDrawer');

        } else {

            document.body.classList.remove('libraryDocument');
            document.body.classList.remove('dashboardDocument');
            document.body.classList.add('hideMainDrawer');
        }

        updateLibraryNavLinks(page);
    });

    function updateTitle(page) {

        var title = page.getAttribute('data-title');

        if (title) {
            LibraryMenu.setTitle(title);
        }
    }

    function updateBackButton(page) {

        var backButton = document.querySelector('.headerBackButton');

        if (backButton) {
            if (page.getAttribute('data-backbutton') == 'true' && embyRouter.canGoBack()) {
                backButton.classList.remove('hide');
            } else {
                backButton.classList.add('hide');
            }
        }
    }

    function initHeadRoom(elem) {

        require(["headroom-window"], function (headroom) {

            headroom.add(elem);
        });
    }

    function initializeApiClient(apiClient) {

        Events.off(apiClient, 'websocketmessage', onWebSocketMessage);

        Events.on(apiClient, 'websocketmessage', onWebSocketMessage);
    }

    if (window.ApiClient) {
        initializeApiClient(window.ApiClient);
    }

    function setDrawerClass(page) {

        var admin = false;

        if (!page) {
            page = viewManager.currentView();
        }

        if (page && page.classList.contains('type-interior')) {
            admin = true;
        }

        var enableNavDrawer = admin || enableLibraryNavDrawer;
        if (enableNavDrawer) {
            loadNavDrawer().then(function () {
                if (admin) {
                    navDrawerElement.classList.add('adminDrawer');
                    navDrawerElement.classList.remove('darkDrawer');
                } else {
                    navDrawerElement.classList.add('darkDrawer');
                    navDrawerElement.classList.remove('adminDrawer');
                }
            });
        }
    }

    function refreshLibraryDrawer(user) {

        if (!enableLibraryNavDrawer) {
            return;
        }

        loadNavDrawer().then(function () {
            var promise = user ? Promise.resolve(user) : ConnectionManager.user(window.ApiClient);

            promise.then(function (user) {
                refreshLibraryInfoInDrawer(user);

                updateLibraryMenu(user.localUser);
            });
        });
    }

    function getNavDrawerOptions() {

        var drawerWidth = screen.availWidth - 50;
        // At least 240
        drawerWidth = Math.max(drawerWidth, 240);
        // But not exceeding 280
        drawerWidth = Math.min(drawerWidth, 260);

        var disableEdgeSwipe = false;

        if (browserInfo.safari) {
            disableEdgeSwipe = true;
        }

        // Default is 600px
        //drawer.responsiveWidth = '640px';

        return {
            target: navDrawerElement,
            onChange: onMainDrawerSelect,
            width: drawerWidth,
            disableEdgeSwipe: disableEdgeSwipe
        };
    }

    function loadNavDrawer() {

        if (navDrawerInstance) {
            return Promise.resolve(navDrawerInstance);
        }

        return new Promise(function (resolve, reject) {

            navDrawerElement = document.querySelector('.mainDrawer');
            navDrawerScrollContainer = navDrawerElement.querySelector('.scrollContainer');

            require(['navdrawer'], function (navdrawer) {
                navDrawerInstance = new navdrawer(getNavDrawerOptions());
                navDrawerElement.classList.remove('hide');
                resolve(navDrawerInstance);
            });
        });
    }

    renderHeader();

    Events.on(ConnectionManager, 'apiclientcreated', function (e, apiClient) {
        initializeApiClient(apiClient);
    });

    Events.on(ConnectionManager, 'localusersignedin', function (e, user) {
        setDrawerClass();
        ConnectionManager.user(ConnectionManager.getApiClient(user.ServerId)).then(function (user) {
            refreshLibraryDrawer(user);
            updateUserInHeader(user);
        });
    });

    Events.on(ConnectionManager, 'localusersignedout', updateUserInHeader);
    Events.on(MediaController, 'playerchange', updateCastIcon);

    setDrawerClass();

    if (enableBottomTabs) {
        require(['appfooter-shared', 'dockedtabs'], function (footer, dockedtabs) {
            new dockedtabs({
                appFooter: footer
            });
        });
    }
});
(function (window, document, $, devicePixelRatio) {

    function renderHeader() {

        var html = '';

        var backIcon = browserInfo.safari ? 'chevron-left' : 'arrow-back';

        html += '<paper-icon-button icon="' + backIcon + '" class="headerButton headerButtonLeft headerBackButton hide"></paper-icon-button>';

        if (AppInfo.enableNavDrawer) {
            html += '<paper-icon-button icon="menu" class="headerButton mainDrawerButton barsMenuButton headerButtonLeft"></paper-icon-button>';
        }

        html += '<div class="libraryMenuButtonText headerButton">' + Globalize.translate('ButtonHome') + '</div>';

        html += '<div class="viewMenuSecondary">';

        html += '<span class="headerSelectedPlayer"></span>';
        html += '<paper-icon-button icon="cast" class="btnCast headerButton headerButtonRight hide"></paper-icon-button>';

        if (AppInfo.enableSearchInTopMenu) {
            html += '<paper-icon-button icon="search" class="headerButton headerButtonRight headerSearchButton hide" onclick="Search.showSearchPanel();"></paper-icon-button>';
            html += '<div class="viewMenuSearch hide">';
            html += '<form class="viewMenuSearchForm">';
            html += '<input type="text" data-role="none" data-type="search" class="headerSearchInput" autocomplete="off" spellcheck="off" />';
            html += '<paper-icon-button icon="close" class="btnCloseSearch"></paper-icon-button>';
            html += '</form>';
            html += '</div>';
        }

        html += '<paper-icon-button icon="mic" class="headerButton headerButtonRight headerVoiceButton hide" onclick="VoiceInputManager.startListening();"></paper-icon-button>';

        html += '<paper-button class="headerButton headerButtonRight btnNotifications subdued" type="button" title="Notifications"><div class="btnNotificationsInner">0</div></paper-button>';

        if (!showUserAtTop()) {
            html += '<paper-icon-button icon="person" class="headerButton headerButtonRight headerUserButton" onclick="return Dashboard.showUserFlyout(this);"></paper-icon-button>';
        }

        if (!browserInfo.mobile && !Dashboard.isConnectMode()) {
            html += '<paper-icon-button icon="settings" class="headerButton headerButtonRight dashboardEntryHeaderButton" onclick="return LibraryMenu.onSettingsClicked(event);"></paper-icon-button>';
        }

        html += '</div>';

        var viewMenuBar = document.createElement('div');
        viewMenuBar.classList.add('viewMenuBar');
        viewMenuBar.classList.add('ui-body-b');
        viewMenuBar.innerHTML = html;

        document.body.appendChild(viewMenuBar);

        require(['imageLoader'], function (imageLoader) {
            imageLoader.lazyChildren(document.querySelector('.viewMenuBar'));
        });

        document.dispatchEvent(new CustomEvent("headercreated", {}));
        bindMenuEvents();
    }

    function onBackClick() {

        if (Dashboard.exitOnBack()) {
            Dashboard.exit();
        }
        else {
            history.back();
        }
    }

    function updateUserInHeader(user) {

        var header = document.querySelector('.viewMenuBar');

        var headerUserButton = header.querySelector('.headerUserButton');
        var hasImage;

        if (user && user.name) {
            if (user.imageUrl && AppInfo.enableUserImage) {

                var userButtonHeight = 26;

                var url = user.imageUrl;

                if (user.supportsImageParams) {
                    url += "&height=" + (userButtonHeight * Math.max(devicePixelRatio || 1, 2));
                }

                if (headerUserButton) {
                    headerUserButton.icon = null;
                    headerUserButton.src = url;
                    headerUserButton.classList.add('headerUserButtonRound');
                    hasImage = true;
                }
            }
        }

        if (headerUserButton && !hasImage) {
            headerUserButton.icon = 'person';
            headerUserButton.src = null;
            headerUserButton.classList.remove('headerUserButtonRound');

            // Looks like a bug in paper-icon-button that this doesn't get removed
            var headerUserButtonImg = headerUserButton.querySelector('img');
            if (headerUserButtonImg) {
                headerUserButtonImg.parentNode.removeChild(headerUserButtonImg);
            }
        }
        if (user) {
            updateLocalUser(user.localUser);
        }

        requiresUserRefresh = false;
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

            requirejs(['voice/voice'], function () {

                if (VoiceInputManager.isSupported()) {
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

    function bindMenuEvents() {

        var mainDrawerButton = document.querySelector('.mainDrawerButton');

        if (mainDrawerButton) {
            mainDrawerButton.addEventListener('click', openMainDrawer);
        }

        var headerBackButton = document.querySelector('.headerBackButton');
        if (headerBackButton) {
            headerBackButton.addEventListener('click', onBackClick);
        }

        var viewMenuBar = document.querySelector(".viewMenuBar");
        initHeadRoom(viewMenuBar);

        viewMenuBar.querySelector('.btnNotifications').addEventListener('click', function () {
            Dashboard.navigate('notificationlist.html');
        });
    }

    function getItemHref(item, context) {

        return LibraryBrowser.getHref(item, context);
    }

    var requiresDrawerRefresh = true;
    var requiresDashboardDrawerRefresh = true;
    var requiresUserRefresh = true;
    var lastOpenTime = new Date().getTime();

    function openMainDrawer() {

        var drawerPanel = document.querySelector('.mainDrawerPanel');
        drawerPanel.openDrawer();
        lastOpenTime = new Date().getTime();
    }

    function onMainDrawerOpened() {

        if (browserInfo.mobile) {
            document.body.classList.add('bodyWithPopupOpen');
        }

        var pageElem = $($.mobile.activePage)[0];

        if (requiresDrawerRefresh || requiresDashboardDrawerRefresh) {

            ConnectionManager.user(window.ApiClient).then(function (user) {

                var drawer = document.querySelector('.mainDrawerPanel .mainDrawer');

                if (requiresDrawerRefresh) {
                    ensureDrawerStructure(drawer);

                    refreshUserInfoInDrawer(user, drawer);
                    refreshLibraryInfoInDrawer(user, drawer);
                    refreshBottomUserInfoInDrawer(user, drawer);

                    document.dispatchEvent(new CustomEvent("libraryMenuCreated", {}));
                    updateLibraryMenu(user.localUser);
                }

                if (requiresDrawerRefresh || requiresDashboardDrawerRefresh) {
                    refreshDashboardInfoInDrawer(pageElem, user, drawer);
                    requiresDashboardDrawerRefresh = false;
                }

                requiresDrawerRefresh = false;
            });
        }

        updateLibraryNavLinks(pageElem);

        document.querySelector('.mainDrawerPanel #drawer').classList.add('verticalScrollingDrawer');
    }
    function closeMainDrawer() {

        document.querySelector('.mainDrawerPanel').closeDrawer();
    }
    function onMainDrawerSelect(e) {

        var drawer = e.target;

        if (drawer.selected != 'drawer') {
            document.body.classList.remove('bodyWithPopupOpen');
            document.querySelector('.mainDrawerPanel #drawer').classList.remove('verticalScrollingDrawer');
        } else {
            onMainDrawerOpened();
        }
    }

    function ensureDrawerStructure(drawer) {

        if (drawer.querySelector('.mainDrawerContent')) {
            return;
        }

        var html = '<div class="mainDrawerContent">';

        html += '<div class="userheader">';
        html += '</div>';
        html += '<div class="libraryDrawerContent">';
        html += '</div>';
        html += '<div class="dashboardDrawerContent">';
        html += '</div>';
        html += '<div class="userFooter">';
        html += '</div>';

        html += '</div>';

        drawer.innerHTML = html;
    }

    function refreshUserInfoInDrawer(user, drawer) {

        var html = '';

        var userAtTop = showUserAtTop();

        var homeHref = window.ApiClient ? 'index.html' : 'selectserver.html?showuser=1';

        var hasUserImage = user.imageUrl && AppInfo.enableUserImage;

        if (userAtTop) {

            html += '<div class="drawerUserPanel">';

            var imgWidth = 40;

            if (hasUserImage) {
                var url = user.imageUrl;
                if (user.supportsImageParams) {
                    url += "&width=" + (imgWidth * Math.max(devicePixelRatio || 1, 2));
                    html += '<div class="lazy drawerUserPanelUserImage" data-src="' + url + '" style="width:' + imgWidth + 'px;height:' + imgWidth + 'px;"></div>';
                }
            } else {
                html += '<div class="drawerUserPanelUserImage"><iron-icon icon="person" style="width:' + imgWidth + 'px;height:' + imgWidth + 'px;"></iron-icon></div>';
            }

            html += '<div class="drawerUserPanelUserName">';
            html += user.name;
            html += '</div>';

            html += '</div>';

            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="remote" href="index.html" onclick="return LibraryMenu.onLinkClicked(event, this);"><iron-icon icon="home" class="sidebarLinkIcon" style="color:#2196F3;"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonHome') + '</span></a>';

        } else {
            html += '<div style="margin-top:5px;"></div>';

            html += '<a class="lnkMediaFolder sidebarLink" href="' + homeHref + '" onclick="return LibraryMenu.onLinkClicked(event, this);">';
            html += '<div style="background-image:url(\'css/images/mblogoicon.png\');width:' + 28 + 'px;height:' + 28 + 'px;background-size:contain;background-repeat:no-repeat;background-position:center center;border-radius:1000px;vertical-align:middle;margin:0 1.6em 0 1.5em;display:inline-block;"></div>';
            html += Globalize.translate('ButtonHome');
            html += '</a>';
        }

        html += '<a class="sidebarLink lnkMediaFolder" data-itemid="remote" href="nowplaying.html" onclick="return LibraryMenu.onLinkClicked(event, this);"><iron-icon icon="tablet-android" class="sidebarLinkIcon" style="color:#673AB7;"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonRemote') + '</span></a>';

        var userHeader = drawer.querySelector('.userheader');

        userHeader.innerHTML = html;

        require(['imageLoader'], function (imageLoader) {
            imageLoader.fillImages(userHeader.getElementsByClassName('lazy'));
        });
    }

    function refreshLibraryInfoInDrawer(user, drawer) {

        var html = '';

        html += '<div class="sidebarDivider"></div>';

        html += '<div class="libraryMenuOptions">';
        html += '</div>';

        drawer.querySelector('.libraryDrawerContent').innerHTML = html;
    }

    function refreshDashboardInfoInDrawer(page, user, drawer) {

        var html = '';

        html += '<div class="sidebarDivider"></div>';

        html += Dashboard.getToolsMenuHtml(page);

        html = html.split('href=').join('onclick="return LibraryMenu.onLinkClicked(event, this);" href=');

        drawer.querySelector('.dashboardDrawerContent').innerHTML = html;
    }

    function refreshBottomUserInfoInDrawer(user, drawer) {

        var html = '';

        html += '<div class="adminMenuOptions">';
        html += '<div class="sidebarDivider"></div>';

        html += '<div class="sidebarHeader">';
        html += Globalize.translate('HeaderAdmin');
        html += '</div>';

        html += '<a class="sidebarLink lnkMediaFolder lnkManageServer" data-itemid="dashboard" href="#"><iron-icon icon="dashboard" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonManageServer') + '</span></a>';
        html += '<a class="sidebarLink lnkMediaFolder editorViewMenu" data-itemid="editor" onclick="return LibraryMenu.onLinkClicked(event, this);" href="edititemmetadata.html"><iron-icon icon="mode-edit" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonMetadataManager') + '</span></a>';

        if (!browserInfo.mobile) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="reports" onclick="return LibraryMenu.onLinkClicked(event, this);" href="reports.html"><iron-icon icon="insert-chart" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonReports') + '</span></a>';
        }
        html += '</div>';

        html += '<div class="userMenuOptions">';

        html += '<div class="sidebarDivider"></div>';

        if (user.localUser && showUserAtTop()) {
            html += '<a class="sidebarLink lnkMediaFolder lnkMySettings" onclick="return LibraryMenu.onLinkClicked(event, this);" href="mypreferencesmenu.html?userId=' + user.localUser.Id + '"><iron-icon icon="settings" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSettings') + '</span></a>';
        }

        html += '<a class="sidebarLink lnkMediaFolder lnkMySync" data-itemid="mysync" onclick="return LibraryMenu.onLinkClicked(event, this);" href="mysync.html"><iron-icon icon="sync" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSync') + '</span></a>';

        if (Dashboard.isConnectMode()) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="selectserver" onclick="return LibraryMenu.onLinkClicked(event, this);" href="selectserver.html?showuser=1"><iron-icon icon="wifi" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSelectServer') + '</span></a>';
        }

        if (showUserAtTop()) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="logout" onclick="return LibraryMenu.onLogoutClicked(this);" href="#"><iron-icon icon="lock" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSignOut') + '</span></a>';
        }

        html += '</div>';

        drawer.querySelector('.userFooter').innerHTML = html;

        drawer.querySelector('.lnkManageServer').addEventListener('click', onManageServerClicked);
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
                    view.icon = 'live-tv';
                    view.onclick = "LibraryBrowser.showTab('livetv.html', 0);";

                    var guideView = $.extend({}, view);
                    guideView.Name = Globalize.translate('ButtonGuide');
                    guideView.ImageTags = {};
                    guideView.icon = 'dvr';
                    guideView.url = 'livetv.html?tab=1';
                    guideView.onclick = "LibraryBrowser.showTab('livetv.html', 1);";
                    list.push(guideView);

                    var recordedTvView = $.extend({}, view);
                    recordedTvView.Name = Globalize.translate('ButtonRecordedTv');
                    recordedTvView.ImageTags = {};
                    recordedTvView.icon = 'video-library';
                    recordedTvView.url = 'livetv.html?tab=3';
                    recordedTvView.onclick = "LibraryBrowser.showTab('livetv.html', 3);";
                    list.push(recordedTvView);
                }
            }

            return list;
        });
    }

    function updateLibraryMenu(user) {

        if (!user) {

            $('.adminMenuOptions').addClass('hide');
            $('.lnkMySync').addClass('hide');
            $('.userMenuOptions').addClass('hide');
            return;
        }

        var userId = Dashboard.getCurrentUserId();

        var apiClient = window.ApiClient;

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
                    icon = 'photo-library';
                    color = "#009688";
                }
                else if (i.CollectionType == "music" || i.CollectionType == "musicvideos") {
                    icon = 'library-music';
                    color = '#FB8521';
                }
                else if (i.CollectionType == "books") {
                    icon = 'library-books';
                    color = "#1AA1E1";
                }
                else if (i.CollectionType == "playlists") {
                    icon = 'view-list';
                    color = "#795548";
                }
                else if (i.CollectionType == "games") {
                    icon = 'games';
                    color = "#F44336";
                }
                else if (i.CollectionType == "movies") {
                    icon = 'video-library';
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
                    icon = 'live-tv';
                    color = "#293AAE";
                }

                icon = i.icon || icon;

                var onclick = i.onclick ? ' function(){' + i.onclick + '}' : 'null';
                return '<a data-itemid="' + itemId + '" class="lnkMediaFolder sidebarLink" onclick="return LibraryMenu.onLinkClicked(event, this, ' + onclick + ');" href="' + getItemHref(i, i.CollectionType) + '"><iron-icon icon="' + icon + '" class="sidebarLinkIcon" style="color:' + color + '"></iron-icon><span class="sectionName">' + i.Name + '</span></a>';

            }).join('');

            var libraryMenuOptions = document.querySelector('.libraryMenuOptions');
            libraryMenuOptions.innerHTML = html;
            var elem = libraryMenuOptions;

            $('.sidebarLink', elem).off('click', onSidebarLinkClick).on('click', onSidebarLinkClick);
        });

        if (user.Policy.IsAdministrator) {
            $('.adminMenuOptions').removeClass('hide');
        } else {
            $('.adminMenuOptions').addClass('hide');
        }

        if (user.Policy.EnableSync) {
            $('.lnkMySync').removeClass('hide');
        } else {
            $('.lnkMySync').addClass('hide');
        }
    }

    function showUserAtTop() {
        return AppInfo.isNativeApp;
    }

    var requiresLibraryMenuRefresh = false;

    function onManageServerClicked() {

        closeMainDrawer();

        Dashboard.navigate('dashboard.html');
    }

    function getTopParentId() {

        return getParameterByName('topParentId') || null;
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

                    // On mobile devices don't navigate until after the closing animation has completed or it may stutter
                    var delay = browserInfo.mobile ? 350 : 200;

                    setTimeout(function () {
                        if (action) {
                            action();
                        } else {
                            Dashboard.navigate(link.href);
                        }
                    }, delay);

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

                // On mobile devices don't navigate until after the closing animation has completed or it may stutter
                var delay = browserInfo.mobile ? 350 : 200;

                setTimeout(function () {
                    Dashboard.logout();
                }, delay);
            }

            return false;
        },

        onHardwareMenuButtonClick: function () {
            openMainDrawer();
        },

        onSettingsClicked: function (event) {

            if (event.which != 1) {
                return true;
            }

            // There doesn't seem to be a way to detect if the drawer is in the process of opening, so try to handle that here
            Dashboard.navigate('dashboard.html');
            return false;
        },

        setTitle: function (title) {
            document.querySelector('.libraryMenuButtonText').innerHTML = title;
        },

        setBackButtonVisible: function (visible) {

            var backButton = document.querySelector('.headerBackButton');

            if (backButton) {
                if (visible) {
                    backButton.classList.remove('hide');
                } else {
                    backButton.classList.add('hide');
                }
            }
        },

        setMenuButtonVisible: function (visible) {

            var mainDrawerButton = document.querySelector('.mainDrawerButton');

            if (mainDrawerButton) {
                if (!visible && browserInfo.mobile) {
                    mainDrawerButton.classList.remove('hide');
                } else {
                    mainDrawerButton.classList.remove('hide');
                }
            }
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

        var info = MediaController.getPlayerInfo();

        if (info.isLocalPlayer) {

            btnCast.icon = 'cast';
            btnCast.classList.remove('btnActiveCast');

            context.querySelector('.headerSelectedPlayer').innerHTML = '';

        } else {

            btnCast.icon = 'cast-connected';
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
            else if (isMySyncPage && itemId == 'mysync') {
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

    function updateTabLinks(page) {

        var elems = page.querySelectorAll('.scopedLibraryViewNav a');

        var id = page.classList.contains('liveTvPage') || page.classList.contains('channelsPage') || page.classList.contains('metadataEditorPage') || page.classList.contains('reportsPage') || page.classList.contains('mySyncPage') || page.classList.contains('allLibraryPage') ?
            '' :
            getTopParentId() || '';

        if (!id) {
            return;
        }

        for (i = 0, length = elems.length; i < length; i++) {

            var lnk = elems[i];
            var src = lnk.href;

            if (src.indexOf('#') != -1) {
                continue;
            }

            src = replaceQueryString(src, 'topParentId', id);

            lnk.href = src;
        }
    }

    function onWebSocketMessage(e, data) {

        var msg = data;

        if (msg.MessageType === "UserConfigurationUpdated") {

            if (msg.Data.Id == Dashboard.getCurrentUserId()) {

                requiresLibraryMenuRefresh = true;
            }
        }
    }

    function buildViewMenuBar(page) {

        var viewMenuBar = document.querySelector('.viewMenuBar');

        if (page.classList.contains('standalonePage')) {
            viewMenuBar.classList.add('hide');
        } else {
            viewMenuBar.classList.remove('hide');
        }

        if (requiresUserRefresh) {
            ConnectionManager.user(window.ApiClient).then(updateUserInHeader);
        }
    }

    pageClassOn('pageinit', 'page', function () {

        var page = this;

        var isLibraryPage = page.classList.contains('libraryPage');

        if (isLibraryPage) {

            var navs = page.querySelectorAll('.libraryViewNav');
            for (var i = 0, length = navs.length; i < length; i++) {
                initHeadRoom(navs[i]);
            }
        }
    });

    pageClassOn('pagebeforeshow', 'page', function () {

        var page = this;

        if (page.classList.contains('type-interior')) {
            requiresDashboardDrawerRefresh = true;
        }

        buildViewMenuBar(page);
        updateTabLinks(page);
    });

    pageClassOn('pageshow', 'page', function () {

        var page = this;

        if (!NavHelper.isBack()) {
            // Scroll back up so in case vertical scroll was messed with
            window.scrollTo(0, 0);
        }

        updateTitle(page);
        updateBackButton(page);

        var isLibraryPage = page.classList.contains('libraryPage');

        if (isLibraryPage) {

            document.body.classList.add('libraryDocument');
            document.body.classList.remove('dashboardDocument');
            document.body.classList.remove('hideMainDrawer');
        }
        else if (page.classList.contains('type-interior')) {

            document.body.classList.remove('libraryDocument');
            document.body.classList.add('dashboardDocument');
            document.body.classList.remove('hideMainDrawer');

        } else {

            document.body.classList.remove('libraryDocument');
            document.body.classList.remove('dashboardDocument');
            document.body.classList.add('hideMainDrawer');
        }
    });

    function updateTitle(page) {
        var title = page.getAttribute('data-title') || page.getAttribute('data-contextname');

        if (!title) {
            var titleKey = getParameterByName('titlekey');

            if (titleKey) {
                title = Globalize.translate(titleKey);
            }
        }

        if (!title) {
            if (page.classList.contains('type-interior')) {
                title = Globalize.translate('ButtonHome');
            }
        }

        if (title) {
            LibraryMenu.setTitle(title);
        }
    }

    function updateBackButton(page) {

        var canGoBack = !page.classList.contains('homePage') && history.length > 0;

        var backButton = document.querySelector('.headerBackButton');

        var showBackButton = AppInfo.enableBackButton;

        if (!showBackButton) {
            showBackButton = page.getAttribute('data-backbutton') == 'true';
        }

        if (backButton) {
            if (canGoBack && showBackButton) {
                backButton.classList.remove('hide');
            } else {
                backButton.classList.add('hide');
            }
        }
    }

    function initHeadRoom(elem) {

        if (!AppInfo.enableHeadRoom) {
            return;
        }

        requirejs(["headroom"], function () {

            // construct an instance of Headroom, passing the element
            var headroom = new Headroom(elem, {
                // or scroll tolerance per direction
                tolerance: {
                    down: 40,
                    up: 0
                }
            });
            // initialise
            headroom.init();
            elem.classList.add('headroomEnabled');
        });
    }

    function initializeApiClient(apiClient) {

        requiresLibraryMenuRefresh = true;
        Events.off(apiClient, 'websocketmessage', onWebSocketMessage);

        Events.on(apiClient, 'websocketmessage', onWebSocketMessage);
    }

    if (window.ApiClient) {
        initializeApiClient(window.ApiClient);
    }

    function setDrawerClass() {

        var drawer = document.querySelector('.mainDrawerPanel #drawer');
        if (drawer) {
            drawer.classList.add('darkDrawer');
        }
    }

    var mainDrawerPanel = document.querySelector('.mainDrawerPanel');
    mainDrawerPanel.addEventListener('iron-select', onMainDrawerSelect);

    renderHeader();

    Events.on(ConnectionManager, 'apiclientcreated', function (e, apiClient) {
        initializeApiClient(apiClient);
    });

    Events.on(ConnectionManager, 'localusersignedin', function (e, user) {
        requiresLibraryMenuRefresh = true;
        requiresDrawerRefresh = true;
        setDrawerClass();
        ConnectionManager.user(ConnectionManager.getApiClient(user.ServerId)).then(updateUserInHeader);
    });

    Events.on(ConnectionManager, 'localusersignedout', function () {
        requiresLibraryMenuRefresh = true;
        requiresDrawerRefresh = true;
        updateUserInHeader();
    });

    Events.on(MediaController, 'playerchange', function () {
        updateCastIcon();
    });

    setDrawerClass();

})(window, document, jQuery, window.devicePixelRatio);

(function () {

    var isCurrentNavBack = false;

    window.addEventListener("navigate", function (e) {

        var data = e.detail.state || {};
        var direction = data.direction;

        isCurrentNavBack = direction == 'back';
    });

    function isBack() {

        return isCurrentNavBack;
    }

    window.NavHelper = {
        isBack: isBack
    };

})();
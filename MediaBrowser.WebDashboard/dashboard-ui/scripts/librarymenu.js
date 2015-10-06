(function (window, document, $, devicePixelRatio) {

    function renderHeader() {

        var html = '';

        var backIcon = $.browser.safari ? 'chevron-left' : 'arrow-back';

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
            html += '<iron-icon class="searchInputIcon" icon="search"></iron-icon>';
            html += '<paper-icon-button icon="close" class="btnCloseSearch"></paper-icon-button>';
            html += '</form>';
            html += '</div>';
        }

        html += '<paper-icon-button icon="mic" class="headerButton headerButtonRight headerVoiceButton hide" onclick="VoiceInputManager.startListening();"></paper-icon-button>';

        html += '<paper-button class="headerButton headerButtonRight btnNotifications subdued" type="button" title="Notifications"><div class="btnNotificationsInner">0</div></paper-button>';

        if (!showUserAtTop()) {
            html += '<paper-icon-button icon="person" class="headerButton headerButtonRight headerUserButton" onclick="return Dashboard.showUserFlyout(this);"></paper-icon-button>';
        }

        if (!$.browser.mobile && !Dashboard.isConnectMode()) {
            html += '<paper-icon-button icon="settings" class="headerButton headerButtonRight dashboardEntryHeaderButton" onclick="return LibraryMenu.onSettingsClicked(event);"></paper-icon-button>';
        }

        html += '</div>';

        var viewMenuBar = document.createElement('div');
        viewMenuBar.classList.add('viewMenuBar');
        viewMenuBar.classList.add('ui-body-b');
        viewMenuBar.innerHTML = html;

        document.body.appendChild(viewMenuBar);

        ImageLoader.lazyChildren(document.querySelector('.viewMenuBar'));

        Events.trigger(document, 'headercreated');
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

    function addUserToHeader(user) {

        var header = document.querySelector('.viewMenuBar');

        var headerSearchButton = document.querySelector('.headerSearchButton');

        if (user.localUser) {
            $('.btnCast', header).visible(true);

            if (headerSearchButton) {
                headerSearchButton.classList.remove('hide');
            }

            requirejs(['voice/voice'], function () {

                if (VoiceInputManager.isSupported()) {
                    document.querySelector('.headerVoiceButton').classList.remove('hide');
                } else {
                    document.querySelector('.headerVoiceButton').classList.add('hide');
                }

            });

        } else {
            $('.btnCast', header).visible(false);
            document.querySelector('.headerVoiceButton').classList.add('hide');
            if (headerSearchButton) {
                headerSearchButton.classList.add('hide');
            }
        }

        var dashboardEntryHeaderButton = document.querySelector('.dashboardEntryHeaderButton');

        if (dashboardEntryHeaderButton) {
            if (user.canManageServer) {
                dashboardEntryHeaderButton.classList.remove('hide');
            } else {
                dashboardEntryHeaderButton.classList.add('hide');
            }
        }

        if (user.name) {
            if (user.imageUrl && AppInfo.enableUserImage) {

                var userButtonHeight = 26;

                var url = user.imageUrl;

                if (user.supportsImageParams) {
                    url += "&height=" + (userButtonHeight * Math.max(devicePixelRatio || 1, 2));
                }

                var headerUserButton = header.querySelector('.headerUserButton');
                if (headerUserButton) {
                    headerUserButton.icon = null;
                    headerUserButton.src = url;
                    headerUserButton.classList.add('headerUserButtonRound');
                }
            }
        }
    }

    function bindMenuEvents() {

        var mainDrawerButton = document.querySelector('.mainDrawerButton');

        if (mainDrawerButton) {
            if (AppInfo.isTouchPreferred || $.browser.mobile) {

                Events.on(mainDrawerButton, 'click', openMainDrawer);

            } else {
                $(mainDrawerButton).createHoverTouch().on('hovertouch', openMainDrawer);
            }
        }

        var headerBackButton = document.querySelector('.headerBackButton');
        if (headerBackButton) {
            Events.on(headerBackButton, 'click', onBackClick);
        }

        var viewMenuBar = document.querySelector(".viewMenuBar");
        initHeadRoom(viewMenuBar);
    }

    function updateViewMenuBarHeadroom(page, viewMenuBar) {

        //if (page.classList.contains('libraryPage')) {
        //    // Don't like this timeout at all but if headroom is activated during the page events it will jump and flicker on us
        //    setTimeout(reEnableHeadroom, 700);
        //} else {
        //    viewMenuBar.classList.add('headroomDisabled');
        //}
    }

    function reEnableHeadroom() {

        //var headroomDisabled = document.querySelectorAll('.headroomDisabled');
        //for (var i = 0, length = headroomDisabled.length; i < length; i++) {
        //    headroomDisabled[i].classList.remove('headroomDisabled');
        //}
    }

    function getItemHref(item, context) {

        return LibraryBrowser.getHref(item, context);
    }

    var requiresDrawerRefresh = true;
    var requiresDashboardDrawerRefresh = true;
    var lastOpenTime = new Date().getTime();

    function openMainDrawer() {

        var drawerPanel = document.querySelector('.mainDrawerPanel');
        drawerPanel.openDrawer();
        lastOpenTime = new Date().getTime();
    }
    function onMainDrawerOpened() {

        if ($.browser.mobile) {
            document.body.classList.add('bodyWithPopupOpen');
        }

        var pageElem = $($.mobile.activePage)[0];

        if (requiresDrawerRefresh || requiresDashboardDrawerRefresh) {

            ConnectionManager.user(window.ApiClient).done(function (user) {

                var drawer = document.querySelector('.mainDrawerPanel .mainDrawer');

                if (requiresDrawerRefresh) {
                    ensureDrawerStructure(drawer);

                    refreshUserInfoInDrawer(user, drawer);
                    refreshLibraryInfoInDrawer(user, drawer);
                    refreshBottomUserInfoInDrawer(user, drawer);

                    Events.trigger(document, 'libraryMenuCreated');
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
    function onMainDrawerClosed() {

        document.body.classList.remove('bodyWithPopupOpen');
        document.querySelector('.mainDrawerPanel #drawer').classList.remove('verticalScrollingDrawer');
    }
    function closeMainDrawer() {

        document.querySelector('.mainDrawerPanel').closeDrawer();
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

        var homeHref = window.ApiClient ? 'index.html' : 'selectserver.html';

        var hasUserImage = user.imageUrl && AppInfo.enableUserImage;

        if (userAtTop) {

            html += '<div class="drawerUserPanel">';
            html += '<div class="drawerUserPanelInner">';
            html += '<div class="drawerUserPanelContent">';

            var imgWidth = 60;

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

        ImageLoader.fillImages(userHeader.getElementsByClassName('lazy'));
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

    function replaceAll(string, find, replace) {
        return string.replace(new RegExp(escapeRegExp(find), 'g'), replace);
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

        if (!$.browser.mobile) {
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
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="selectserver" onclick="return LibraryMenu.onLinkClicked(event, this);" href="selectserver.html"><iron-icon icon="wifi" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSelectServer') + '</span></a>';
        }

        if (showUserAtTop()) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="logout" onclick="return LibraryMenu.onLogoutClicked(this);" href="#"><iron-icon icon="lock" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSignOut') + '</span></a>';
        }

        html += '</div>';

        drawer.querySelector('.userFooter').innerHTML = html;

        Events.on(drawer.querySelector('.lnkManageServer'), 'click', onManageServerClicked);
    }

    function onSidebarLinkClick() {
        var section = this.getElementsByClassName('sectionName')[0];
        var text = section ? section.innerHTML : this.innerHTML;

        LibraryMenu.setTitle(text);
    }

    function getUserViews(apiClient, userId) {

        var deferred = $.Deferred();

        apiClient.getUserViews({}, userId).done(function (result) {

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

            deferred.resolveWith(null, [list]);
        });

        return deferred.promise();
    }

    function updateLibraryMenu(user) {

        if (!user) {

            $('.adminMenuOptions').visible(false);
            $('.lnkMySync').visible(false);
            $('.userMenuOptions').visible(false);
            return;
        }

        var userId = Dashboard.getCurrentUserId();

        var apiClient = window.ApiClient;

        getUserViews(apiClient, userId).done(function (result) {

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
            $('.adminMenuOptions').visible(true);
        } else {
            $('.adminMenuOptions').visible(false);
        }

        if (user.Policy.EnableSync) {
            $('.lnkMySync').visible(true);
        } else {
            $('.lnkMySync').visible(false);
        }
    }

    function showUserAtTop() {
        return Dashboard.isConnectMode() || $.browser.mobile;
    }

    var requiresLibraryMenuRefresh = false;
    var requiresViewMenuRefresh = false;

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

                    setTimeout(function () {
                        if (action) {
                            action();
                        } else {
                            Dashboard.navigate(link.href);
                        }
                    }, 400);
                }, 50);
            }

            return false;
        },

        onLogoutClicked: function () {
            // There doesn't seem to be a way to detect if the drawer is in the process of opening, so try to handle that here
            if ((new Date().getTime() - lastOpenTime) > 200) {

                closeMainDrawer();

                setTimeout(function () {
                    Dashboard.logout();
                }, 400);
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
                if (!visible && $.browser.mobile) {
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

        var info = MediaController.getPlayerInfo();

        if (info.isLocalPlayer) {

            $('.btnCast').removeClass('btnActiveCast').each(function () {
                this.icon = 'cast';
            });
            $('.headerSelectedPlayer').html('');

        } else {

            $('.btnCast').addClass('btnActiveCast').each(function () {
                this.icon = 'cast-connected';
            });

            $('.headerSelectedPlayer').html((info.deviceName || info.name));
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
        var context = getParameterByName('context');

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
            if (viewMenuBar) {
                viewMenuBar.classList.add('hide');
            }
            return;
        }

        if (requiresViewMenuRefresh) {
            if (viewMenuBar) {
                viewMenuBar.parentNode.removeChild(viewMenuBar);
                viewMenuBar = null;
            }
        }

        if (!viewMenuBar) {

            renderHeader();
            updateViewMenuBarHeadroom(page, document.querySelector('.viewMenuBar'));

            updateCastIcon();

            updateLibraryNavLinks(page);
            requiresViewMenuRefresh = false;

            ConnectionManager.user(window.ApiClient).done(addUserToHeader);

        } else {
            viewMenuBar.classList.remove('hide');
            updateLibraryNavLinks(page);
            updateViewMenuBarHeadroom(page, viewMenuBar);
            requiresViewMenuRefresh = false;
        }
    }

    pageClassOn('pagebeforeshow', 'page', function () {

        var page = this;

        if (page.classList.contains('type-interior')) {
            requiresDashboardDrawerRefresh = true;
        }

        onPageBeforeShowDocumentReady(page);

    });

    pageClassOn('pageshow', 'page', function () {

        var page = this;

        onPageShowDocumentReady(page);

    });

    //pageClassOn('pagebeforehide', 'page', function () {

    //    var headroomEnabled = document.querySelectorAll('.headroomEnabled');
    //    for (var i = 0, length = headroomEnabled.length; i < length; i++) {
    //        headroomEnabled[i].classList.add('headroomDisabled');
    //    }

    //});

    function onPageBeforeShowDocumentReady(page) {

        buildViewMenuBar(page);

        var isLibraryPage = page.classList.contains('libraryPage');
        var darkDrawer = false;

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

        var mainDrawerButton = document.querySelector('.mainDrawerButton');

        if (mainDrawerButton) {
            if (page.getAttribute('data-menubutton') == 'false' && $.browser.mobile) {
                mainDrawerButton.classList.remove('hide');
            } else {
                mainDrawerButton.classList.remove('hide');
            }
        }

        if (isLibraryPage) {

            document.body.classList.add('libraryDocument');
            document.body.classList.remove('dashboardDocument');
            document.body.classList.remove('hideMainDrawer');

            var navs = page.querySelectorAll('.libraryViewNav');
            for (var i = 0, length = navs.length; i < length; i++) {
                initHeadRoom(navs[i]);
            }
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

        if (!Dashboard.isConnectMode() && !$.browser.mobile) {
            darkDrawer = true;
        }

        var drawer = document.querySelector('.mainDrawerPanel #drawer');
        if (drawer) {
            if (darkDrawer) {
                drawer.classList.add('darkDrawer');
            } else {
                drawer.classList.remove('darkDrawer');
            }
        }

        updateBackButton(page);
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

    function onPageShowDocumentReady(page) {

        if (!NavHelper.isBack()) {
            // Scroll back up so in case vertical scroll was messed with
            window.scrollTo(0, 0);
        }
        updateTabLinks(page);
    }

    function initHeadRoom(elem) {

        if (!AppInfo.enableHeadRoom) {
            return;
        }

        requirejs(["thirdparty/headroom"], function () {

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

    Dashboard.ready(function () {

        if (window.ApiClient) {
            initializeApiClient(window.ApiClient);
        }

        Events.on(ConnectionManager, 'apiclientcreated', function (e, apiClient) {
            initializeApiClient(apiClient);

        });

        Events.on(ConnectionManager, 'localusersignedin', function () {
            requiresLibraryMenuRefresh = true;
            requiresViewMenuRefresh = true;
            requiresDrawerRefresh = true;
        });

        Events.on(ConnectionManager, 'localusersignedout', function () {
            requiresLibraryMenuRefresh = true;
            requiresViewMenuRefresh = true;
            requiresDrawerRefresh = true;
        });

        Events.on(MediaController, 'playerchange', function () {
            updateCastIcon();
        });

        var mainDrawerPanel = document.querySelector('.mainDrawerPanel');
        Events.on(mainDrawerPanel, 'paper-drawer-panel-open', onMainDrawerOpened);
        Events.on(mainDrawerPanel, 'paper-drawer-panel-close', onMainDrawerClosed);
    });

})(window, document, jQuery, window.devicePixelRatio);

$.fn.createHoverTouch = function () {

    var preventHover = false;
    var timerId;

    function startTimer(elem) {

        stopTimer();

        timerId = setTimeout(function () {

            Events.trigger(elem, 'hovertouch');
        }, 300);
    }

    function stopTimer(elem) {

        if (timerId) {
            clearTimeout(timerId);
            timerId = null;
        }
    }

    return $(this).on('mouseenter', function () {

        if (preventHover === true) {
            preventHover = false;
            return;
        }

        startTimer(this);

    }).on('mouseleave', function () {

        stopTimer(this);

    }).on('touchstart', function () {

        preventHover = true;

    }).on('click', function () {

        preventHover = true;

        if (preventHover) {
            Events.trigger(this, 'hovertouch');
            stopTimer(this);
            preventHover = false;
        }
    });

};

(function () {

    var isCurrentNavBack = false;

    $(window).on("navigate", function (e, data) {
        data = data.state || {};
        isCurrentNavBack = data.direction == 'back';
    });

    function isBack() {

        return isCurrentNavBack;
    }

    window.NavHelper = {
        isBack: isBack
    };

})();
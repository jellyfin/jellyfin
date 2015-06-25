(function (window, document, $, devicePixelRatio) {

    var backStack = [];
    var addNextToBackStack = true;

    function renderHeader() {

        var html = '<div class="viewMenuBar ui-bar-b">';

        if (AppInfo.enableBackButton) {
            html += '<paper-icon-button icon="chevron-left" class="headerButton headerButtonLeft headerBackButton"></paper-icon-button>';
        }

        html += '<paper-icon-button icon="menu" class="headerButton mainDrawerButton barsMenuButton headerButtonLeft"></paper-icon-button>';

        html += '<div class="libraryMenuButtonText headerButton">' + Globalize.translate('ButtonHome') + '</div>';

        html += '<div class="viewMenuSecondary">';

        html += '<span class="headerSelectedPlayer"></span>';
        html += '<paper-icon-button icon="cast" class="btnCast headerButton headerButtonRight hide"></paper-icon-button>';

        html += '<paper-icon-button icon="search" class="headerButton headerButtonRight headerSearchButton hide" onclick="Search.showSearchPanel();"></paper-icon-button>';
        html += '<div class="viewMenuSearch hide">';
        html += '<form class="viewMenuSearchForm">';
        html += '<input type="text" data-role="none" data-type="search" class="headerSearchInput" autocomplete="off" spellcheck="off" />';
        html += '<div class="searchInputIcon fa fa-search"></div>';
        html += '<paper-icon-button icon="close" class="btnCloseSearch"></paper-icon-button>';
        html += '</form>';
        html += '</div>';

        html += '<paper-icon-button icon="mic" class="headerButton headerButtonRight headerVoiceButton hide" onclick="VoiceInputManager.startListening();"></paper-icon-button>';

        if (!showUserAtTop()) {
            html += '<button class="headerButton headerButtonRight headerUserButton" type="button" data-role="none" onclick="Dashboard.showUserFlyout(this);">';
            html += '<div class="fa fa-user"></div>';
            html += '</button>';
        }

        if (!$.browser.mobile && !AppInfo.isNativeApp) {
            html += '<paper-icon-button icon="settings" class="headerButton headerButtonRight dashboardEntryHeaderButton hide" onclick="Dashboard.navigate(\'dashboard.html\');"></paper-icon-button>';
        }

        html += '</div>';

        html += '</div>';

        $(document.body).append(html);
        $('.viewMenuBar').lazyChildren();

        $(document).trigger('headercreated');
        bindMenuEvents();
    }

    function onBackClick() {

        if (Dashboard.exitOnBack()) {
            Dashboard.exit();
        }
        else {
            addNextToBackStack = false;

            backStack.length = Math.max(0, backStack.length - 1);
            history.back();
        }
    }

    function addUserToHeader(user) {

        var header = $('.viewMenuBar');

        if (user.localUser) {
            $('.btnCast', header).visible(true);
            $('.headerSearchButton', header).visible(true);

            requirejs(['voice/voice'], function () {

                if (VoiceInputManager.isSupported()) {
                    $('.headerVoiceButton', header).visible(true);
                } else {
                    $('.headerVoiceButton', header).visible(false);
                }

            });

        } else {
            $('.btnCast', header).visible(false);
            $('.headerVoiceButton', header).visible(false);
            $('.headerSearchButton', header).visible(false);
        }

        if (user.canManageServer) {
            $('.dashboardEntryHeaderButton', header).visible(true);
        } else {
            $('.dashboardEntryHeaderButton', header).visible(false);
        }

        var userButtonHtml = '';
        if (user.name) {

            if (user.imageUrl && AppInfo.enableUserImage) {

                var userButtonHeight = 26;

                var url = user.imageUrl;

                if (user.supportsImageParams) {
                    url += "&height=" + (userButtonHeight * Math.max(devicePixelRatio || 1, 2));
                }

                userButtonHtml += '<div class="lazy headerUserImage" data-src="' + url + '" style="width:' + userButtonHeight + 'px;height:' + userButtonHeight + 'px;"></div>';
            } else {
                userButtonHtml += '<div class="fa fa-user"></div>';
            }
            $('.headerUserButton', header).html(userButtonHtml).lazyChildren();
        }
    }

    function bindMenuEvents() {

        if (AppInfo.isTouchPreferred) {

            if ('ontouchend' in document) {
                $('.mainDrawerButton').on('touchend click', openMainDrawer);
            } else {
                $('.mainDrawerButton').on('click', openMainDrawer);
            }

        } else {
            $('.mainDrawerButton').createHoverTouch().on('hovertouch', openMainDrawer);
        }

        $('.headerBackButton').on('click', onBackClick);

        // Have to wait for document ready here because otherwise 
        // we may see the jQM redirect back and forth problem
        $(initViewMenuBarHeadroom);
    }

    function initViewMenuBarHeadroom() {

        // grab an element
        var viewMenuBar = document.getElementsByClassName("viewMenuBar")[0];
        initHeadRoom(viewMenuBar);
    }

    function updateViewMenuBarHeadroom(page, viewMenuBar) {

        if ($(page).hasClass('libraryPage')) {
            // Don't like this timeout at all but if headroom is activated during the page events it will jump and flicker on us
            setTimeout(reEnableHeadroom, 700);
        } else {
            viewMenuBar.addClass('headroomDisabled');
        }
    }

    function reEnableHeadroom() {
        $('.headroomDisabled').removeClass('headroomDisabled');
    }

    function getItemHref(item, context) {

        return LibraryBrowser.getHref(item, context);
    }

    var requiresDrawerRefresh = true;
    var requiresDashboardDrawerRefresh = true;
    var lastOpenTime = new Date().getTime();

    function openMainDrawer() {

        var drawerPanel = $('.mainDrawerPanel')[0];
        drawerPanel.openDrawer();
        lastOpenTime = new Date().getTime();
    }
    function onMainDrawerOpened() {

        if ($.browser.mobile) {
            $(document.body).addClass('bodyWithPopupOpen');
        }

        var drawer = $('.mainDrawerPanel .mainDrawer');

        ConnectionManager.user(window.ApiClient).done(function (user) {

            if (requiresDrawerRefresh) {
                ensureDrawerStructure(drawer);

                refreshUserInfoInDrawer(user, drawer);
                refreshLibraryInfoInDrawer(user, drawer);
                refreshBottomUserInfoInDrawer(user, drawer);

                $(document).trigger('libraryMenuCreated');
                updateLibraryMenu(user.localUser);
            }

            if (requiresDrawerRefresh || requiresDashboardDrawerRefresh) {
                refreshDashboardInfoInDrawer($.mobile.activePage, user, drawer);
                requiresDashboardDrawerRefresh = false;
            }

            requiresDrawerRefresh = false;

            updateLibraryNavLinks($.mobile.activePage);
        });

        $('.mainDrawerPanel #drawer').addClass('verticalScrollingDrawer');
    }
    function onMainDrawerClosed() {

        $(document.body).removeClass('bodyWithPopupOpen');
        $('.mainDrawerPanel #drawer').removeClass('verticalScrollingDrawer');
    }
    function closeMainDrawer() {

        var drawerPanel = $('.mainDrawerPanel')[0];
        drawerPanel.closeDrawer();
    }

    function ensureDrawerStructure(drawer) {

        if ($('.mainDrawerContent', drawer).length) {
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

        $(drawer).html(html);
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
                html += '<div class="fa fa-user drawerUserPanelUserImage" style="font-size:' + imgWidth + 'px;"></div>';
            }

            html += '<div class="drawerUserPanelUserName">';
            html += user.name;
            html += '</div>';

            html += '</div>';
            html += '</div>';
            html += '</div>';

            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="remote" href="index.html" onclick="return LibraryMenu.onLinkClicked(this);"><iron-icon icon="home" class="sidebarLinkIcon" style="color:#2196F3;"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonHome') + '</span></a>';

        } else {
            html += '<div style="margin-top:5px;"></div>';

            html += '<a class="lnkMediaFolder sidebarLink" href="' + homeHref + '" onclick="return LibraryMenu.onLinkClicked(this);">';
            html += '<div class="lazy" data-src="css/images/mblogoicon.png" style="width:' + 28 + 'px;height:' + 28 + 'px;background-size:contain;background-repeat:no-repeat;background-position:center center;border-radius:1000px;vertical-align:middle;margin:0 1.6em 0 1.5em;display:inline-block;"></div>';
            html += Globalize.translate('ButtonHome');
            html += '</a>';
        }

        html += '<a class="sidebarLink lnkMediaFolder" data-itemid="remote" href="nowplaying.html" onclick="return LibraryMenu.onLinkClicked(this);"><iron-icon icon="tablet-android" class="sidebarLinkIcon" style="color:#673AB7;"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonRemote') + '</span></a>';

        $('.userheader', drawer).html(html).lazyChildren();
    }

    function refreshLibraryInfoInDrawer(user, drawer) {

        var html = '';

        html += '<div class="sidebarDivider"></div>';

        html += '<div class="libraryMenuOptions">';
        html += '</div>';

        $('.libraryDrawerContent', drawer).html(html);
    }

    function refreshDashboardInfoInDrawer(page, user, drawer) {

        var html = '';

        html += '<div class="sidebarDivider"></div>';

        html += Dashboard.getToolsMenuHtml(page);

        html = html.split('href=').join('onclick="return LibraryMenu.onLinkClicked(this);" href=');

        $('.dashboardDrawerContent', drawer).html(html);
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
        html += '<a class="sidebarLink lnkMediaFolder editorViewMenu" data-itemid="editor" onclick="return LibraryMenu.onLinkClicked(this);" href="edititemmetadata.html"><iron-icon icon="mode-edit" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonMetadataManager') + '</span></a>';

        if (!$.browser.mobile && !AppInfo.isTouchPreferred) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="reports" onclick="return LibraryMenu.onLinkClicked(this);" href="reports.html"><iron-icon icon="insert-chart" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonReports') + '</span></a>';
        }
        html += '</div>';

        html += '<div class="userMenuOptions">';
        html += '<div class="sidebarDivider"></div>';

        html += '<a class="sidebarLink lnkMediaFolder" data-itemid="inbox" onclick="return LibraryMenu.onLinkClicked(this);" href="notificationlist.html"><iron-icon icon="inbox" class="sidebarLinkIcon"></iron-icon>';
        html += Globalize.translate('ButtonInbox');
        html += '<div class="btnNotifications"><div class="btnNotificationsInner">0</div></div>';
        html += '</a>';

        if (user.localUser && showUserAtTop()) {
            html += '<a class="sidebarLink lnkMediaFolder lnkMySettings" onclick="return LibraryMenu.onLinkClicked(this);" data-itemid="mysync" href="mypreferencesdisplay.html?userId=' + user.localUser.Id + '"><iron-icon icon="settings" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSettings') + '</span></a>';
        }

        html += '<a class="sidebarLink lnkMediaFolder lnkMySync" data-itemid="mysync" onclick="return LibraryMenu.onLinkClicked(this);" href="mysync.html"><iron-icon icon="refresh" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSync') + '</span></a>';

        if (Dashboard.isConnectMode()) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="selectserver" onclick="return LibraryMenu.onLinkClicked(this);" href="selectserver.html"><span class="fa fa-globe sidebarLinkIcon"></span><span class="sidebarLinkText">' + Globalize.translate('ButtonSelectServer') + '</span></a>';
        }

        if (showUserAtTop()) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="logout" onclick="return LibraryMenu.onLogoutClicked(this);" href="#"><iron-icon icon="lock" class="sidebarLinkIcon"></iron-icon><span class="sidebarLinkText">' + Globalize.translate('ButtonSignOut') + '</span></a>';
        }

        html += '</div>';

        $('.userFooter', drawer).html(html);

        $('.lnkManageServer', drawer).on('click', onManageServerClicked);
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

        apiClient.getUserViews(userId).done(function (result) {

            var items = result.Items;

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

                if (i.Type == 'Channel') {
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

                return '<a data-itemid="' + itemId + '" class="lnkMediaFolder sidebarLink" onclick="return LibraryMenu.onLinkClicked(this);" href="' + getItemHref(i, i.CollectionType) + '"><iron-icon icon="' + icon + '" class="sidebarLinkIcon" style="color:' + color + '"></iron-icon><span class="sectionName">' + i.Name + '</span></a>';

            }).join('');

            var elem = $('.libraryMenuOptions').html(html);

            $('.sidebarLink', elem).off('click.updateText').on('click.updateText', function () {

                var section = $('.sectionName', this)[0];
                var text = section ? section.innerHTML : this.innerHTML;

                $('.libraryMenuButtonText').html(text);

            });
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
        return $.browser.mobile || AppInfo.isNativeApp;
    }

    var requiresLibraryMenuRefresh = false;
    var requiresViewMenuRefresh = false;

    function onManageServerClicked() {

        closeMainDrawer();

        requirejs(["scripts/registrationservices"], function () {

            RegistrationServices.validateFeature('manageserver').done(function () {
                Dashboard.navigate('dashboard.html');

            });
        });
    }

    function setLibraryMenuText(text) {

        $('.libraryMenuButtonText').html('<span>' + text + '</span>');

    }

    function getTopParentId() {

        return getParameterByName('topParentId') || null;
    }

    window.LibraryMenu = {
        getTopParentId: getTopParentId,

        setText: setLibraryMenuText,

        onLinkClicked: function (link) {

            // There doesn't seem to be a way to detect if the drawer is in the process of opening, so try to handle that here
            if ((new Date().getTime() - lastOpenTime) > 200) {

                closeMainDrawer();

                setTimeout(function () {
                    Dashboard.navigate(link.href);
                }, 300);
            }

            return false;
        },

        onLogoutClicked: function () {
            // There doesn't seem to be a way to detect if the drawer is in the process of opening, so try to handle that here
            if ((new Date().getTime() - lastOpenTime) > 200) {

                closeMainDrawer();

                setTimeout(function () {
                    Dashboard.logout();
                }, 300);
            }

            return false;
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

        page = $(page);

        var isLiveTvPage = page.hasClass('liveTvPage');
        var isChannelsPage = page.hasClass('channelsPage');
        var isEditorPage = page.hasClass('metadataEditorPage');
        var isReportsPage = page.hasClass('reportsPage');
        var isMySyncPage = page.hasClass('mySyncPage');

        var id = isLiveTvPage || isChannelsPage || isEditorPage || isReportsPage || isMySyncPage || page.hasClass('allLibraryPage') ?
            '' :
            getTopParentId() || '';

        $('.lnkMediaFolder').each(function () {

            var itemId = this.getAttribute('data-itemid');

            if (isChannelsPage && itemId == 'channels') {
                $(this).addClass('selectedMediaFolder');
            }
            else if (isLiveTvPage && itemId == 'livetv') {
                $(this).addClass('selectedMediaFolder');
            }
            else if (isEditorPage && itemId == 'editor') {
                $(this).addClass('selectedMediaFolder');
            }
            else if (isReportsPage && itemId == 'reports') {
                $(this).addClass('selectedMediaFolder');
            }
            else if (isMySyncPage && itemId == 'mysync') {
                $(this).addClass('selectedMediaFolder');
            }
            else if (id && itemId == id) {
                $(this).addClass('selectedMediaFolder');
            }
            else {
                $(this).removeClass('selectedMediaFolder');
            }

        });

        var context = getParameterByName('context');

        if (context !== 'playlists') {
            $('.scopedLibraryViewNav a', page).each(function () {

                var src = this.href;

                if (src.indexOf('#') != -1) {
                    return;
                }

                src = replaceQueryString(src, 'topParentId', id);

                this.href = src;
            });
        }
    }

    function updateContextText(page) {

        var jPage = $(page);

        var name = jPage.attr('data-contextname');

        if (name) {

            $('.libraryMenuButtonText').html('<span>' + name + '</span>');

        }
        else if (jPage.hasClass('allLibraryPage') || jPage.hasClass('type-interior')) {
            $('.libraryMenuButtonText').html(Globalize.translate('ButtonHome'));
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

        if ($(page).hasClass('standalonePage')) {
            $('.viewMenuBar').visible(false);
            return;
        }

        if (requiresViewMenuRefresh) {
            $('.viewMenuBar').remove();
        }

        var viewMenuBar = $('.viewMenuBar').visible(true);
        if (!$('.viewMenuBar').length) {

            renderHeader();
            updateViewMenuBarHeadroom(page, $('.viewMenuBar'));

            updateCastIcon();

            updateLibraryNavLinks(page);
            updateContextText(page);
            requiresViewMenuRefresh = false;

            ConnectionManager.user(window.ApiClient).done(addUserToHeader);

        } else {
            updateContextText(page);
            updateLibraryNavLinks(page);
            updateViewMenuBarHeadroom(page, viewMenuBar);
            requiresViewMenuRefresh = false;
        }
    }

    // The first time we create the view menu bar, wait until doc ready + login validated
    // Otherwise we run into the jQM redirect back and forth problem
    var updateViewMenuBarBeforePageShow = false;

    $(document).on('pagebeforeshowready', ".page", function () {

        var page = this;

        requiresDashboardDrawerRefresh = true;

        if (updateViewMenuBarBeforePageShow) {
            onPageBeforeShowDocumentReady(page);
        }

    }).one('pageshowready', ".page", function () {

        var page = this;

        $(function () {
            onPageBeforeShowDocumentReady(page);
            updateViewMenuBarBeforePageShow = true;
        });

    }).on('pageshowready', ".page", function () {

        var page = this;

        onPageShowDocumentReady(page);

    }).on('pagebeforehide', ".page", function () {

        if (addNextToBackStack) {
            var text = $('.libraryMenuButtonText').text() || document.title;

            backStack.push(text);
        }

        addNextToBackStack = true;

        $('.headroomEnabled').addClass('headroomDisabled');
    });

    function onPageBeforeShowDocumentReady(page) {

        buildViewMenuBar(page);

        var jpage = $(page);

        var isLibraryPage = jpage.hasClass('libraryPage');
        var darkDrawer = false;

        if (isLibraryPage) {

            $(document.body).addClass('libraryDocument').removeClass('dashboardDocument').removeClass('hideMainDrawer');

            if (AppInfo.enableBottomTabs) {
                $(page).addClass('noSecondaryNavPage');

                $(function () {

                    $('.footer').addClass('footerOverBottomTabs');
                });

            } else {

                $('.libraryViewNav', page).each(function () {

                    initHeadRoom(this);
                });
            }

            if (!$.browser.mobile) {
                darkDrawer = true;
            }
        }
        else if (jpage.hasClass('type-interior')) {
            $(document.body).addClass('dashboardDocument').removeClass('libraryDocument').removeClass('hideMainDrawer');
        } else {
            $(document.body).removeClass('dashboardDocument').removeClass('libraryDocument').addClass('hideMainDrawer');
        }

        if (darkDrawer) {
            $('.mainDrawerPanel #drawer').addClass('darkDrawer');
        } else {
            $('.mainDrawerPanel #drawer').removeClass('darkDrawer');
        }

        if (AppInfo.enableBackButton) {
            updateBackButton(page);
        }
    }

    function updateBackButton(page) {

        var jPage = $(page);

        var canGoBack = backStack.length > 0 && jPage.is('.itemDetailPage');

        $('.headerBackButton').visible(canGoBack);

        jPage.off('swiperight', onPageSwipeLeft);

        if (canGoBack) {
            jPage.on('swiperight', onPageSwipeLeft);
        }
    }

    function onPageSwipeLeft(e) {

        var target = $(e.target);

        if (!target.is('.hiddenScrollX') && !target.parents('.hiddenScrollX').length) {
            history.back();
        }
    }

    function onPageShowDocumentReady(page) {
        var elem = $('.libraryViewNav .ui-btn-active:visible', page);

        if (elem.length) {
            elem[0].scrollIntoView();

            // Scroll back up so in case vertical scroll was messed with
            $(document).scrollTop(0);
        }
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
            $(elem).addClass('headroomEnabled');
        });
    }

    function initializeApiClient(apiClient) {

        requiresLibraryMenuRefresh = true;
        $(apiClient).off('websocketmessage.librarymenu', onWebSocketMessage).on('websocketmessage.librarymenu', onWebSocketMessage);
    }

    Dashboard.ready(function () {

        if (window.ApiClient) {
            initializeApiClient(window.ApiClient);
        }

        $(ConnectionManager).on('apiclientcreated', function (e, apiClient) {
            initializeApiClient(apiClient);

        }).on('localusersignedin localusersignedout', function () {
            requiresLibraryMenuRefresh = true;
            requiresViewMenuRefresh = true;
            requiresDrawerRefresh = true;
        });

        $(MediaController).on('playerchange', function () {
            updateCastIcon();
        });

        $('.mainDrawerPanel').on('paper-drawer-panel-open', onMainDrawerOpened).on('paper-drawer-panel-close', onMainDrawerClosed);
    });

})(window, document, jQuery, window.devicePixelRatio);

$.fn.createHoverTouch = function () {

    var preventHover = false;
    var timerId;

    function startTimer(elem) {

        stopTimer();

        timerId = setTimeout(function () {

            $(elem).trigger('hovertouch');
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
            $(this).trigger('hovertouch');
            stopTimer(this);
            preventHover = false;
        }
    });

};
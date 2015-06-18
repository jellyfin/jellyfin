(function (window, document, $, devicePixelRatio) {

    var backStack = [];
    var addNextToBackStack = true;

    function renderHeader() {

        var html = '<div class="viewMenuBar ui-bar-b">';

        if (AppInfo.enableBackButton) {
            html += '<button type="button" data-role="none" class="headerButton headerButtonLeft headerBackButton"><div class="fa fa-chevron-left"></div></button>';
        }

        html += '<button type="button" data-role="none" title="Menu" class="headerButton dashboardMenuButton barsMenuButton headerButtonLeft">';
        html += '<div class="barMenuInner fa fa-bars">';
        html += '</div>';
        html += '</button>';

        html += '<button type="button" data-role="none" title="Menu" class="headerButton libraryMenuButton barsMenuButton headerButtonLeft">';
        html += '<div class="barMenuInner fa fa-bars">';
        html += '</div>';
        html += '</button>';

        html += '<div class="libraryMenuButtonText headerButton">' + Globalize.translate('ButtonHome') + '</div>';

        html += '<div class="viewMenuSecondary">';

        html += '<button id="btnCast" class="btnCast btnDefaultCast headerButton headerButtonRight" type="button" data-role="none" style="display:none;"><div class="headerSelectedPlayer"></div><i class="material-icons btnCastImageDefault">cast</i><i class="material-icons btnCastImageActive">cast_connected</i></button>';

        html += '<button onclick="Search.showSearchPanel();" type="button" data-role="none" class="headerButton headerButtonRight headerSearchButton" style="display:none;"><i class="material-icons">search</i></button>';
        html += '<div class="viewMenuSearch hide">';
        html += '<form class="viewMenuSearchForm">';
        html += '<input type="text" data-role="none" data-type="search" class="headerSearchInput" autocomplete="off" spellcheck="off" />';
        html += '<div class="searchInputIcon fa fa-search"></div>';
        html += '<button data-role="none" type="button" data-iconpos="notext" class="imageButton btnCloseSearch"><i class="fa fa-close"></i></button>';
        html += '</form>';
        html += '</div>';

        html += '<button onclick="VoiceInputManager.startListening();" type="button" data-role="none" class="headerButton headerButtonRight headerVoiceButton" style="display:none;"><i class="material-icons">mic</i></button>';

        if (!showUserAtTop()) {
            html += '<button class="headerButton headerButtonRight headerUserButton" type="button" data-role="none" onclick="Dashboard.showUserFlyout(this);">';
            html += '<div class="fa fa-user"></div>';
            html += '</button>';
        }

        if (!$.browser.mobile && !AppInfo.isNativeApp) {
            html += '<a href="dashboard.html" class="headerButton headerButtonRight dashboardEntryHeaderButton" style="display:none;"><i class="material-icons">settings</i></a>';
            //html += '<a href="dashboard.html" class="headerButton headerButtonRight dashboardEntryHeaderButton clearLink" style="display:none;"><paper-icon-button icon="settings"></paper-icon-button></a>';
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
            $('.btnCast', header).show();
            $('.headerSearchButton', header).show();

            requirejs(['voice/voice'], function () {

                if (VoiceInputManager.isSupported()) {
                    $('.headerVoiceButton', header).show();
                } else {
                    $('.headerVoiceButton', header).hide();
                }

            });

        } else {
            $('.btnCast', header).hide();
            $('.headerVoiceButton', header).hide();
            $('.headerSearchButton', header).hide();
        }

        if (user.canManageServer) {
            $('.dashboardEntryHeaderButton', header).show();
        } else {
            $('.dashboardEntryHeaderButton', header).hide();
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

            $('.libraryMenuButton').on('click', showLibraryMenu);
            $('.dashboardMenuButton').on('click', showDashboardMenu);

        } else {
            $('.libraryMenuButton').createHoverTouch().on('hovertouch', showLibraryMenu);
            $('.dashboardMenuButton').createHoverTouch().on('hovertouch', showDashboardMenu);
        }

        $('.headerBackButton').on('click', onBackClick);

        // Have to wait for document ready here because otherwise 
        // we may see the jQM redirect back and forth problem
        $(initViewMenuBarHeadroom);

        //$('.headerButtonViewMenu').off('click', onViewButtonClick).on('click', onViewButtonClick);

        if (AppInfo.isNativeApp) {
            $(document).off('swiperight.drawer').on('swiperight.drawer', '.libraryPage', onSwipeRight);
        }
    }

    function onSwipeRight(event) {

        if (event.swipestop && event.swipestop.coords) {
            var x = event.swipestop.coords[0];

            if (x < 50) {
                showLibraryMenu();
            }
        }
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

    function getViewsHtml() {

        var html = '';

        html += '<div class="libraryMenuOptions">';
        html += '</div>';

        html += '<div class="adminMenuOptions">';
        html += '<div class="sidebarDivider"></div>';

        html += '<div class="sidebarHeader">';
        html += Globalize.translate('HeaderAdmin');
        html += '</div>';

        html += '<a class="sidebarLink lnkMediaFolder lnkManageServer" data-itemid="dashboard" href="#"><span class="fa fa-server sidebarLinkIcon"></span>' + Globalize.translate('ButtonManageServer') + '</a>';
        html += '<a class="sidebarLink lnkMediaFolder editorViewMenu" data-itemid="editor" href="edititemmetadata.html"><span class="fa fa-edit sidebarLinkIcon"></span>' + Globalize.translate('ButtonMetadataManager') + '</a>';

        if (!$.browser.mobile && !AppInfo.isTouchPreferred) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="reports" href="reports.html"><span class="fa fa-bar-chart sidebarLinkIcon"></span>' + Globalize.translate('ButtonReports') + '</a>';
        }
        html += '</div>';

        html += '<div class="userMenuOptions">';
        html += '<div class="sidebarDivider"></div>';

        html += '<a class="sidebarLink lnkMediaFolder" data-itemid="inbox" href="notificationlist.html"><span class="fa fa-inbox sidebarLinkIcon"></span>';
        html += Globalize.translate('ButtonInbox');
        html += '<div class="btnNotifications"><div class="btnNotificationsInner">0</div></div>';
        html += '</a>';

        html += '<a class="sidebarLink lnkMediaFolder syncViewMenu" data-itemid="mysync" href="mysync.html"><span class="fa fa-refresh sidebarLinkIcon" ></span>' + Globalize.translate('ButtonSync') + '</a>';

        if (Dashboard.isConnectMode()) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="selectserver" href="selectserver.html"><span class="fa fa-globe sidebarLinkIcon"></span>' + Globalize.translate('ButtonSelectServer') + '</a>';
        }

        if (showUserAtTop()) {
            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="logout" href="#" onclick="Dashboard.logout();"><span class="fa fa-lock sidebarLinkIcon"></span>' + Globalize.translate('ButtonSignOut') + '</a>';
        }

        html += '</div>';


        return html;
    }

    function showLibraryMenu() {

        var page = $.mobile.activePage;
        var panel;

        ConnectionManager.user(window.ApiClient).done(function (user) {

            panel = getLibraryMenu(user);
            updateLibraryNavLinks(page);

            panel = $(panel).panel('toggle').off('mouseleave.librarymenu');

            if (!AppInfo.isTouchPreferred) {
                panel.on('mouseleave.librarymenu', function () {

                    $(this).panel("close");

                });
            }
        });
    }

    function showDashboardMenu() {

        var page = $.mobile.activePage;
        var panel = getDashboardMenu(page);

        panel = $(panel).panel('toggle').off('mouseleave.librarymenu');

        if (!AppInfo.isTouchPreferred) {
            panel.on('mouseleave.librarymenu', function () {

                $(this).panel("close");

            });
        }
    }

    function updateLibraryMenu(panel) {

        var apiClient = window.ApiClient;

        if (!apiClient) {

            $('.adminMenuOptions').hide();
            $('.syncViewMenu').hide();
            $('.userMenuOptions').hide();
            return;
        }

        var userId = Dashboard.getCurrentUserId();

        apiClient.getUserViews(userId).done(function (result) {

            var items = result.Items;

            var html = '';
            html += '<div class="sidebarHeader">';
            html += Globalize.translate('HeaderMedia');
            html += '</div>';

            html += items.map(function (i) {

                var iconCssClass = 'fa';

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
                    iconCssClass += ' fa-photo';
                }
                else if (i.CollectionType == "music" || i.CollectionType == "musicvideos") {
                    iconCssClass += ' fa-music';
                }
                else if (i.CollectionType == "books") {
                    iconCssClass += ' fa-book';
                }
                else if (i.CollectionType == "playlists") {
                    iconCssClass += ' fa-list';
                }
                else if (i.CollectionType == "games") {
                    iconCssClass += ' fa-gamepad';
                }
                else if (i.CollectionType == "movies") {
                    iconCssClass += ' fa-film';
                }
                else if (i.CollectionType == "channels" || i.Type == 'Channel') {
                    iconCssClass += ' fa-globe';
                }
                else if (i.CollectionType == "tvshows" || i.CollectionType == "livetv") {
                    iconCssClass += ' fa-video-camera';
                }
                else {
                    iconCssClass += ' fa-folder-open-o';
                }

                return '<a data-itemid="' + itemId + '" class="lnkMediaFolder sidebarLink" href="' + getItemHref(i, i.CollectionType) + '"><span class="' + iconCssClass + ' sidebarLinkIcon"></span><span class="sectionName">' + i.Name + '</span></a>';

            }).join('');

            var elem = $('.libraryMenuOptions').html(html);

            $('.sidebarLink', elem).on('click', function () {

                var section = $('.sectionName', this)[0];
                var text = section ? section.innerHTML : this.innerHTML;

                $('.libraryMenuButtonText').html(text);

            });
        });

        Dashboard.getCurrentUser().done(function (user) {

            if (user.Policy.IsAdministrator) {
                $('.adminMenuOptions').show();
            } else {
                $('.adminMenuOptions').hide();
            }

            if (user.Policy.EnableSync) {
                $('.syncViewMenu').show();
            } else {
                $('.syncViewMenu').hide();
            }
        });
    }

    function showUserAtTop() {

        return $.browser.mobile || AppInfo.isNativeApp;
    }

    var requiresLibraryMenuRefresh = false;
    var requiresViewMenuRefresh = false;

    function getLibraryMenu(user) {

        var panel = $('#libraryPanel');

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="libraryPanel" class="libraryPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<div class="sidebarLinks librarySidebarLinks">';

            var userAtTop = showUserAtTop();

            var homeHref = window.ApiClient ? 'index.html' : 'selectserver.html';

            var userHref = user.localUser && user.localUser.Policy.EnableUserPreferenceAccess ?
                'mypreferencesdisplay.html?userId=' + user.localUser.Id :
                (user.localUser ? ('mypreferenceswebclient.html?userId=' + user.localUser.Id) : '#');

            var hasUserImage = user.imageUrl && AppInfo.enableUserImage;
            if (userAtTop) {
                var paddingLeft = hasUserImage ? 'padding-left:.7em;' : '';
                html += '<a style="margin-top:0;' + paddingLeft + 'display:block;color:#fff;text-decoration:none;font-size:16px;font-weight:400!important;background: #111;" href="' + userHref + '">';

                var imgWidth = 44;

                if (hasUserImage) {
                    var url = user.imageUrl;

                    if (user.supportsImageParams) {
                        url += "&width=" + (imgWidth * Math.max(devicePixelRatio || 1, 2));
                    }

                    html += '<div class="lazy" data-src="' + url + '" style="width:' + imgWidth + 'px;height:' + imgWidth + 'px;background-size:contain;background-repeat:no-repeat;background-position:center center;border-radius:1000px;vertical-align:middle;margin-right:.8em;display:inline-block;"></div>';
                } else {
                    html += '<span class="fa fa-user sidebarLinkIcon"></span>';
                }

                html += user.name;
                html += '</a>';

                html += '<div class="sidebarDivider" style="margin-top:0;"></div>';

                html += '<a class="lnkMediaFolder sidebarLink" href="' + homeHref + '"><span class="fa fa-home sidebarLinkIcon"></span><span>' + Globalize.translate('ButtonHome') + '</span></a>';
            } else {
                html += '<div style="margin-top:5px;"></div>';

                html += '<a class="lnkMediaFolder sidebarLink" href="' + homeHref + '">';
                html += '<div class="lazy" data-src="css/images/mblogoicon.png" style="width:' + 28 + 'px;height:' + 28 + 'px;background-size:contain;background-repeat:no-repeat;background-position:center center;border-radius:1000px;vertical-align:middle;margin:0 1.4em 0 1.3em;display:inline-block;"></div>';
                html += Globalize.translate('ButtonHome');
                html += '</a>';
            }

            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="remote" href="nowplaying.html"><span class="fa fa-tablet sidebarLinkIcon"></span>' + Globalize.translate('ButtonRemote') + '</a>';

            html += '<div class="sidebarDivider"></div>';

            html += getViewsHtml();
            html += '</div>';

            html += '</div>';

            $(document.body).append(html).trigger('libraryMenuCreated');

            panel = $('#libraryPanel').panel({}).lazyChildren().trigger('create');

            $('.lnkManageServer', panel).on('click', onManageServerClicked);

            updateLibraryMenu();
        }
        else if (requiresLibraryMenuRefresh) {
            updateLibraryMenu();
            requiresLibraryMenuRefresh = false;
        }

        return panel;
    }

    function onManageServerClicked() {

        requirejs(["scripts/registrationservices"], function () {

            RegistrationServices.validateFeature('manageserver').done(function () {
                Dashboard.navigate('dashboard.html');

            });
        });
    }

    function getDashboardMenu(page) {

        var panel = $('#dashboardPanel', page);

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="dashboardPanel" class="dashboardPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<div style="margin: 0 -1em;">';

            html += '</div>';

            html += '</div>';

            $(document.body).append(html);
            panel = $('#dashboardPanel').panel({}).trigger('create');
        }

        return panel;
    }

    function setLibraryMenuText(text) {

        $('.libraryMenuButtonText').html('<span>' + text + '</span>');

    }

    function getTopParentId() {

        return getParameterByName('topParentId') || null;
    }

    window.LibraryMenu = {
        showLibraryMenu: showLibraryMenu,

        getTopParentId: getTopParentId,

        setText: setLibraryMenuText
    };

    function updateCastIcon() {

        var info = MediaController.getPlayerInfo();

        if (info.isLocalPlayer) {

            $('.btnCast').addClass('btnDefaultCast').removeClass('btnActiveCast');
            $('.headerSelectedPlayer').html('');

        } else {

            $('.btnCast').removeClass('btnDefaultCast').addClass('btnActiveCast');

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
            $('.viewMenuBar').hide();
            return;
        }

        if (requiresViewMenuRefresh) {
            $('.viewMenuBar').remove();
        }

        var viewMenuBar = $('.viewMenuBar').show();
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

    }).on('pagehide', ".page", function () {

        if (addNextToBackStack) {
            var text = $('.libraryMenuButtonText').text() || document.title;

            backStack.push(text);
        }

        addNextToBackStack = true;

    }).on('pagebeforehide', ".page", function () {

        $('.headroomEnabled').addClass('headroomDisabled');
    });

    function onPageBeforeShowDocumentReady(page) {

        buildViewMenuBar(page);

        var jpage = $(page);

        var isLibraryPage = jpage.hasClass('libraryPage');

        if (isLibraryPage) {
            $(document.body).addClass('libraryDocument').removeClass('dashboardDocument');

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
        }
        else if (jpage.hasClass('type-interior')) {
            $(document.body).addClass('dashboardDocument').removeClass('libraryDocument');
        } else {
            $(document.body).removeClass('dashboardDocument').removeClass('libraryDocument');
        }

        if (AppInfo.enableBackButton)
        {
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
            var headroom = new Headroom(elem);
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
        });
    });

    $(function () {

        $(MediaController).on('playerchange', function () {
            updateCastIcon();
        });

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
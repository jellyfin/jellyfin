(function (window, document, $, devicePixelRatio) {

    function renderHeader(user) {

        var html = '<div class="viewMenuBar ui-bar-b">';

        if (($.browser.safari && window.navigator.standalone) || Dashboard.isRunningInCordova()) {
            html += '<button type="button" data-role="none" onclick="history.back();" class="headerButton headerButtonLeft headerBackButton"><div class="fa fa-arrow-left"></div></button>';
        }

        html += '<button type="button" data-role="none" title="Menu" class="headerButton dashboardMenuButton barsMenuButton headerButtonLeft">';
        html += '<div class="barMenuInner fa fa-bars">';
        html += '</div>';
        html += '</button>';

        html += '<button type="button" data-role="none" title="Menu" class="headerButton libraryMenuButton barsMenuButton headerButtonLeft">';
        html += '<div class="barMenuInner fa fa-bars">';
        html += '</div>';
        html += '</button>';

        html += '<div class="libraryMenuButtonText headerButton"><span>EMBY</span></div>';

        html += '<div class="viewMenuSecondary">';

        var btnCastVisible = user.localUser ? '' : 'visibility:hidden;';

        if (!AppInfo.enableHeaderImages) {
            html += '<button id="btnCast" class="btnCast btnCastIcon btnDefaultCast headerButton headerButtonRight" type="button" data-role="none" style="' + btnCastVisible + '">';
            html += '<div class="headerSelectedPlayer"></div><i class="fa fa-wifi"></i>';
            html += '</button>';
        } else {
            html += '<button id="btnCast" class="btnCast btnDefaultCast headerButton headerButtonRight" type="button" data-role="none" style="' + btnCastVisible + '"><div class="headerSelectedPlayer"></div><div class="btnCastImage"></div></button>';
        }

        if (user.localUser) {
            html += '<button onclick="Search.showSearchPanel($.mobile.activePage);" type="button" data-role="none" class="headerButton headerButtonRight headerSearchButton"><div class="fa fa-search" style="font-size:21px;"></div></button>';

            html += '<div class="viewMenuSearch hide"><form class="viewMenuSearchForm">';
            html += '<input type="text" data-role="none" data-type="search" class="headerSearchInput" autocomplete="off" spellcheck="off" />';
            html += '<div class="searchInputIcon fa fa-search"></div>';
            html += '<button data-role="none" type="button" data-iconpos="notext" class="imageButton btnCloseSearch"><i class="fa fa-close"></i></button>';
            html += '</form></div>';
        }

        if (user.name) {

            html += '<button class="headerButton headerButtonRight headerUserButton" type="button" data-role="none" onclick="Dashboard.showUserFlyout(this);">';

            if (user.imageUrl && AppInfo.enableUserImage) {

                var userButtonHeight = 26;

                var url = user.imageUrl;

                if (user.supportsImageParams) {
                    url += "&height=" + (userButtonHeight * Math.max(devicePixelRatio || 1, 2));
                }

                html += '<div class="lazy headerUserImage" data-src="' + url + '" style="width:' + userButtonHeight + 'px;height:' + userButtonHeight + 'px;"></div>';
            } else {
                html += '<div class="fa fa-user"></div>';
            }

            html += '</button>';
        }

        if (user.canManageServer) {
            html += '<a href="dashboard.html" class="headerButton headerButtonRight dashboardEntryHeaderButton"><div class="fa fa-cog"></div></a>';
        }

        html += '</div>';

        html += '</div>';

        $(document.body).prepend(html);
        $('.viewMenuBar').trigger('create').lazyChildren();

        $(document).trigger('headercreated');
        bindMenuEvents();
    }

    function bindMenuEvents() {

        if (AppInfo.isTouchPreferred) {

            $('.libraryMenuButton').on('click', function () {
                showLibraryMenu(false);
            });
            $('.dashboardMenuButton').on('click', function () {
                showDashboardMenu(false);
            });

        } else {
            $('.libraryMenuButton').createHoverTouch().on('hovertouch', showLibraryMenu);
            $('.dashboardMenuButton').createHoverTouch().on('hovertouch', showDashboardMenu);
        }

        // grab an element
        var viewMenuBar = document.getElementsByClassName("viewMenuBar")[0];
        initHeadRoom(viewMenuBar);
    }

    function updateViewMenuBarHeadroom(page, viewMenuBar) {

        if ($(page).hasClass('libraryPage')) {
            viewMenuBar.removeClass('headroomDisabled');
        } else {
            viewMenuBar.addClass('headroomDisabled');
        }
    }

    function getItemHref(item, context) {

        return LibraryBrowser.getHref(item, context);
    }

    function getViewsHtml() {

        var html = '';

        html += '<div class="libraryMenuOptions">';
        html += '</div>';

        html += '<div class="libraryMenuDivider"></div>';
        html += '<div class="adminMenuOptions">';

        html += '<a class="sidebarLink lnkMediaFolder" data-itemid="dashboard" data-rel="none" href="dashboard.html"><span class="fa fa-cog sidebarLinkIcon"></span>' + Globalize.translate('ButtonDashboard') + '</a>';
        html += '<a class="sidebarLink lnkMediaFolder editorViewMenu" data-itemid="editor" href="edititemmetadata.html"><span class="fa fa-edit sidebarLinkIcon"></span>' + Globalize.translate('ButtonMetadataManager') + '</a>';
        html += '<a class="sidebarLink lnkMediaFolder" data-itemid="reports" href="reports.html"><span class="fa fa-bar-chart sidebarLinkIcon"></span>' + Globalize.translate('ButtonReports') + '</a>';
        html += '</div>';
        html += '<a class="sidebarLink lnkMediaFolder syncViewMenu" data-itemid="mysync" href="mysync.html"><span class="fa fa-cloud sidebarLinkIcon"></span>' + Globalize.translate('ButtonSync') + '</a>';

        return html;
    }

    function showLibraryMenu() {

        var page = $.mobile.activePage;
        var panel;

        ConnectionManager.user(window.ApiClient).done(function (user) {

            panel = getLibraryMenu(user);
            updateLibraryNavLinks(page);

            $(panel).panel('toggle').off('mouseleave.librarymenu').on('mouseleave.librarymenu', function () {

                $(this).panel("close");

            });
        });
    }

    function showDashboardMenu() {

        var page = $.mobile.activePage;
        var panel = getDashboardMenu(page);

        $(panel).panel('toggle').off('mouseleave.librarymenu').on('mouseleave.librarymenu', function () {

            $(this).panel("close");

        });
    }

    function updateLibraryMenu(panel) {

        var apiClient = window.ApiClient;

        if (!apiClient) {

            $('.adminMenuOptions').hide();
            $('.syncViewMenu').hide();
            return;
        }

        var userId = Dashboard.getCurrentUserId();

        apiClient.getUserViews(userId).done(function (result) {

            var items = result.Items;

            var html = items.map(function (i) {

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

    var requiresLibraryMenuRefresh = false;
    var requiresViewMenuRefresh = false;

    function getLibraryMenu(user) {

        var panel = $('#libraryPanel');

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="libraryPanel" class="libraryPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<div class="sidebarLinks librarySidebarLinks">';

            var showUserAtTop = AppInfo.isTouchPreferred;

            if (showUserAtTop) {

                var userHref = user.localUser && user.localUser.Policy.EnableUserPreferenceAccess ?
                    'mypreferencesdisplay.html?userId=' + user.localUser.Id :
                    (user.localUser ? 'index.html' : '#');

                var hasUserImage = user.imageUrl && AppInfo.enableUserImage;
                var paddingLeft = hasUserImage ? 'padding-left:.7em;' : '';
                html += '<a style="margin-top:0;' + paddingLeft + 'display:block;color:#fff;text-decoration:none;font-size:16px;font-weight:400!important;background: #000;" href="' + userHref + '">';

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

                html += '<div class="libraryMenuDivider" style="margin-top:0;"></div>';
            }

            var homeHref = window.ApiClient ? 'index.html' : 'selectserver.html';

            if (showUserAtTop) {
                html += '<a class="lnkMediaFolder sidebarLink" href="' + homeHref + '"><span class="fa fa-home sidebarLinkIcon"></span><span>' + Globalize.translate('ButtonHome') + '</span></a>';

            } else {
                html += '<a class="lnkMediaFolder sidebarLink" style="margin-top:.5em;padding-left:1em;display:block;color:#fff;text-decoration:none;" href="' + homeHref + '">';

                html += '<img style="max-width:36px;vertical-align:middle;margin-right:1em;" src="css/images/mblogoicon.png" />';

                html += Globalize.translate('ButtonHome');
                html += '</a>';
            }

            html += '<a class="sidebarLink lnkMediaFolder" data-itemid="dashboard" data-rel="none" href="nowplaying.html"><span class="fa fa-tablet sidebarLinkIcon"></span>' + Globalize.translate('ButtonRemote') + '</a>';

            html += '<div class="libraryMenuDivider"></div>';

            html += getViewsHtml();
            html += '</div>';

            html += '</div>';

            $(document.body).append(html);

            panel = $('#libraryPanel').panel({}).lazyChildren().trigger('create');

            updateLibraryMenu();
        }
        else if (requiresLibraryMenuRefresh) {
            updateLibraryMenu();
            requiresLibraryMenuRefresh = false;
        }

        return panel;
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

        return getParameterByName('topParentId') /*|| sessionStore.getItem('topParentId')*/ || null;
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

        var name = $(page)[0].getAttribute('data-contextname');

        if (name) {

            $('.libraryMenuButtonText').html('<span>' + name + '</span>');

        }
            //else if ($(page).hasClass('type-interior')) {

            //    $('.libraryMenuButtonText').html('<span>' + 'Dashboard' + '</span>');

            //}
        else if ($(page).hasClass('allLibraryPage') || $(page).hasClass('type-interior')) {
            $('.libraryMenuButtonText').html('<span class="logoLibraryMenuButtonText">EMBY</span>');
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
            $('.viewMenuBar').remove();
            return;
        }

        if (requiresViewMenuRefresh) {
            $('.viewMenuBar').remove();
        }

        var viewMenuBar = $('.viewMenuBar');
        if (!$('.viewMenuBar').length) {

            ConnectionManager.user(window.ApiClient).done(function (user) {

                renderHeader(user);
                updateViewMenuBarHeadroom(page, $('.viewMenuBar'));

                updateCastIcon();

                updateLibraryNavLinks(page);
                updateContextText(page);
                requiresViewMenuRefresh = false;
            });
        } else {
            updateContextText(page);
            updateLibraryNavLinks(page);
            updateViewMenuBarHeadroom(page, viewMenuBar);
            requiresViewMenuRefresh = false;
        }

    }

    $(document).on('pageinit', ".page", function () {

        var page = this;

        $('.libraryViewNav', page).wrapInner('<div class="libraryViewNavInner"></div>');

        $('.libraryViewNav a', page).each(function () {

            this.innerHTML = '<span class="libraryViewNavLinkContent">' + this.innerHTML + '</span>';

        });

    }).on('pagebeforeshowready', ".page", function () {

        var page = this;
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

    }).on('pageshow', ".libraryPage", function () {

        var page = this;

        var elem = $('.libraryViewNavInner .ui-btn-active:visible', page);

        if (elem.length) {
            elem[0].scrollIntoView();

            // Scroll back up so in case vertical scroll was messed with
            $(document).scrollTop(0);
        }
    });

    function initHeadRoom(elem) {

        if (!AppInfo.enableHeadRoom) {
            return;
        }

        requirejs(["thirdparty/headroom"], function () {

            // construct an instance of Headroom, passing the element
            var headroom = new Headroom(elem);
            // initialise
            headroom.init();
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
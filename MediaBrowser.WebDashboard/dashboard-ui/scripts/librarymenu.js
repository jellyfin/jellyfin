(function (window, document, $) {

    function renderHeader(user) {

        var html = '<div class="viewMenuBar ui-bar-b">';

        html += '<button type="button" data-role="none" title="Menu" class="headerButton dashboardMenuButton barsMenuButton headerButtonLeft">';
        html += '<div class="barMenuInner fa fa-bars">';
        html += '</div>';
        html += '</button>';

        html += '<button type="button" data-role="none" title="Menu" class="headerButton libraryMenuButton barsMenuButton headerButtonLeft">';
        html += '<div class="barMenuInner fa fa-bars">';
        html += '</div>';
        html += '</button>';

        html += '<div class="libraryMenuButtonText headerButton"><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></div>';

        html += '<div class="viewMenuSecondary">';

        if (user.localUser) {

            html += '<button id="btnCast" class="btnCast btnDefaultCast headerButton headerButtonRight" type="button" data-role="none"><div class="headerSelectedPlayer"></div><div class="btnCastImage"></div></button>';

            html += '<button onclick="Search.showSearchPanel($.mobile.activePage);" type="button" data-role="none" class="headerButton headerButtonRight headerSearchButton"><div class="fa fa-search" style="font-size:21px;"></div></button>';
        } else {
            html += '<button id="btnCast" class="btnCast btnDefaultCast headerButton headerButtonRight" type="button" data-role="none" style="visibility:hidden;"><div class="headerSelectedPlayer"></div><div class="btnCastImage"></div></button>';

        }

        html += '<a class="headerButton headerButtonRight headerUserButton" href="#" onclick="Dashboard.showUserFlyout(this);">';

        if (user.imageUrl) {

            var userButtonHeight = 24;

            var url = user.imageUrl;

            if (user.supportsImageParams) {
                url += "height=" + userButtonHeight;
            }

            html += '<img src="' + url + '" style="border-radius: 1000px; height:' + userButtonHeight + 'px;" />';
        } else {
            html += '<div class="fa fa-user"></div>';
        }

        html += '</a>';

        if (user.canManageServer) {
            html += '<a href="dashboard.html" class="headerButton headerButtonRight"><div class="fa fa-cog"></div></a>';
        }

        html += '</div>';

        html += '</div>';

        $(document.body).prepend(html);
        $('.viewMenuBar').trigger('create');

        $(document).trigger('headercreated');

        $('.libraryMenuButton').createHoverTouch().on('hovertouch', showLibraryMenu);
        $('.dashboardMenuButton').createHoverTouch().on('hovertouch', showDashboardMenu);
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

        html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder editorViewMenu iconViewMenu" data-itemid="editor" href="edititemmetadata.html"><span class="fa fa-edit"></span>' + Globalize.translate('ButtonMetadataManager') + '</a>';
        html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder iconViewMenu" data-itemid="reports" href="reports.html"><span class="fa fa-bar-chart"></span>' + Globalize.translate('ButtonReports') + '</a>';
        html += '</div>';
        //html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder iconViewMenu syncViewMenu" data-itemid="mysync" href="mysync.html"><span class="fa fa-cloud"></span>' + Globalize.translate('ButtonSync') + '</a>';

        return html;
    }

    function showLibraryMenu() {

        var page = $.mobile.activePage;
        var panel;

        panel = getLibraryMenu();
        updateLibraryNavLinks(page);

        $(panel).panel('toggle').off('mouseleave.librarymenu').on('mouseleave.librarymenu', function () {

            $(this).panel("close");

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

        var apiClient = ConnectionManager.currentApiClient();

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

                return '<a data-itemid="' + itemId + '" class="lnkMediaFolder viewMenuLink viewMenuTextLink iconViewMenu" href="' + getItemHref(i, i.CollectionType) + '"><span class="' + iconCssClass + '"></span><span class="sectionName">' + i.Name + '</span></a>';

            }).join('');

            var elem = $('.libraryMenuOptions').html(html);

            $('.viewMenuTextLink', elem).on('click', function () {

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

    function getLibraryMenu(user) {

        var panel = $('#libraryPanel');

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="libraryPanel" class="libraryPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<div style="margin: 0 -1em;">';

            var homeHref = ConnectionManager.currentApiClient() ? 'index.html' : 'selectserver.html';

            html += '<a class="lnkMediaFolder viewMenuLink viewMenuTextLink homeViewMenu" href="' + homeHref + '">' + Globalize.translate('ButtonHome') + '</a>';

            html += '<div class="libraryMenuDivider"></div>';

            html += getViewsHtml();
            html += '</div>';

            html += '</div>';

            $(document.body).append(html);

            panel = $('#libraryPanel').panel({}).trigger('create');

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

        var name = page.getAttribute('data-contextname');

        if (name) {

            $('.libraryMenuButtonText').html('<span>' + name + '</span>');

        }
            //else if ($(page).hasClass('type-interior')) {

            //    $('.libraryMenuButtonText').html('<span>' + 'Dashboard' + '</span>');

            //}
        else if ($(page).hasClass('allLibraryPage') || $(page).hasClass('type-interior')) {
            $('.libraryMenuButtonText').html('<span class="logoLibraryMenuButtonText">MEDIA</span><span class="logoLibraryMenuButtonText mediaBrowserAccent">BROWSER</span>');
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

    $(document).on('pageinit', ".page", function () {

        var page = this;

        $('.libraryViewNav', page).wrapInner('<div class="libraryViewNavInner"></div>');

        $('.libraryViewNav a', page).each(function () {

            this.innerHTML = '<span class="libraryViewNavLinkContent">' + this.innerHTML + '</span>';

        });

    }).on('pagebeforeshow', ".page:not(.standalonePage)", function () {

        var page = this;
        if (!$('.viewMenuBar').length) {

            ConnectionManager.user().done(function (user) {

                renderHeader(user);

                updateCastIcon();

                updateLibraryNavLinks(page);
                updateContextText(page);
            });
        } else {
            updateContextText(page);
            updateLibraryNavLinks(page);
        }

        var jpage = $(page);

        if (jpage.hasClass('libraryPage')) {
            $(document.body).addClass('libraryDocument').removeClass('dashboardDocument');
        }
        else if (jpage.hasClass('type-interior')) {
            $(document.body).addClass('dashboardDocument').removeClass('libraryDocument');
        } else {
            $(document.body).removeClass('dashboardDocument').removeClass('libraryDocument');
        }

    }).on('pagebeforeshow', ".page", function () {

        var page = this;

        if ($(page).hasClass('standalonePage')) {
            $('.viewMenuBar').hide();
        } else {
            $('.viewMenuBar').show();
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

    function initializeApiClient(apiClient) {

        $(apiClient).off('websocketmessage.librarymenu', onWebSocketMessage).on('websocketmessage.librarymenu', onWebSocketMessage);
    }

    $(ConnectionManager).on('apiclientcreated', function (e, apiClient) {

        initializeApiClient(apiClient);
    });

    $(function () {

        $(MediaController).on('playerchange', function () {
            updateCastIcon();
        });

    });

})(window, document, jQuery);

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
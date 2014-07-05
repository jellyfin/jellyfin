(function (window, document, $) {

    function renderHeader(user) {

        var html = '<div class="viewMenuBar ui-bar-b">';

        //html += '<a href="index.html" class="headerButton headerButtonLeft headerHomeButton">';
        //html += '<img src="css/images/items/folders/home.png" />';
        //html += '</a>';

        html += '<button type="button" data-role="none" title="Menu" class="headerButton dashboardMenuButton barsMenuButton headerButtonLeft">';
        html += '<img src="css/images/menu.png" />';
        html += '</button>';

        html += '<button type="button" data-role="none" title="Menu" class="headerButton libraryMenuButton barsMenuButton headerButtonLeft">';
        html += '<img src="css/images/menu.png" />';
        html += '</button>';
        
        html += '<div class="libraryMenuButtonText headerButton"><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></div>';

        html += '<div class="viewMenuSecondary">';

        html += '<button id="btnCast" class="btnCast btnDefaultCast headerButton headerButtonRight" type="button" data-role="none"><div class="headerSelectedPlayer"></div><div class="btnCastImage"></div></button>';

        html += '<button onclick="Search.showSearchPanel($.mobile.activePage);" type="button" data-role="none" class="headerButton headerButtonRight headerSearchButton"><img src="css/images/headersearch.png" /></button>';

        html += '<a class="headerButton headerButtonRight headerUserButton" href="#" onclick="Dashboard.showUserFlyout(this);">';

        var userButtonHeight = 21;
        if (user.PrimaryImageTag) {

            var url = ApiClient.getUserImageUrl(user.Id, {
                height: userButtonHeight,
                tag: user.PrimaryImageTag,
                type: "Primary"
            });

            html += '<img src="' + url + '" style="height:' + userButtonHeight + 'px;" />';
        } else {
            html += '<img src="css/images/currentuserdefaultwhite.png" style="height:' + userButtonHeight + 'px;" />';
        }

        html += '</a>';

        if (user.Configuration.IsAdministrator) {
            html += '<a href="dashboard.html" class="headerButton headerButtonRight"><img src="css/images/items/folders/settings.png" /></a>';
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

        html += '<div class="adminMenuOptions">';
        html += '<div class="libraryMenuDivider"></div>';
        //html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder dashboardViewMenu" data-itemid="dashboard" href="dashboard.html">'+Globalize.translate('ButtonDashboard')+'</a>';
        html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder editorViewMenu" data-itemid="editor" href="edititemmetadata.html">' + Globalize.translate('ButtonMetadataManager') + '</a>';
        html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder reportsViewMenu" data-itemid="reports" href="reports.html">' + Globalize.translate('ButtonReports') + '</a>';
        html += '</div>';

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

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getUserViews(userId).done(function (result) {

            var items = result.Items;

            var html = items.map(function (i) {

                var viewMenuCssClass = (i.CollectionType || 'general') + 'ViewMenu';

                var itemId = i.Id;

                if (i.CollectionType == "channels") {
                    itemId = "channels";
                }
                else if (i.CollectionType == "livetv") {
                    itemId = "livetv";
                }
                
                if (i.Type == 'Channel') {
                    viewMenuCssClass = 'channelsViewMenu';
                }

                return '<a data-itemid="' + itemId + '" class="lnkMediaFolder viewMenuLink viewMenuTextLink ' + viewMenuCssClass + '" href="' + getItemHref(i) + '">' + i.Name + '</a>';

            }).join('');

            var elem = $('.libraryMenuOptions').html(html);

            $('.viewMenuTextLink', elem).on('click', function () {

                $('.libraryMenuButtonText').html(this.innerHTML);

            });
        });

        Dashboard.getCurrentUser().done(function (user) {

            if (user.Configuration.IsAdministrator) {
                $('.adminMenuOptions').show();
            } else {
                $('.adminMenuOptions').hide();
            }
        });
    }

    var requiresLibraryMenuRefresh = false;

    function getLibraryMenu() {

        var panel = $('#libraryPanel');

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="libraryPanel" class="libraryPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<div style="margin: 0 -1em;">';

            html += '<a class="lnkMediaFolder viewMenuLink viewMenuTextLink homeViewMenu" href="index.html">' + Globalize.translate('ButtonHome') + '</a>';
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

        return getParameterByName('topParentId') || sessionStorage.getItem('topParentId') || null;
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
            $('.headerSelectedPlayer').html(info.deviceName || info.name);
        }
    }

    function updateLibraryNavLinks(page) {

        page = $(page);

        var isLiveTvPage = page.hasClass('liveTvPage');
        var isChannelsPage = page.hasClass('channelsPage');
        var isEditorPage = page.hasClass('metadataEditorPage');
        var isReportsPage = page.hasClass('reportsPage');

        var id = isLiveTvPage || isChannelsPage || isEditorPage || isReportsPage || page.hasClass('allLibraryPage') ?
            '' :
            getTopParentId() || '';

        sessionStorage.setItem('topParentId', id);

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
            else if (id && itemId == id) {
                $(this).addClass('selectedMediaFolder');
            }
            else {
                $(this).removeClass('selectedMediaFolder');
            }

        });

        $('.scopedLibraryViewNav a', page).each(function () {

            var src = this.href;

            if (src.indexOf('#') != -1) {
                return;
            }

            src = replaceQueryString(src, 'topParentId', id);

            this.href = src;
        });
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
            $('.libraryMenuButtonText').html('<span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span>');
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

            Dashboard.getCurrentUser().done(function (user) {

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

    $(function () {

        $(MediaController).on('playerchange', function () {
            updateCastIcon();
        });

        $(ApiClient).on('websocketmessage', onWebSocketMessage);
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
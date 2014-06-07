(function (window, document, $) {

    function renderHeader(user) {

        var html = '<div class="viewMenuBar ui-bar-b">';

        //html += '<a href="index.html" class="headerButton headerButtonLeft headerHomeButton">';
        //html += '<img src="css/images/items/folders/home.png" />';
        //html += '</a>';
        html += '<button type="button" data-role="none" title="Menu" class="headerButton libraryMenuButton headerButtonLeft">';
        html += '<img src="css/images/menu.png" />';
        html += '</button>';
        html += '<div class="libraryMenuButtonText headerButton"><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></div>';

        html += '<div class="viewMenuSecondary">';

        html += '<a href="nowplaying.html" class="headerButton headerButtonRight headerRemoteButton"><img src="css/images/remote.png" /></a>';

        html += '<button id="btnCast" class="btnCast btnDefaultCast headerButton headerButtonRight" type="button" data-role="none"><div class="btnCastImage"></div></button>';

        html += '<button onclick="Search.showSearchPanel($.mobile.activePage);" type="button" data-role="none" class="headerButton headerButtonRight headerSearchButton"><img src="css/images/headersearch.png" /></button>';

        if (user.Configuration.IsAdministrator) {
            html += '<a href="dashboard.html" class="headerButton headerButtonRight headerSettingsButton"><img src="css/images/items/folders/settings.png" /></a>';
        }

        html += '<a class="headerButton headerButtonRight" href="#" onclick="Dashboard.showUserFlyout(this);">';

        if (user.PrimaryImageTag) {

            var url = ApiClient.getUserImageUrl(user.Id, {
                height: 18,
                tag: user.PrimaryImageTag,
                type: "Primary"
            });

            html += '<img src="' + url + '" />';
        } else {
            html += '<img src="css/images/currentuserdefaultwhite.png" />';
        }

        html += '</a>';

        html += '</div>';

        html += '</div>';

        $(document.body).prepend(html);

        $(document).trigger('headercreated');

        $('.libraryMenuButton').createHoverTouch().on('hovertouch', showLibraryMenu);
    }

    function getItemHref(item) {

        return LibraryBrowser.getHref(item);
    }

    function getViewsHtml() {

        var html = '';

        html += '<div class="libraryMenuOptions">';
        html += '</div>';

        html += '<div class="adminMenuOptions">';
        html += '<div class="libraryMenuDivider"></div>';
        //html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder dashboardViewMenu" data-itemid="dashboard" href="dashboard.html">Dashboard</a>';
        html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder editorViewMenu" data-itemid="editor" href="edititemmetadata.html">Metadata Manager</a>';
        html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder reportsViewMenu" data-itemid="reports" href="reports.html">Reports</a>';
        html += '</div>';

        return html;
    }

    function showLibraryMenu() {

        var panel = getLibraryMenu();

        updateLibraryNavLinks($.mobile.activePage);

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

    function getLibraryMenu(user, channelCount, items, liveTvInfo) {

        var panel = $('#libraryPanel');

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="libraryPanel" class="libraryPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<div style="margin: 0 -1em;">';

            html += '<a class="lnkMediaFolder viewMenuLink viewMenuTextLink homeViewMenu" href="index.html">Home</a>';
            html += '<div class="libraryMenuDivider"></div>';

            html += getViewsHtml(user, channelCount, items, liveTvInfo);
            html += '</div>';

            html += '</div>';

            $(document.body).append(html);

            panel = $('#libraryPanel').panel({}).trigger('create');

            updateLibraryMenu();
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

        } else {

            $('.btnCast').removeClass('btnDefaultCast').addClass('btnActiveCast');
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
        else if ($(page).hasClass('allLibraryPage')) {
            $('.libraryMenuButtonText').html('<span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span>');
        }
    }

    $(document).on('pageinit', ".libraryPage", function () {

        var page = this;

        $('.libraryViewNav', page).wrapInner('<div class="libraryViewNavInner"></div>');

        $('.libraryViewNav a', page).each(function () {

            this.innerHTML = '<span class="libraryViewNavLinkContent">' + this.innerHTML + '</span>';

        });

    }).on('pagebeforeshow', ".libraryPage", function () {

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

    }).on('pagebeforeshow', ".page", function () {

        var page = this;

        if ($(page).hasClass('libraryPage')) {
            $('.viewMenuBar').show();
        } else {
            $('.viewMenuBar').hide();
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

    });

})(window, document, jQuery);

$.fn.createHoverTouch = function () {

    var preventHover = false;
    var timerId;

    function startTimer(elem) {

        stopTimer();

        timerId = setTimeout(function () {

            $(elem).trigger('hovertouch');
        }, 250);
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
        }
    });

};
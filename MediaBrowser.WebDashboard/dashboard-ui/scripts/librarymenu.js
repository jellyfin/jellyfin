(function (window, document, $) {

    var itemCountsPromise;
    var liveTvInfoPromise;
    var itemsPromise;

    function ensurePromises() {
        itemsPromise = itemsPromise || ApiClient.getItems(Dashboard.getCurrentUserId(), {

            SortBy: "SortName"

        });
        itemCountsPromise = itemCountsPromise || ApiClient.getItemCounts(Dashboard.getCurrentUserId());
        liveTvInfoPromise = liveTvInfoPromise || ApiClient.getLiveTvInfo();
    }

    function renderHeader(page, user) {

        var html = '<div class="viewMenuBar ui-bar-b">';

        html += '<button type="button" data-role="none" title="Menu" onclick="LibraryMenu.showLibraryMenu();" class="headerButton libraryMenuButton headerButtonLeft"><img src="css/images/menu.png" /></button>';

        html += '<a class="desktopHomeLink headerButton headerButtonLeft" href="index.html"><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></a>';

        //html += '<a class="viewMenuRemoteControlButton" href="nowplaying.html" data-role="button" data-icon="play" data-inline="true" data-iconpos="notext" title="Now Playing">Remote Control</a>';

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

        var $page = $(page);

        $page.prepend(html);

        $page.trigger('headercreated');
    }

    function getItemHref(item) {

        return LibraryBrowser.getHref(item);
    }

    function getViewsHtml(user, counts, items, liveTvInfo) {

        var html = '';

        html += items.map(function (i) {

            var viewMenuCssClass = (i.CollectionType || 'general') + 'ViewMenu';

            return '<a data-itemid="' + i.Id + '" class="lnkMediaFolder viewMenuLink viewMenuTextLink ' + viewMenuCssClass + '" href="' + getItemHref(i) + '">' + i.Name + '</a>';

        }).join('');

        var showChannels = counts.ChannelCount;
        var showLiveTv = liveTvInfo.EnabledUsers.indexOf(user.Id) != -1;

        if (showChannels || showLiveTv) {
            html += '<div class="libraryMenuDivider"></div>';
        }

        if (showChannels) {
            html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder channelsViewMenu" data-itemid="channels" href="channels.html">Channels</a>';
        }

        if (showLiveTv) {
            html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder tvshowsViewMenu" data-itemid="livetv" href="livetvsuggested.html">Live TV</a>';
        }

        if (user.Configuration.IsAdministrator) {
            html += '<div class="libraryMenuDivider"></div>';
            html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder dashboardViewMenu" data-itemid="editor" href="dashboard.html">Dashboard</a>';
            html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder editorViewMenu" data-itemid="editor" href="edititemmetadata.html">Metadata Manager</a>';
        }

        return html;
    }

    function showLibraryMenu() {

        ensurePromises();

        var userPromise = Dashboard.getCurrentUser();

        $.when(itemCountsPromise, itemsPromise, liveTvInfoPromise, userPromise).done(function (response1, response2, response3, response4) {

            var counts = response1[0];
            var items = response2[0].Items;
            var liveTvInfo = response3[0];
            var user = response4[0];

            var page = $.mobile.activePage;

            var panel = getLibraryMenu(page, user, counts, items, liveTvInfo);

            $(panel).panel('toggle');
        });
    }

    function getLibraryMenu(page, user, counts, items, liveTvInfo) {

        var panel = $('#libraryPanel', page);

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="libraryPanel" class="libraryPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<p class="libraryPanelHeader"><a href="index.html" class="imageLink"><img src="css/images/mblogoicon.png" /><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></a></p>';

            html += '<div style="margin: 0 -1em;">';
            html += getViewsHtml(user, counts, items, liveTvInfo);
            html += '</div>';

            html += '</div>';

            $(page).append(html);

            panel = $('#libraryPanel', page).panel({}).trigger('create');
        }

        updateLibraryNavLinks(page);

        return panel;
    }

    function getTopParentId() {

        return getParameterByName('topParentId') || sessionStorage.getItem('topParentId') || null;
    }

    window.LibraryMenu = {
        showLibraryMenu: showLibraryMenu,

        getTopParentId: getTopParentId
    };

    function updateCastIcon() {

        var info = MediaController.getPlayerInfo();

        if (info.isLocalPlayer) {

            $('.btnCast').addClass('btnDefaultCast').removeClass('btnActiveCast');

        } else {

            $('.btnCast').removeClass('btnDefaultCast').addClass('btnActiveCast');
        }
    }

    function updateLibraryNavLinks(page, updateElements) {

        page = $(page);

        var isLiveTvPage = page.hasClass('liveTvPage');
        var isChannelsPage = page.hasClass('channelsPage');

        var id = isLiveTvPage || isChannelsPage || page.hasClass('allLibraryPage') ?
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

    $(document).on('pageinit', ".libraryPage", function () {

        var page = this;

        $('.libraryViewNav', page).wrapInner('<div class="libraryViewNavInner"></div>');

        $('.libraryViewNav a', page).each(function () {

            this.innerHTML = '<span class="libraryViewNavLinkContent">' + this.innerHTML + '</span>';

        });

    }).on('pagebeforeshow', ".libraryPage", function () {

        var page = this;

        updateLibraryNavLinks(page);

        if (!$('.viewMenuBar', page).length) {

            Dashboard.getCurrentUser().done(function (user) {

                renderHeader(page, user);

                updateCastIcon();
                
                updateLibraryNavLinks(page);
            });
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
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

        html += '<button type="button" data-icon="bars" data-iconpos="notext" data-inline="true" title="Menu" class="libraryMenuButton" onclick="LibraryMenu.showLibraryMenu();">Menu</button>';

        html += '<a class="desktopHomeLink" href="index.html"><img src="css/images/mblogoicon.png" /><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></a>';

        html += '<a class="viewMenuRemoteControlButton" href="nowplaying.html" data-role="button" data-icon="play" data-inline="true" data-iconpos="notext" title="Now Playing">Remote Control</a>';

        if (user.Configuration.IsAdministrator) {
            html += '<a class="editorMenuLink" href="edititemmetadata.html" data-role="button" data-icon="edit" data-inline="true" data-iconpos="notext" title="Metadata Manager">Metadata Manager</a>';
        }

        html += '<div class="viewMenuSecondary">';

        html += '<button id="btnCast" class="btnCast btnDefaultCast" type="button" data-role="none"></button>';

        html += '<a class="viewMenuLink btnCurrentUser" href="#" onclick="Dashboard.showUserFlyout(this);">';

        if (user.PrimaryImageTag) {

            var url = ApiClient.getUserImageUrl(user.Id, {
                height: 24,
                tag: user.PrimaryImageTag,
                type: "Primary"
            });

            html += '<img src="' + url + '" />';
        } else {
            html += '<img src="css/images/currentuserdefaultwhite.png" />';
        }

        html += '</a>';

        html += '<button onclick="Search.showSearchPanel($.mobile.activePage);" type="button" data-icon="search" data-inline="true" data-iconpos="notext">Search</button>';

        if (user.Configuration.IsAdministrator) {
            html += '<a href="dashboard.html" data-role="button" data-icon="gear" data-inline="true" data-iconpos="notext">Dashboard</a>';
        }

        html += '</div>';

        html += '</div>';

        html += '<div class="desktopLibraryMenu">';
        html += '</div>';

        var $page = $(page);

        $page.prepend(html);

        $('.viewMenuBar,.desktopLibraryMenu', page).trigger('create');

        $page.trigger('headercreated');
    }

    function getItemHref(item) {

        if (item.CollectionType == 'boxsets' || item.Type == 'ManualCollectionsFolder') {
            return 'collections.html?topParentId=' + item.Id;
        }

        if (item.CollectionType == 'trailers' || item.Type == 'TrailerCollectionFolder') {
            return 'movietrailers.html?topParentId=' + item.Id;
        }

        if (item.CollectionType == 'movies') {
            return 'movieslatest.html?topParentId=' + item.Id;
        }

        if (item.CollectionType == 'tvshows') {
            return 'tvrecommended.html?topParentId=' + item.Id;
        }

        if (item.CollectionType == 'music') {
            return 'musicrecommended.html?topParentId=' + item.Id;
        }

        if (item.CollectionType == 'games') {
            return 'gamesrecommended.html?topParentId=' + item.Id;
        }

        return 'itemlist.html?topParentId=' + item.Id + '&parentid=' + item.Id;
    }

    function insertViews(page, user, counts, items, liveTvInfo) {

        var html = '';

        html += items.map(function (i) {

            var viewMenuCssClass = (i.CollectionType || 'general') + 'ViewMenu';

            return '<a data-itemid="' + i.Id + '" class="lnkMediaFolder viewMenuLink viewMenuTextLink ' + viewMenuCssClass + '" href="' + getItemHref(i) + '">' + i.Name + '</a>';

        }).join('');

        var showChannels = counts.ChannelCount;
        var showLiveTv = liveTvInfo.EnabledUsers.indexOf(user.Id) != -1;

        if (showChannels || showLiveTv) {
            html += '<div class="desktopLibraryMenuDivider"></div>';
        }

        if (showChannels) {
            html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder channelsViewMenu" data-itemid="channels" href="channels.html">Channels</a>';
        }

        if (showLiveTv) {
            html += '<a class="viewMenuLink viewMenuTextLink lnkMediaFolder tvshowsViewMenu" data-itemid="livetv" href="livetvsuggested.html">Live TV</a>';
        }

        $('.desktopLibraryMenu', page).html(html);
    }

    function showLibraryMenu() {

        ensurePromises();

        $.when(itemCountsPromise, itemsPromise, liveTvInfoPromise).done(function (response1, response2, response3) {

            var counts = response1[0];
            var items = response2[0].Items;
            var liveTvInfo = response3[0];

            var page = $.mobile.activePage;

            var panel = getLibraryMenu(page, counts, items, liveTvInfo);

            $(panel).panel('toggle');
        });
    }

    function getLibraryMenu(page, counts, items, liveTvInfo) {

        var panel = $('#libraryPanel', page);

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="libraryPanel" class="libraryPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<p class="libraryPanelHeader"><a href="index.html" class="imageLink"><img src="css/images/mblogoicon.png" /><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></a></p>';

            html += '<ul data-role="listview">';

            html += items.map(function (i) {

                return '<li><a data-itemid="' + i.Id + '" class="libraryPanelLink lnkMediaFolder" href="' + getItemHref(i) + '">' + i.Name + '</a></li>';

            }).join('');

            if (counts.ChannelCount) {
                html += '<li><a class="libraryPanelLink lnkMediaFolder" data-itemid="channels" href="channels.html">Channels</a></li>';
            }

            if (liveTvInfo.EnabledUsers.indexOf(Dashboard.getCurrentUserId()) != -1) {
                html += '<li><a class="libraryPanelLink lnkMediaFolder" data-itemid="livetv" href="livetvsuggested.html">Live TV</a></li>';
            }

            html += '</ul>';
            html += '</div>';

            $(page).append(html);

            panel = $('#libraryPanel', page).panel({}).trigger('create');
        }

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

    function updateLibraryNavLinks(page) {

        page = $(page);

        var isLiveTvPage = page.hasClass('liveTvPage');
        var isChannelsPage = page.hasClass('channelsPage');

        var id = isLiveTvPage || isChannelsPage || page.hasClass('allLibraryPage') ?
            '' :
            getTopParentId() || '';

        sessionStorage.setItem('topParentId', id);

        $('.lnkMediaFolder', page).each(function () {

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

    }).on('pagebeforeshow', ".page", function () {

        var page = this;

        if ($(page).hasClass('libraryPage')) {

            if (!$('.viewMenuBar', page).length) {

                Dashboard.getCurrentUser().done(function (user) {

                    renderHeader(page, user);

                    ensurePromises();

                    $.when(itemCountsPromise, itemsPromise, liveTvInfoPromise).done(function (response1, response2, response3) {

                        var counts = response1[0];
                        var items = response2[0].Items;
                        var liveTvInfo = response3[0];

                        insertViews(page, user, counts, items, liveTvInfo);

                        updateLibraryNavLinks(page);

                    });
                });
            }

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
(function (window, document, $) {

    var itemCountsPromise;
    var liveTvInfoPromise;

    function ensurePromises() {
        itemCountsPromise = itemCountsPromise || ApiClient.getItemCounts(Dashboard.getCurrentUserId());
        liveTvInfoPromise = liveTvInfoPromise || ApiClient.getLiveTvInfo();
    }

    function renderHeader(page, user) {

        var html = '<div class="viewMenuBar ui-bar-b">';

        html += '<button type="button" data-icon="bars" data-iconpos="notext" data-inline="true" title="Menu" class="libraryMenuButton" onclick="LibraryMenu.showLibraryMenu($(this).parents(\'.page\'));">Menu</button>';

        html += '<a class="desktopHomeLink" href="index.html"><img src="css/images/mblogoicon.png" /></a>';

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

        var $page = $(page);

        $page.prepend(html);

        $('.viewMenuBar', page).trigger('create');

        $page.trigger('headercreated');
    }

    function insertViews(page, user, counts, liveTvInfo) {

        var html = '';

        var selectedCssClass = ' selectedViewLink';
        var selectedHtml = "<span class='selectedViewIndicator'>&#9654;</span>";

        var view = page.getAttribute('data-view') || getParameterByName('context');

        if (counts.MovieCount) {

            html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'movies' ? selectedCssClass : '') + '" href="movieslatest.html">' + (view == 'movies' ? selectedHtml : '') + '<span class="viewName">Movies</span></a>';
        }

        if (counts.SeriesCount) {
            html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'tv' ? selectedCssClass : '') + '" href="tvrecommended.html">' + (view == 'tv' ? selectedHtml : '') + '<span class="viewName">TV</span></a>';
        }

        if (liveTvInfo.EnabledUsers.indexOf(user.Id) != -1) {
            html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'livetv' ? selectedCssClass : '') + '" href="livetvsuggested.html">' + (view == 'livetv' ? selectedHtml : '') + '<span class="viewName">Live TV</span></a>';
        }

        if (counts.SongCount || counts.MusicVideoCount) {
            html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'music' ? selectedCssClass : '') + '" href="musicrecommended.html">' + (view == 'music' ? selectedHtml : '') + '<span class="viewName">Music</span></a>';
        }

        if (counts.GameCount) {
            html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'games' ? selectedCssClass : '') + '" href="gamesrecommended.html">' + (view == 'games' ? selectedHtml : '') + '<span class="viewName">Games</span></a>';
        }

        if (counts.ChannelCount) {
            html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'channels' ? selectedCssClass : '') + '" href="channels.html">' + (view == 'channels' ? selectedHtml : '') + '<span class="viewName">Channels</span></a>';
        }

        //if (counts.BoxSetCount) {
        html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'boxsets' ? selectedCssClass : '') + '" href="collections.html">' + (view == 'boxsets' ? selectedHtml : '') + '<span class="viewName">Collections</span></a>';
        //}

        $('.viewMenuRemoteControlButton', page).before(html);
    }

    function showLibraryMenu(page) {

        ensurePromises();

        $.when(itemCountsPromise, liveTvInfoPromise).done(function (response1, response2) {

            var counts = response1[0];
            var liveTvInfo = response2[0];

            var panel = getLibraryMenu(page, counts, liveTvInfo);

            $(panel).panel('toggle');
        });
    }

    function getLibraryMenu(page, counts, liveTvInfo) {

        var panel = $('#libraryPanel', page);

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="libraryPanel" class="libraryPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<p class="libraryPanelHeader"><a href="index.html" class="imageLink"><img src="css/images/mblogoicon.png" /><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></a></p>';

            html += '<ul data-role="listview">';

            if (counts.MovieCount) {
                html += '<li><a class="libraryPanelLink" href="movieslatest.html">Movies</a></li>';
            }

            if (counts.SeriesCount) {
                html += '<li><a class="libraryPanelLink" href="tvrecommended.html">TV</a></li>';
            }

            if (liveTvInfo.EnabledUsers.indexOf(Dashboard.getCurrentUserId()) != -1) {
                html += '<li><a class="libraryPanelLink" href="livetvsuggested.html">Live TV</a></li>';
            }

            if (counts.SongCount || counts.MusicVideoCount) {
                html += '<li><a class="libraryPanelLink" href="musicrecommended.html">Music</a></li>';
            }

            if (counts.ChannelCount) {
                html += '<li><a class="libraryPanelLink" href="channels.html">Channels</a></li>';
            }

            if (counts.GameCount) {
                html += '<li><a class="libraryPanelLink" href="gamesrecommended.html">Games</a></li>';
            }

            //if (counts.BoxSetCount) {
            html += '<li><a class="libraryPanelLink" href="collections.html">Collections</a></li>';
            //}

            html += '</ul>';
            html += '</div>';

            $(page).append(html);

            panel = $('#libraryPanel', page).panel({}).trigger('create');
        }

        return panel;
    }

    window.LibraryMenu = {
        showLibraryMenu: showLibraryMenu
    };
    
    function updateCastIcon() {
        
        var info = MediaController.getPlayerInfo();

        if (info.isLocalPlayer) {

            $('.btnCast').addClass('btnDefaultCast').removeClass('btnActiveCast');

        } else {

            $('.btnCast').removeClass('btnDefaultCast').addClass('btnActiveCast');
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

        if (!$('.viewMenuBar', page).length) {

            Dashboard.getCurrentUser().done(function (user) {

                renderHeader(page, user);

                ensurePromises();

                $.when(itemCountsPromise, liveTvInfoPromise).done(function (response1, response2) {

                    var counts = response1[0];
                    var liveTvInfo = response2[0];

                    insertViews(page, user, counts, liveTvInfo);


                });
            });
        }

        updateCastIcon();

    }).on('pageshow', ".libraryPage", function () {

        var page = this;

        var elem = $('.libraryViewNavInner .ui-btn-active:visible', page);

        if (elem.length) {
            elem[0].scrollIntoView();

            // Scroll back up so in case vertical scroll was messed with
            $(document).scrollTop(0);
        }
    });

    $(function() {
        
        $(MediaController).on('playerchange', function () {
            updateCastIcon();
        });

    });


})(window, document, jQuery);
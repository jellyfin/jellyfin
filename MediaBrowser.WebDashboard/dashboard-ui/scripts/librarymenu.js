(function (window, document, $) {

    var itemCountsPromise;
    var liveTvServicesPromise;

    function ensurePromises() {
        itemCountsPromise = itemCountsPromise || ApiClient.getItemCounts(Dashboard.getCurrentUserId());
        liveTvServicesPromise = liveTvServicesPromise || ApiClient.getLiveTvServices();
    }

    function renderHeader(page, user) {

        var html = '<div class="viewMenuBar ui-bar-b">';

        html += '<button type="button" data-icon="bars" data-iconpos="notext" data-inline="true" title="Menu" class="libraryMenuButton" onclick="LibraryMenu.showLibraryMenu($(this).parents(\'.page\'));">Menu</button>';

        html += '<a class="desktopHomeLink" href="index.html"><img src="css/images/mblogoicon.png" /></a>';

        html += '<button class="viewMenuRemoteControlButton" onclick="RemoteControl.showMenu();" type="button" data-icon="remote" data-inline="true" data-iconpos="notext" title="Remote Control">Remote Control</button>';

        if (user.Configuration.IsAdministrator) {
            html += '<a class="editorMenuLink" href="edititemmetadata.html" data-role="button" data-icon="edit" data-inline="true" data-iconpos="notext" title="Metadata Manager">Metadata Manager</a>';
        }

        html += '<div class="viewMenuSecondary">';

        html += '<a class="viewMenuLink btnCurrentUser" href="#" onclick="Dashboard.showUserFlyout(this);">';

        if (user.PrimaryImageTag) {

            var url = ApiClient.getUserImageUrl(user.Id, {
                height: 40,
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

        $(page).prepend(html);

        $('.viewMenuBar', page).trigger('create');
    }

    function insertViews(page, user, counts, liveTvServices) {

        var html = '';

        var selectedCssClass = ' selectedViewLink';
        var selectedHtml = "<span class='selectedViewIndicator'>&#9654;</span>";

        var view = page.getAttribute('data-view') || getParameterByName('context');

        if (counts.MovieCount) {

            html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'movies' ? selectedCssClass : '') + '" href="moviesrecommended.html">' + (view == 'movies' ? selectedHtml : '') + '<span class="viewName">Movies</span></a>';
        }

        if (counts.SeriesCount) {
            html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'tv' ? selectedCssClass : '') + '" href="tvrecommended.html">' + (view == 'tv' ? selectedHtml : '') + '<span class="viewName">TV</span></a>';
        }

        if (liveTvServices.length) {
            html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'livetv' ? selectedCssClass : '') + '" href="livetvchannels.html">' + (view == 'livetv' ? selectedHtml : '') + '<span class="viewName">Live TV</span></a>';
        }

        if (counts.SongCount || counts.MusicVideoCount) {
            html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'music' ? selectedCssClass : '') + '" href="musicrecommended.html">' + (view == 'music' ? selectedHtml : '') + '<span class="viewName">Music</span></a>';
        }

        if (counts.GameCount) {
            html += '<a class="viewMenuLink viewMenuTextLink desktopViewMenuLink' + (view == 'games' ? selectedCssClass : '') + '" href="gamesrecommended.html">' + (view == 'games' ? selectedHtml : '') + '<span class="viewName">Games</span></a>';
        }

        $('.viewMenuRemoteControlButton', page).before(html);
    }

    function showLibraryMenu(page) {

        ensurePromises();

        $.when(itemCountsPromise, liveTvServicesPromise).done(function (response1, response2) {

            var counts = response1[0];
            var liveTvServices = response2[0];

            var panel = getLibraryMenu(page, counts, liveTvServices);

            $(panel).panel('toggle');
        });
    }

    function getLibraryMenu(page, counts, liveTvServices) {

        var panel = $('#libraryPanel', page);

        if (!panel.length) {

            var html = '';

            html += '<div data-role="panel" id="libraryPanel" class="libraryPanel" data-position="left" data-display="overlay" data-position-fixed="true" data-theme="b">';

            html += '<p class="libraryPanelHeader"><a href="index.html" class="imageLink"><img src="css/images/mblogoicon.png" /><span>MEDIA</span><span class="mediaBrowserAccent">BROWSER</span></a></p>';

            html += '<div data-role="collapsible-set" data-inset="false" data-mini="true">';

            if (counts.MovieCount) {

                html += getCollapsibleHtml('Movies', [

                    { text: 'Suggested', href: 'moviesrecommended.html' },
                    { text: 'Movies', href: 'movies.html' },
                    { text: 'Box Sets', href: 'boxsets.html' },
                    { text: 'Trailers', href: 'movietrailers.html' },
                    { text: 'Genres', href: 'moviegenres.html' },
                    { text: 'People', href: 'moviepeople.html' },
                    { text: 'Studios', href: 'moviestudios.html' }
                ]);
            }

            if (counts.SeriesCount) {
                html += getCollapsibleHtml('TV', [

                    { text: 'Suggested', href: 'tvrecommended.html' },
                    { text: 'Next Up', href: 'tvnextup.html' },
                    { text: 'Upcoming', href: 'tvupcoming.html' },
                    { text: 'Shows', href: 'tvshows.html' },
                    { text: 'Episodes', href: 'episodes.html' },
                    { text: 'Genres', href: 'tvgenres.html' },
                    { text: 'People', href: 'tvpeople.html' },
                    { text: 'Networks', href: 'tvstudios.html' }
                ]);
            }

            if (liveTvServices.length) {
                html += getCollapsibleHtml('Live TV', [

                    { text: 'Guide', href: 'livetvguide.html' },
                    { text: 'Channels', href: 'livetvchannels.html' },
                    { text: 'Recordings', href: 'livetvrecordings.html' },
                    { text: 'Scheduled', href: 'livetvtimers.html' },
                    { text: 'Series', href: 'livetvseriestimers.html' }
                ]);
            }

            if (counts.SongCount || counts.MusicVideoCount) {
                html += getCollapsibleHtml('Music', [

                    { text: 'Suggested', href: 'musicrecommended.html' },
                    { text: 'Songs', href: 'songs.html' },
                    { text: 'Albums', href: 'musicalbums.html' },
                    { text: 'Album Artists', href: 'musicalbumartists.html' },
                    { text: 'Artists', href: 'musicartists.html' },
                    { text: 'Music Videos', href: 'musicvideos.html' },
                    { text: 'Genres', href: 'musicgenres.html' }
                ]);
            }

            if (counts.GameCount) {
                html += getCollapsibleHtml('Games', [

                    { text: 'Suggested', href: 'gamesrecommended.html' },
                    { text: 'Games', href: 'games.html' },
                    { text: 'Game Systems', href: 'gamesystems.html' },
                    { text: 'Genres', href: 'gamegenres.html' },
                    { text: 'Studios', href: 'gamestudios.html' }
                ]);
            }

            html += '</div>';

            html += '</div>';

            $(page).append(html);

            panel = $('#libraryPanel', page).panel({}).trigger('create');
        }

        return panel;
    }

    function getCollapsibleHtml(title, links) {

        var i, length;
        var selectedIndex = -1;
        var collapsed = 'true';

        var currentUrl = window.location.toString().toLowerCase();

        for (i = 0, length = links.length; i < length; i++) {

            if (currentUrl.indexOf(links[i].href.toLowerCase()) != -1) {
                collapsed = 'false';
                selectedIndex = i;
                break;
            }
        }

        var html = '';

        html += '<div data-role="collapsible" data-mini="true" data-collapsed="' + collapsed + '">';
        html += '<h4 class="libraryPanelCollapsibleHeader">' + title + '</h4>';

        html += '<ul data-role="listview" data-inset="false">';

        for (i = 0, length = links.length; i < length; i++) {

            var link = links[i];
            
            html += '<li><a class="libraryPanelLink" href="' + link.href + '">' + link.text + '</a></li>';
        }

        html += '</ul>';
        html += '</div>';

        return html;
    }

    window.LibraryMenu = {
        showLibraryMenu: showLibraryMenu
    };

    $(document).on('pagebeforeshow', ".libraryPage", function () {

        var page = this;

        if (!$('.viewMenuBar', page).length) {

            Dashboard.getCurrentUser().done(function (user) {

                renderHeader(page, user);

                ensurePromises();

                $.when(itemCountsPromise, liveTvServicesPromise).done(function (response1, response2) {

                    var counts = response1[0];
                    var liveTvServices = response2[0];

                    insertViews(page, user, counts, liveTvServices);


                });
            });
        }
    });

})(window, document, jQuery);
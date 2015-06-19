(function ($, document, LibraryBrowser) {

    var currentItem;
    var shape;
    var _childrenItemsFunction;

    function getPromise() {

        var id = getParameterByName('id');

        if (id) {
            return ApiClient.getItem(Dashboard.getCurrentUserId(), id);
        }

        var name = getParameterByName('genre');

        if (name) {
            return ApiClient.getGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('musicgenre');

        if (name) {
            return ApiClient.getMusicGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('gamegenre');

        if (name) {
            return ApiClient.getGameGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('musicartist');

        if (name) {
            return ApiClient.getArtist(name, Dashboard.getCurrentUserId());
        }
        else {
            throw new Error('Invalid request');
        }
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        getPromise().done(function (item) {

            var context = getParameterByName('context');

            var editQuery = '?id=' + item.Id;
            if (context) {
                editQuery += '&context=' + context;
            }

            currentItem = item;

            Backdrops.setBackdrops(page, [item]);

            renderHeader(page, item, context);

            var name = item.Name;

            Dashboard.setPageTitle(name);

            $('.itemName', page).html(name);

            renderDetails(page, item, context);
            renderTabs(page, item, context);

            $(page).trigger('displayingitem', [{

                item: item,
                context: context
            }]);

            Dashboard.getCurrentUser().done(function (user) {

                if (MediaController.canPlay(item)) {
                    $('.btnPlay', page).show();
                } else {
                    $('.btnPlay', page).hide();
                }

                if (SyncManager.isAvailable(item, user)) {
                    $('.btnSync', page).removeClass('hide');
                } else {
                    $('.btnSync', page).addClass('hide');
                }

                var editImagesHref = user.Policy.IsAdministrator ? 'edititemimages.html' + editQuery : null;

                $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item, editImagesHref, true));

                if (LibraryBrowser.getMoreCommands(item, user).length) {
                    $('.btnMoreCommands', page).show();
                } else {
                    $('.btnMoreCommands', page).show();
                }

            });

            Dashboard.hideLoadingMsg();
        });
    }

    function renderHeader(page, item, context) {

        $('.itemTabs', page).hide();

        if (context == "movies" && item.Type == "Genre") {
            $('#movieGenreTabs', page).show();
        }

        if (context == "movies" && item.Type == "Person") {
            $('#moviePeopleTabs', page).show();
        }

        if (context == "movies" && item.Type == "Studio") {
            $('#movieStudioTabs', page).show();
        }

        if (context == "tv" && item.Type == "Studio") {
            $('#tvStudioTabs', page).show();
        }

        if (context == "tv" && item.Type == "Genre") {
            $('#tvGenreTabs', page).show();
        }

        if (context == "tv" && item.Type == "Person") {
            $('#tvPeopleTabs', page).show();
        }

        if (context == "music" && item.Type == "MusicGenre") {
            $('#musicGenreTabs', page).show();
        }
        if (context == "music" && item.Type == "MusicArtist") {
            $('#artistTabs', page).show();
        }
        if (context == "games" && item.Type == "GameGenre") {
            $('#gameGenreTabs', page).show();
        }
        if (context == "games" && item.Type == "Studio") {
            $('#gameStudioTabs', page).show();
        }
    }

    function renderTabs(page, item, context) {

        var html = '<fieldset data-role="controlgroup" data-type="horizontal" class="libraryTabs">';

        html += '<legend></legend>';

        if (item.MovieCount) {

            html += '<input type="radio" name="ibnItems" id="radioMovies" class="context-movies" value="on">';
            html += '<label for="radioMovies">' + Globalize.translate('TabMovies') + '</label>';
        }

        if (item.SeriesCount) {

            html += '<input type="radio" name="ibnItems" id="radioShows" class="context-tv" value="on">';
            html += '<label for="radioShows">' + Globalize.translate('TabSeries') + '</label>';
        }

        if (item.EpisodeCount) {

            html += '<input type="radio" name="ibnItems" id="radioEpisodes" class="context-tv" value="on">';
            html += '<label for="radioEpisodes">' + Globalize.translate('TabEpisodes') + '</label>';
        }

        if (item.TrailerCount) {

            html += '<input type="radio" name="ibnItems" id="radioTrailers" class="context-movies" value="on">';
            html += '<label for="radioTrailers">' + Globalize.translate('TabTrailers') + '</label>';
        }

        if (item.GameCount) {

            html += '<input type="radio" name="ibnItems" id="radioGames" class="context-games" value="on">';
            html += '<label for="radioGames">' + Globalize.translate('TabGames') + '</label>';
        }

        if (item.AlbumCount) {

            html += '<input type="radio" name="ibnItems" id="radioAlbums" class="context-music" value="on">';
            html += '<label for="radioAlbums">' + Globalize.translate('TabAlbums') + '</label>';
        }

        if (item.SongCount) {

            html += '<input type="radio" name="ibnItems" id="radioSongs" class="context-music" value="on">';
            html += '<label for="radioSongs">' + Globalize.translate('TabSongs') + '</label>';
        }

        if (item.MusicVideoCount) {

            html += '<input type="radio" name="ibnItems" id="radioMusicVideos" class="context-music" value="on">';
            html += '<label for="radioMusicVideos">' + Globalize.translate('TabMusicVideos') + '</label>';
        }

        html += '</fieldset>';

        var elem = $('#itemTabs', page).html(html).trigger('create');

        bindRadioEvents(page);

        var selectedRadio = null;

        if (context) {
            selectedRadio = $('.context-' + context + ':first', elem);
        }

        if (selectedRadio && selectedRadio.length) {
            selectedRadio.attr("checked", "checked").checkboxradio("refresh").trigger('click');
        } else {
            $('input:first', elem).attr("checked", "checked").checkboxradio("refresh").trigger('click');
        }

    }

    function bindRadioEvents(page) {

        $("#radioMusicVideos", page).on("click", function () {

            shape = "poster";
            loadItems(page, {
                MediaTypes: "",
                IncludeItemTypes: "MusicVideo",
                PersonTypes: "",
                ArtistIds: ""
            });

        });

        $("#radioMovies", page).on("click", function () {

            shape = "poster";
            loadItems(page, {
                MediaTypes: "",
                IncludeItemTypes: "Movie",
                PersonTypes: "",
                ArtistIds: ""
            });

        });

        $("#radioShows", page).on("click", function () {

            shape = "poster";
            loadItems(page, {
                MediaTypes: "",
                IncludeItemTypes: "Series",
                PersonTypes: "",
                ArtistIds: ""
            });
        });

        $("#radioTrailers", page).on("click", function () {

            shape = "poster";
            loadItems(page, {
                MediaTypes: "",
                IncludeItemTypes: "Trailer",
                PersonTypes: "",
                ArtistIds: ""
            });
        });

        $("#radioGames", page).on("click", function () {

            shape = "poster";
            loadItems(page, {
                IncludeItemTypes: "",
                MediaTypes: "Game",
                PersonTypes: "",
                ArtistIds: ""
            });
        });

        $("#radioEpisodes", page).on("click", function () {

            shape = "backdrop";
            loadItems(page, {
                MediaTypes: "",
                IncludeItemTypes: "Episode",
                PersonTypes: "",
                ArtistIds: ""
            });
        });

        $("#radioAlbums", page).on("click", function () {

            shape = "square";
            loadItems(page, {
                MediaTypes: "",
                IncludeItemTypes: "MusicAlbum",
                PersonTypes: "",
                ArtistIds: ""
            });
        });

        $("#radioSongs", page).on("click", function () {

            loadItems(page, {
                MediaTypes: "",
                IncludeItemTypes: "Audio",
                PersonTypes: "",
                ArtistIds: ""
            });
        });
    }

    function renderDetails(page, item, context) {

        //LibraryBrowser.renderDetailPageBackdrop(page, item);
        LibraryBrowser.renderOverview($('.itemOverview', page), item);

        renderUserDataIcons(page, item);
        LibraryBrowser.renderLinks($('#itemLinks', page), item);

        LibraryBrowser.renderGenres($('.itemGenres', page), item, context);

        if (item.Type == "Person" && item.PremiereDate) {

            try {
                var birthday = parseISO8601Date(item.PremiereDate, { toLocal: true }).toDateString();

                $('#itemBirthday', page).show().html(Globalize.translate('BirthDateValue').replace('{0}', birthday));
            }
            catch (err) {
                $('#itemBirthday', page).hide();
            }
        } else {
            $('#itemBirthday', page).hide();
        }

        if (item.Type == "Person" && item.EndDate) {

            try {
                var deathday = parseISO8601Date(item.EndDate, { toLocal: true }).toDateString();

                $('#itemDeathDate', page).show().html(Globalize.translate('DeathDateValue').replace('{0}', deathday));
            }
            catch (err) {
                $('#itemBirthday', page).hide();
            }
        } else {
        }

        if (item.Type == "Person" && item.ProductionLocations && item.ProductionLocations.length) {

            var gmap = '<a class="textlink" target="_blank" href="https://maps.google.com/maps?q=' + item.ProductionLocations[0] + '">' + item.ProductionLocations[0] + '</a>';

            $('#itemBirthLocation', page).show().html(Globalize.translate('BirthPlaceValue').replace('{0}', gmap)).trigger('create');
        } else {
            $('#itemBirthLocation', page).hide();
        }
    }

    function renderUserDataIcons(page, item) {
        $('.userDataIcons', page).html(LibraryBrowser.getUserDataIconsHtml(item));
    }

    function addCurrentItemToQuery(query) {

        if (currentItem.Type == "Person") {
            query.PersonIds = currentItem.Id;
        }
        else if (currentItem.Type == "Genre") {
            query.Genres = currentItem.Name;
        }
        else if (currentItem.Type == "MusicGenre") {
            query.Genres = currentItem.Name;
        }
        else if (currentItem.Type == "GameGenre") {
            query.Genres = currentItem.Name;
        }
        else if (currentItem.Type == "Studio") {
            query.StudioIds = currentItem.Id;
        }
        else if (currentItem.Type == "MusicArtist") {
            query.ArtistIds = currentItem.Id;
        }
    }

    function getQuery(options) {

        var query = {

            SortBy: "SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: "",
            Recursive: true,
            Fields: "AudioInfo,SeriesInfo,ParentId,PrimaryImageAspectRatio,SyncInfo",
            Limit: LibraryBrowser.getDefaultPageSize(),
            StartIndex: 0,
            CollapseBoxSetItems: false
        };

        query = $.extend(query, options || {});

        if (query.IncludeItemTypes == "Audio") {
            query.SortBy = "AlbumArtist,Album,SortName";
        }

        addCurrentItemToQuery(query);

        return query;
    }

    function getItemsFunction(options) {

        var query = getQuery(options);

        return function (index, limit, fields) {

            query.StartIndex = index;
            query.Limit = limit;
            query.Fields = fields;

            return ApiClient.getItems(Dashboard.getCurrentUserId(), query);

        };

    }

    function loadItems(page, options) {

        Dashboard.showLoadingMsg();

        _childrenItemsFunction = getItemsFunction(options);

        var query = getQuery(options);

        getItemsFunction(options)(options.StartIndex, options.Limit, options.Fields).done(function (result) {

            var html = '';

            if (result.TotalRecordCount > query.Limit) {

                var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    showLimit: false,
                    updatePageSizeSetting: false
                });

                $('.listTopPaging', page).html(pagingHtml);

                $('.viewSettings', page).show();
            } else {
                $('.listTopPaging', page).html('');
                $('.viewSettings', page).hide();
            }

            var screenWidth = $(window).width();

            if (query.IncludeItemTypes == "Audio") {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    playFromHere: true,
                    defaultAction: 'playallfromhere',
                    smallIcon: true
                });
            }
            else if (query.IncludeItemTypes == "Movie" || query.IncludeItemTypes == "Trailer") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    context: 'movies',
                    showTitle: true,
                    centerText: true
                });

            }
            else if (query.IncludeItemTypes == "Episode") {

                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    showTitle: true,
                    showParentTitle: true,
                    overlayText: screenWidth >= 600
                });

            }
            else if (query.IncludeItemTypes == "Series") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    preferThumb: true,
                    context: 'tv'
                });

            }
            else if (query.IncludeItemTypes == "MusicAlbum") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "square",
                    context: 'music',
                    playFromHere: true,
                    showTitle: true,
                    showParentTitle: true
                });

            }
            else {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    smallIcon: true
                });
            }

            html += LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                showLimit: false,
                updatePageSizeSetting: false
            });

            $('#items', page).html(html).trigger('create').lazyChildren();

            $('.btnNextPage', page).on('click', function () {

                query.StartIndex = query.StartIndex + query.Limit;
                loadItems(page, query);
            });

            $('.btnPreviousPage', page).on('click', function () {

                query.StartIndex = query.StartIndex - query.Limit;
                loadItems(page, query);
            });

            $('.selectPageSize', page).on('change', function () {
                query.Limit = parseInt(this.value);
                query.StartIndex = 0;
                loadItems(page, query);
            });

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageinitdepends', "#itemByNameDetailPage", function () {

        var page = this;

        $('.btnPlay', page).on('click', function () {
            var userdata = currentItem.UserData || {};
            LibraryBrowser.showPlayMenu(null, currentItem.Id, currentItem.Type, false, "Audio", userdata.PlaybackPositionTicks);
        });

        $('.itemsContainer', page).on('playallfromhere', function (e, index) {

            LibraryBrowser.playAllFromHere(_childrenItemsFunction, index);

        }).on('queueallfromhere', function (e, index) {

            LibraryBrowser.queueAllFromHere(_childrenItemsFunction, index);

        });

        $('.btnMoreCommands', page).on('click', function () {

            var button = this;

            Dashboard.getCurrentUser().done(function (user) {

                LibraryBrowser.showMoreCommands(button, currentItem.Id, LibraryBrowser.getMoreCommands(currentItem, user));
            });
        });

        $('.btnSync', page).on('click', function () {

            SyncManager.showMenu({
                items: [currentItem]
            });
        });

    }).on('pagebeforeshowready', "#itemByNameDetailPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#itemByNameDetailPage", function () {

        currentItem = null;
    });


})(jQuery, document, LibraryBrowser);
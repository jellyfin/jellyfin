define(['listView', 'cardBuilder', 'emby-itemscontainer'], function (listView, cardBuilder) {

    function renderItems(page, item) {

        var sections = [];

        if (item.MovieCount) {

            sections.push({
                name: Globalize.translate('TabMovies'),
                type: 'Movie'
            });
        }

        if (item.SeriesCount) {

            sections.push({
                name: Globalize.translate('TabSeries'),
                type: 'Series'
            });
        }

        if (item.EpisodeCount) {

            sections.push({
                name: Globalize.translate('TabEpisodes'),
                type: 'Episode'
            });
        }

        if (item.TrailerCount) {
            sections.push({
                name: Globalize.translate('TabTrailers'),
                type: 'Trailer'
            });
        }

        if (item.GameCount) {

            sections.push({
                name: Globalize.translate('TabGames'),
                type: 'Game'
            });
        }

        if (item.AlbumCount) {

            sections.push({
                name: Globalize.translate('TabAlbums'),
                type: 'MusicAlbum'
            });
        }

        if (item.SongCount) {

            sections.push({
                name: Globalize.translate('TabSongs'),
                type: 'Audio'
            });
        }

        if (item.MusicVideoCount) {

            sections.push({
                name: Globalize.translate('TabMusicVideos'),
                type: 'MusicVideo'
            });
        }

        var elem = page.querySelector('#childrenContent');

        elem.innerHTML = sections.map(function (section) {

            var html = '';

            html += '<div class="homePageSection" data-type="' + section.type + '">';

            html += '<div>';
            html += '<h1 class="listHeader" style="display:inline-block;vertical-align:middle;">';
            html += section.name;
            html += '</h1>';
            html += '<a href="#" class="clearLink hide" style="margin-left:1em;vertical-align:middle;"><button is="emby-button" type="button" class="raised more mini noIcon">' + Globalize.translate('ButtonMore') + '</button></a>';
            html += '</div>';

            html += '<div is="emby-itemscontainer" class="itemsContainer">';
            html += '</div>';

            html += '</div>';

            return html;

        }).join('');

        var sectionElems = elem.querySelectorAll('.homePageSection');
        for (var i = 0, length = sectionElems.length; i < length; i++) {
            renderSection(page, item, sectionElems[i], sectionElems[i].getAttribute('data-type'));
        }
    }

    function renderSection(page, item, element, type) {

        switch (type) {

            case 'Movie':
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Movie",
                    PersonTypes: "",
                    ArtistIds: "",
                    Limit: 10
                }, {
                    shape: "portrait",
                    showTitle: true,
                    centerText: true,
                    overlayMoreButton: true
                });
                break;

            case 'MusicVideo':
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "MusicVideo",
                    PersonTypes: "",
                    ArtistIds: "",
                    Limit: 10
                }, {
                    shape: "portrait",
                    showTitle: true,
                    centerText: true,
                    overlayPlayButton: true
                });
                break;

            case 'Game':
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Game",
                    PersonTypes: "",
                    ArtistIds: "",
                    Limit: 10
                }, {
                    shape: "portrait",
                    showTitle: true,
                    centerText: true,
                    overlayMoreButton: true
                });
                break;

            case 'Trailer':
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Trailer",
                    PersonTypes: "",
                    ArtistIds: "",
                    Limit: 10
                }, {
                    shape: "portrait",
                    showTitle: true,
                    centerText: true,
                    overlayPlayButton: true
                });
                break;

            case 'Series':
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Series",
                    PersonTypes: "",
                    ArtistIds: "",
                    Limit: 10
                }, {
                    shape: "portrait",
                    showTitle: true,
                    centerText: true,
                    overlayMoreButton: true
                });
                break;

            case 'MusicAlbum':
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "MusicAlbum",
                    PersonTypes: "",
                    ArtistIds: "",
                    Limit: 8
                }, {
                    shape: "square",
                    playFromHere: true,
                    showTitle: true,
                    showParentTitle: true,
                    centerText: true,
                    overlayPlayButton: true
                });
                break;

            case 'Episode':
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Episode",
                    PersonTypes: "",
                    ArtistIds: "",
                    Limit: 6
                }, {
                    shape: "backdrop",
                    showTitle: true,
                    showParentTitle: true,
                    centerText: true,
                    overlayPlayButton: true
                });
                break;

            case 'Audio':
                loadItems(element, item, type, {
                    MediaTypes: "",
                    IncludeItemTypes: "Audio",
                    PersonTypes: "",
                    ArtistIds: "",
                    Limit: 30
                }, {
                    playFromHere: true,
                    action: 'playallfromhere',
                    smallIcon: true
                });
                break;

            default:
                break;
        }
    }

    function loadItems(element, item, type, query, listOptions) {

        query = getQuery(query, item);

        getItemsFunction(query, item)(query.StartIndex, query.Limit, query.Fields).then(function (result) {

            var html = '';

            if (query.Limit && result.TotalRecordCount > query.Limit) {
                // Add more button
                var link = element.querySelector('a');
                link.classList.remove('hide');
                link.setAttribute('href', getMoreItemsHref(item, type));
            } else {
                element.querySelector('a').classList.add('hide');
            }

            listOptions.items = result.Items;
            var itemsContainer = element.querySelector('.itemsContainer');

            if (type == 'Audio') {
                html = listView.getListViewHtml(listOptions);
                itemsContainer.classList.remove('vertical-wrap');
                itemsContainer.classList.add('vertical-list');
            } else {
                html = cardBuilder.getCardsHtml(listOptions);
                itemsContainer.classList.add('vertical-wrap');
                itemsContainer.classList.remove('vertical-list');
            }

            itemsContainer.innerHTML = html;

            ImageLoader.lazyChildren(itemsContainer);
        });
    }

    function getMoreItemsHref(item, type) {

        return 'secondaryitems.html?type=' + type + '&parentId=' + item.Id;
    }

    function addCurrentItemToQuery(query, item) {

        if (item.Type == "Person") {
            query.PersonIds = item.Id;
        }
        else if (item.Type == "Genre") {
            query.Genres = item.Name;
        }
        else if (item.Type == "MusicGenre") {
            query.Genres = item.Name;
        }
        else if (item.Type == "GameGenre") {
            query.Genres = item.Name;
        }
        else if (item.Type == "Studio") {
            query.StudioIds = item.Id;
        }
        else if (item.Type == "MusicArtist") {
            query.ArtistIds = item.Id;
        }
    }

    function getQuery(options, item) {

        var query = {

            SortBy: "SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: "",
            Recursive: true,
            Fields: "AudioInfo,SeriesInfo,ParentId,PrimaryImageAspectRatio,BasicSyncInfo",
            Limit: LibraryBrowser.getDefaultPageSize(),
            StartIndex: 0,
            CollapseBoxSetItems: false
        };

        query = Object.assign(query, options || {});

        if (query.IncludeItemTypes == "Audio") {
            query.SortBy = "AlbumArtist,Album,SortName";
        }

        addCurrentItemToQuery(query, item);

        return query;
    }

    function getItemsFunction(options, item) {

        var query = getQuery(options, item);

        return function (index, limit, fields) {

            query.StartIndex = index;
            query.Limit = limit;

            if (fields) {
                query.Fields += "," + fields;
            }

            return ApiClient.getItems(Dashboard.getCurrentUserId(), query);

        };

    }

    window.ItemsByName = {
        renderItems: renderItems
    };

});
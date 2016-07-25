define(['libraryBrowser', 'scrollStyles', 'emby-itemscontainer'], function (libraryBrowser) {

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts;
    }

    function getThumbShape() {
        return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
    }

    function getPosterShape() {
        return enableScrollX() ? 'overflowPortrait' : 'portrait';
    }

    function getSquareShape() {
        return enableScrollX() ? 'overflowSquare' : 'square';
    }

    function getSections() {

        return [
            { name: 'HeaderFavoriteMovies', types: "Movie", id: "favoriteMovies", shape: getPosterShape(), showTitle: false, overlayPlayButton: true },
            { name: 'HeaderFavoriteShows', types: "Series", id: "favoriteShows", shape: getPosterShape(), showTitle: false, overlayPlayButton: true },
            { name: 'HeaderFavoriteEpisodes', types: "Episode", id: "favoriteEpisode", shape: getThumbShape(), preferThumb: false, showTitle: true, showParentTitle: true, overlayPlayButton: true },
            { name: 'HeaderFavoriteGames', types: "Game", id: "favoriteGames", shape: getSquareShape(), preferThumb: false, showTitle: true },
            { name: 'HeaderFavoriteArtists', types: "MusicArtist", id: "favoriteArtists", shape: getSquareShape(), preferThumb: false, showTitle: true, overlayText: false, showParentTitle: true, centerText: true, overlayPlayButton: true },
            { name: 'HeaderFavoriteAlbums', types: "MusicAlbum", id: "favoriteAlbums", shape: getSquareShape(), preferThumb: false, showTitle: true, overlayText: false, showParentTitle: true, centerText: true, overlayPlayButton: true },
            { name: 'HeaderFavoriteSongs', types: "Audio", id: "favoriteSongs", shape: getSquareShape(), preferThumb: false, showTitle: true, overlayText: false, showParentTitle: true, centerText: true, overlayMoreButton: true, defaultAction: 'instantmix' }
        ];
    }

    function loadSection(elem, userId, topParentId, section, isSingleSection) {

        var screenWidth = window.innerWidth;
        var options = {

            SortBy: "SortName",
            SortOrder: "Ascending",
            Filters: "IsFavorite",
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            CollapseBoxSetItems: false,
            ExcludeLocationTypes: "Virtual",
            EnableTotalRecordCount: false
        };

        if (topParentId) {
            options.ParentId = topParentId;
        }

        if (!isSingleSection) {
            options.Limit = screenWidth >= 1920 ? 10 : (screenWidth >= 1440 ? 8 : 6);

            if (enableScrollX()) {
                options.Limit = 20;
            }
        }

        var promise;
        if (section.types == 'MusicArtist') {
            promise = ApiClient.getArtists(userId, options);
        } else {

            options.IncludeItemTypes = section.types;
            promise = ApiClient.getItems(userId, options);
        }

        return promise.then(function (result) {

            var html = '';

            if (result.Items.length) {

                html += '<div>';
                html += '<h1 style="display:inline-block; vertical-align:middle;" class="listHeader">' + Globalize.translate(section.name) + '</h1>';

                if (options.Limit && result.Items.length >= options.Limit) {
                    var href = "secondaryitems.html?type=" + section.types + "&filters=IsFavorite&titlekey=" + section.name;

                    html += '<a class="clearLink" href="' + href + '" style="margin-left:2em;"><button is="emby-button" type="button" class="raised more mini">' + Globalize.translate('ButtonMore') + '</button></a>';
                }

                html += '</div>';

                if (enableScrollX()) {
                    html += '<div is="emby-itemscontainer" class="itemsContainer hiddenScrollX">';
                } else {
                    html += '<div is="emby-itemscontainer" class="itemsContainer">';
                }

                html += libraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: section.preferThumb,
                    shape: section.shape,
                    overlayText: section.overlayText !== false,
                    showTitle: section.showTitle,
                    showParentTitle: section.showParentTitle,
                    lazy: true,
                    showDetailsMenu: true,
                    centerText: section.centerText,
                    overlayPlayButton: section.overlayPlayButton,
                    overlayMoreButton: section.overlayMoreButton,
                    context: 'home-favorites',
                    defaultAction: section.defaultAction
                });

                html += '</div>';
            }

            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
        });
    }

    function loadSections(page, userId, topParentId, types) {

        Dashboard.showLoadingMsg();

        var sections = getSections();

        var sectionid = getParameterByName('sectionid');

        if (sectionid) {
            sections = sections.filter(function (s) {

                return s.id == sectionid;
            });
        }

        if (types) {
            sections = sections.filter(function (s) {

                return types.indexOf(s.id) != -1;
            });
        }

        var i, length;

        var elem = page.querySelector('.favoriteSections');

        if (!elem.innerHTML) {
            var html = '';
            for (i = 0, length = sections.length; i < length; i++) {

                html += '<div class="homePageSection section' + sections[i].id + '"></div>';
            }

            elem.innerHTML = html;
        }

        var promises = [];

        for (i = 0, length = sections.length; i < length; i++) {

            var section = sections[i];

            elem = page.querySelector('.section' + section.id);

            promises.push(loadSection(elem, userId, topParentId, section, sections.length == 1));
        }

        Promise.all(promises).then(function () {
            Dashboard.hideLoadingMsg();
        });
    }

    return {
        render: loadSections
    };

});
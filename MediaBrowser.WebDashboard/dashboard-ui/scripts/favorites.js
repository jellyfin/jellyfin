(function ($, document) {

    function enableScrollX() {
        return $.browser.mobile && AppInfo.enableAppLayouts;
    }

    function getThumbShape() {
        return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
    }

    function getPosterShape() {
        return enableScrollX() ? 'overflowportrait' : 'portrait';
    }

    function getSquareShape() {
        return enableScrollX() ? 'overflowSquare' : 'square';
    }

    function getSections() {

        return [
            { name: 'HeaderFavoriteMovies', types: "Movie", id: "favoriteMovies", shape: getPosterShape(), showTitle: false },
            { name: 'HeaderFavoriteShows', types: "Series", id: "favoriteShows", shape: getPosterShape(), showTitle: false },
            { name: 'HeaderFavoriteEpisodes', types: "Episode", id: "favoriteEpisode", shape: getThumbShape(), preferThumb: false, showTitle: true, showParentTitle: true },
            { name: 'HeaderFavoriteGames', types: "Game", id: "favoriteGames", shape: getSquareShape(), preferThumb: false, showTitle: true },
            { name: 'HeaderFavoriteAlbums', types: "MusicAlbum", id: "favoriteAlbums", shape: getSquareShape(), preferThumb: false, showTitle: true, overlayText: false, showParentTitle: true, centerText: true, overlayPlayButton: true }
        ];
    }

    function loadSection(elem, userId, section, isSingleSection) {

        var screenWidth = $(window).width();

        var options = {

            SortBy: "SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: section.types,
            Filters: "IsFavorite",
            Limit: screenWidth >= 1920 ? 10 : (screenWidth >= 1440 ? 8 : 6),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            CollapseBoxSetItems: false,
            ExcludeLocationTypes: "Virtual"
        };

        if (isSingleSection) {
            options.Limit = null;
        }

        return ApiClient.getItems(userId, options).done(function (result) {

            var html = '';

            if (result.Items.length) {

                html += '<div>';
                html += '<h1 style="display:inline-block; vertical-align:middle;" class="listHeader">' + Globalize.translate(section.name) + '</h1>';

                if (result.TotalRecordCount > result.Items.length) {
                    var href = "secondaryitems.html?type=" + section.types + "&filters=IsFavorite&titlekey=" + section.name;

                    html += '<a class="clearLink" href="' + href + '" style="margin-left:2em;"><paper-button raised class="more mini">' + Globalize.translate('ButtonMoreItems') + '</paper-button></a>';
                }

                html += '</div>';

                if (enableScrollX()) {
                    html += '<div class="itemsContainer hiddenScrollX">';
                } else {
                    html += '<div class="itemsContainer">';
                }

                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: section.preferThumb,
                    shape: section.shape,
                    overlayText: section.overlayText !== false,
                    context: 'home-favorites',
                    showTitle: section.showTitle,
                    showParentTitle: section.showParentTitle,
                    lazy: true,
                    showDetailsMenu: true,
                    centerText: section.centerText,
                    overlayPlayButton: section.overlayPlayButton
                });

                html += '</div>';
            }

            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
            $(elem).createCardMenus();
        });
    }

    function loadSections(page, userId) {

        Dashboard.showLoadingMsg();

        var sections = getSections();

        var sectionid = getParameterByName('sectionid');

        if (sectionid) {
            sections = sections.filter(function (s) {

                return s.id == sectionid;
            });
        }

        var i, length;

        var elem = $('.sections', page);

        if (!elem.html().length) {
            var html = '';
            for (i = 0, length = sections.length; i < length; i++) {

                html += '<div class="homePageSection section' + sections[i].id + '"></div>';
            }

            elem.html(html);
        }

        var promises = [];

        for (i = 0, length = sections.length; i < length; i++) {

            var section = sections[i];

            elem = page.querySelector('.section' + section.id);

            promises.push(loadSection(elem, userId, section, sections.length == 1));
        }

        $.when(promises).done(function () {
            Dashboard.hideLoadingMsg();

            LibraryBrowser.setLastRefreshed(page);
        });
    }

    $(document).on('pageinitdepends', "#indexPage", function () {

        var page = this;
        var tabContent = page.querySelector('.homeFavoritesTabContent');

        $(page.querySelector('neon-animated-pages')).on('tabchange', function () {

            if (parseInt(this.selected) == 2) {
                if (LibraryBrowser.needsRefresh(tabContent)) {
                    loadSections(tabContent, Dashboard.getCurrentUserId());
                }
            }
        });

    });

})(jQuery, document);
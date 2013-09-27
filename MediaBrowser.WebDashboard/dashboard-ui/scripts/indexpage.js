(function ($, document, apiClient) {

    function reloadTips(page) {

        var tips = [
            'Did you know that editing the artist or album of a music video will allow it to appear on the artist and album pages?',
            'Did you know that editing the tmdb id, tvdb id, and/or games db id of an album will allow media browser to link it to a movie, series or game as a soundtrack?',
            'Did you know that you can re-order your media collections by editing their sort names?',
            'Did you know that series, seasons, games and boxsets can have local trailers?',
            'Did you know that movies can have special features by placing them in a "specials" sub-folder underneath the movie folder?',
            'Did you know that the trailer plugin can automatically download trailers for existing movies in your collection?'
        ];

        var random = Math.floor((Math.random() * tips.length * 1.5));

        var tip = tips[random];

        if (tip) {
            $('#tip', page).html(tip).show();
        } else {
            $('#tip', page).hide();
        }
    }

    function getViewHtml(view) {

        var html = '';

        html += '<a id="' + view.id + '" class="posterItem backdropPosterItem" href="' + view.url + '">';

        html += '<div class="posterItemImage" style="padding:1px;"></div><div class="posterItemText posterItemTextCentered">' + view.name + '</div>';

        html += '</a>';

        return html;
    }

    function appendViewImages(elem, urls) {

        var html = '';

        for (var i = 0, length = urls.length; i < length; i++) {

            var url = urls[i];

            html += '<div class="viewCollageImage" style="background-image: url(\'' + url + '\');"></div>';

        }


        elem.html(html);
    }

    function renderMovieViewImages(page, userId) {

        apiClient.getItems(userId, {

            SortBy: "random",
            IncludeItemTypes: "Movie,Trailer",
            Limit: 6,
            ImageTypes: "Primary",
            Recursive: true

        }).done(function (result) {

            var urls = [];

            for (var i = 0, length = result.Items.length; i < length; i++) {

                urls.push(LibraryBrowser.getImageUrl(result.Items[i], 'Primary', 0, {
                    width: 160
                }));

            }

            appendViewImages($('#moviesView .posterItemImage', page), urls);

        });

    }

    function renderMusicViewImages(page, userId) {

        apiClient.getItems(userId, {

            SortBy: "random",
            IncludeItemTypes: "MusicAlbum",
            Limit: 6,
            ImageTypes: "Primary",
            Recursive: true

        }).done(function (result) {

            var urls = [];

            for (var i = 0, length = result.Items.length; i < length; i++) {

                urls.push(LibraryBrowser.getImageUrl(result.Items[i], 'Primary', 0, {
                    width: 160
                }));

            }

            appendViewImages($('#musicView .posterItemImage', page), urls);

        });

    }

    function renderGamesViewImages(page, userId) {

        apiClient.getItems(userId, {

            SortBy: "random",
            MediaTypes: "Game",
            Limit: 6,
            ImageTypes: "Primary",
            Recursive: true

        }).done(function (result) {

            var urls = [];

            for (var i = 0, length = result.Items.length; i < length; i++) {

                urls.push(LibraryBrowser.getImageUrl(result.Items[i], 'Primary', 0, {
                    width: 160
                }));

            }

            appendViewImages($('#gamesView .posterItemImage', page), urls);

        });

    }

    function renderTvViewImages(page, userId) {

        apiClient.getItems(userId, {

            SortBy: "random",
            IncludeItemTypes: "Series",
            Limit: 6,
            ImageTypes: "Primary",
            Recursive: true

        }).done(function (result) {

            var urls = [];

            for (var i = 0, length = result.Items.length; i < length; i++) {

                urls.push(LibraryBrowser.getImageUrl(result.Items[i], 'Primary', 0, {
                    width: 160
                }));

            }

            appendViewImages($('#tvView .posterItemImage', page), urls);

        });

    }

    function renderViews(page, userId) {

        apiClient.getItemCounts(userId).done(function (counts) {

            var views = [];

            var showViewsHeader;

            if (counts.MovieCount || counts.TrailerCount) {
                showViewsHeader = true;
                views.push({ id: "moviesView", name: "Movies", url: "moviesrecommended.html", img: "css/images/items/list/chapter.png", background: "#0094FF" });
            }

            if (counts.EpisodeCount || counts.SeriesCount) {
                showViewsHeader = true;
                views.push({ id: "tvView", name: "TV Shows", url: "tvrecommended.html", img: "css/images/items/list/collection.png", background: "#FF870F" });
            }

            if (counts.SongCount || counts.MusicVideoCount) {
                showViewsHeader = true;
                views.push({ id: "musicView", name: "Music", url: "musicrecommended.html", img: "css/images/items/list/audiocollection.png", background: "#6FBD45" });
            }

            if (counts.GameCount) {
                showViewsHeader = true;
                views.push({ id: "gamesView", name: "Games", url: "gamesrecommended.html", img: "css/images/items/list/gamecollection.png", background: "#E12026" });
            }
            
            if (showViewsHeader) {
                $('#viewsHeader', page).show();
            } else {
                $('#viewsHeader', page).hide();
            }

            var html = '';

            for (var i = 0, length = views.length; i < length; i++) {

                html += getViewHtml(views[i]);
            }

            var elem = $('#views', page).html(html);

            if (counts.MovieCount || counts.TrailerCount) {
                renderMovieViewImages(elem, userId);
            }
            if (counts.EpisodeCount || counts.SeriesCount) {
                renderTvViewImages(elem, userId);
            }
            if (counts.SongCount || counts.MusicVideoCount) {
                renderMusicViewImages(elem, userId);
            }
            if (counts.GameCount) {
                renderGamesViewImages(elem, userId);
            }
        });

    }

    $(document).on('pagebeforeshow', "#indexPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        if (!userId) {
            return;
        }

        renderViews(page, userId);

        var options = {

            sortBy: "SortName"
        };

        apiClient.getItems(userId, options).done(function (result) {

            $('#divCollections', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                showTitle: true,
                shape: "backdrop",
                centerText: true
            }));

        });
    });

    $(document).on('pagebeforeshow', "#indexPage", function () {

        var page = this;

        reloadTips(page);

    });

})(jQuery, document, ApiClient);
(function ($, document, apiClient) {

    function reloadTips(page) {

        var tips = [
            'Did you know that editing the artist or album of a music video will allow it to appear on the artist page?',
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

        html += '<a class="posterItem backdropPosterItem" href="' + view.url + '">';

        html += '<div class="posterItemImage" style="background-color: ' + view.background + ';background-image:url(\'' + view.img + '\');"></div><div class="posterItemText posterItemTextCentered">' + view.name + '</div>';

        html += '</a>';

        return html;
    }

    $(document).on('pagebeforeshow', "#indexPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        if (!userId) {
            return;
        }

        var options = {

            sortBy: "SortName"
        };

        apiClient.getItemCounts(userId).done(function (counts) {

            var views = [];

            if (counts.MovieCount || counts.TrailerCount) {
                views.push({ name: "Movies", url: "moviesrecommended.html", img: "css/images/items/list/chapter.png", background: "#0094FF" });
            }

            if (counts.EpisodeCount || counts.SeriesCount) {
                views.push({ name: "TV Shows", url: "tvrecommended.html", img: "css/images/items/list/collection.png", background: "#FF870F" });
            }

            if (counts.SongCount || counts.MusicVideoCount) {
                views.push({ name: "Music", url: "musicrecommended.html", img: "css/images/items/list/audiocollection.png", background: "#6FBD45" });
            }

            if (counts.GameCount) {
                views.push({ name: "Games", url: "gamesrecommended.html", img: "css/images/items/list/gamecollection.png", background: "#E12026" });
            }

            var html = '';

            for (var i = 0, length = views.length; i < length; i++) {

                html += getViewHtml(views[i]);
            }

            $('#views', page).html(html);
        });

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
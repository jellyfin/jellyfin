(function ($, document, apiClient) {

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

})(jQuery, document, ApiClient);
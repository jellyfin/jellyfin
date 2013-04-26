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

        apiClient.getItems(userId, options).done(function (result) {

            $('#divCollections', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                showTitle: true,
                shape: "backdrop",
                centerText: true
            }));

        });

        // Kick this off now. Just see if there are any games in the library
        var gamesPromise = ApiClient.getItems(userId, { Recursive: true, limit: 0, MediaTypes: "Game" });

        var views = [
            { name: "Movies", url: "moviesrecommended.html", img: "css/images/items/list/chapter.png", background: "#0094FF" },
            { name: "TV Shows", url: "tvrecommended.html", img: "css/images/items/list/collection.png", background: "#FF870F" },
            { name: "Music", url: "musicrecommended.html", img: "css/images/items/list/audiocollection.png", background: "#6FBD45" }
        ];

        var html = '';

        for (var i = 0, length = views.length; i < length; i++) {

            html += getViewHtml(views[i]);
        }

        $('#views', page).html(html);

        gamesPromise.done(function (result) {

            if (result.TotalRecordCount) {

                var view = { name: "Games", url: "gamesrecommended.html", img: "css/images/items/list/gamecollection.png", background: "#E12026" };

                $('#views', page).append(getViewHtml(view));
            }

        });
    });

})(jQuery, document, ApiClient);
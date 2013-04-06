(function ($, document, apiClient) {

    function getViewHtml(view) {

        var html = '';

        html += '<div class="posterViewItem">';
        html += '<a href="' + view.url + '">';
        html += '<img style="background: ' + view.background + ';" src="' + view.img + '"><div class="posterViewItemText">' + view.name + '</div>';
        html += '</a>';
        html += '</div>';

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
                showTitle: true
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

                var view = { name: "Games", url: "#", img: "css/images/items/list/gamecollection.png", background: "#E12026" };

                $('#views', page).append(getViewHtml(view));
            }

        });
    });

})(jQuery, document, ApiClient);
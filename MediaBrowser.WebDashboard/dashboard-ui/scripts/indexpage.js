(function ($, document, apiClient) {

    $(document).on('pageshow', "#indexPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        if (!userId) {
            return;
        }

        page = $(page);

        var options = {

            sortBy: "SortName"
        };

        apiClient.getItems(userId, options).done(function (result) {

            $('#divCollections', page).html(Dashboard.getPosterViewHtml({
                items: result.Items,
                showTitle: true
            }));

        });

        var views = [
            { name: "Movies", url: "moviesrecommended.html", img: "css/images/items/list/chapter.png", background: "#E12026" },
            { name: "TV Shows", url: "tvrecommended.html", img: "css/images/items/list/collection.png", background: "#FF870F" },
            { name: "Music", url: "musicrecommended.html", img: "css/images/items/list/audiowide.png", background: "#4BB3DD" }
        ];

        var html = '';

        for (var i = 0, length = views.length; i < length; i++) {

            var view = views[i];

            html += '<div class="posterViewItem">';
            html += '<a href="' + view.url + '">';
            html += '<img style="background: ' + view.background + ';" src="' + view.img + '"><div class="posterViewItemText">' + view.name + '</div>';
            html += '</a>';
            html += '</div>';
        }

        $('#views', page).html(html);
    });

})(jQuery, document, ApiClient);
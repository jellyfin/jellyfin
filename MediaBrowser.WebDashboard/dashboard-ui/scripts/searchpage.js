(function () {

    function loadSuggestions(page) {

        var options = {

            SortBy: "IsFavoriteOrLike,Random",
            IncludeItemTypes: "Movie,Series,MusicArtist",
            Limit: 20,
            Recursive: true,
            ImageTypeLimit: 0,
            EnableImages: false
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            var html = result.Items.map(function (i) {

                var href = LibraryBrowser.getHref(i);

                var itemHtml = '<a href="' + href + '" style="display:block;padding:.5em 0;">';
                itemHtml += i.Name;
                itemHtml += '</a>';
                return itemHtml;

            }).join('');

            page.querySelector('.searchSuggestions').innerHTML = html;
        });
    }

    pageIdOn('pageshowready', "searchPage", function () {

        var page = this;
        loadSuggestions(page);

        Search.showSearchPanel();
    });


})();
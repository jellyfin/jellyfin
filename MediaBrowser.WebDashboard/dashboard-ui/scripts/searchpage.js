define([], function () {

    function loadSuggestions(page) {

        var options = {

            SortBy: "IsFavoriteOrLiked,Random",
            IncludeItemTypes: "Movie,Series,MusicArtist",
            Limit: 20,
            Recursive: true,
            ImageTypeLimit: 0,
            EnableImages: false
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            var html = result.Items.map(function (i) {

                var href = LibraryBrowser.getHref(i);

                var itemHtml = '<div><a style="display:inline-block;padding:.55em 1em;" href="' + href + '">';
                itemHtml += i.Name;
                itemHtml += '</a></div>';
                return itemHtml;

            }).join('');

            page.querySelector('.searchSuggestions').innerHTML = html;
        });
    }

    pageIdOn('pageshow', "searchPage", function () {

        var page = this;
        loadSuggestions(page);

        Search.showSearchPanel();
    });


});
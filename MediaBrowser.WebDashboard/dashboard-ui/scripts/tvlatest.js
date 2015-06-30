(function ($, document) {

    function getView() {

        return 'Thumb';
    }

    function loadLatest(page) {

        var userId = Dashboard.getCurrentUserId();

        var parentId = LibraryMenu.getTopParentId();

        var limit = 30;

        if (AppInfo.hasLowImageBandwidth) {
            limit = 16;
        }

        var options = {

            IncludeItemTypes: "Episode",
            Limit: limit,
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            var view = getView();
            var html = '';

            if (view == 'ThumbCard') {

                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    shape: "backdrop",
                    preferThumb: true,
                    inheritThumb: false,
                    showUnplayedIndicator: false,
                    showChildCountIndicator: true,
                    overlayText: false,
                    showParentTitle: true,
                    lazy: true,
                    showTitle: true,
                    cardLayout: true
                });

            } else if (view == 'Thumb') {

                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    shape: "backdrop",
                    preferThumb: true,
                    inheritThumb: false,
                    showParentTitle: false,
                    showUnplayedIndicator: false,
                    showChildCountIndicator: true,
                    overlayText: false,
                    centerText: true,
                    lazy: true,
                    showTitle: false
                });
            }

            var elem = page.querySelector('#latestEpisodes');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
        });
    }

    $(document).on('pagebeforeshowready', "#tvNextUpPage", function () {

        var page = this;
        if (NavHelper.needsRefresh(page)) {
            loadLatest(page);
        }
    });


})(jQuery, document);
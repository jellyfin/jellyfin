(function ($, document) {

    function getView() {

        return 'Thumb';
    }

    function loadLatest(page) {

        Dashboard.showLoadingMsg();

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

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).then(function (items) {

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
                    showTitle: false,
                    overlayPlayButton: AppInfo.enableAppLayouts
                });
            }

            var elem = page.querySelector('#latestEpisodes');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            Dashboard.hideLoadingMsg();
            LibraryBrowser.setLastRefreshed(page);
        });
    }

    window.TvPage.renderLatestTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            loadLatest(tabContent);
        }
    };

})(jQuery, document);
define([], function () {

    function getView() {

        return 'Thumb';
    }

    function loadLatest(context, params) {

        Dashboard.showLoadingMsg();

        var userId = Dashboard.getCurrentUserId();

        var parentId = params.topParentId;

        var options = {

            IncludeItemTypes: "Episode",
            Limit: 30,
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

            var elem = context.querySelector('#latestEpisodes');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            Dashboard.hideLoadingMsg();
        });
    }
    return function (view, params, tabContent) {

        var self = this;

        self.renderTab = function() {

            loadLatest(tabContent, params);
        };
    };
});
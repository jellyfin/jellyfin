define(['components/categorysyncbuttons', 'cardBuilder', 'apphost', 'emby-itemscontainer'], function (categorysyncbuttons, cardBuilder, appHost) {

    function getNextUpPromise() {

        var query = {

            Limit: 24,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated,BasicSyncInfo",
            UserId: Dashboard.getCurrentUserId(),
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Thumb"
        };

        return ApiClient.getNextUpEpisodes(query);
    }
    function loadNextUp(page, promise) {

        promise.then(function (result) {

            if (result.Items.length) {
                page.querySelector('.noNextUpItems').classList.add('hide');
            } else {
                page.querySelector('.noNextUpItems').classList.remove('hide');
            }

            var html = '';

            var supportsImageAnalysis = appHost.supports('imageanalysis');

            html += cardBuilder.getCardsHtml({
                items: result.Items,
                shape: "backdrop",
                showTitle: true,
                showParentTitle: true,
                overlayText: false,
                lazy: true,
                preferThumb: true,
                showDetailsMenu: true,
                centerText: !supportsImageAnalysis,
                overlayPlayButton: AppInfo.enableAppLayouts && !supportsImageAnalysis,
                context: 'home-nextup',
                cardLayout: supportsImageAnalysis,
                vibrant: supportsImageAnalysis
            });

            var elem = page.querySelector('#nextUpItems');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
            Dashboard.hideLoadingMsg();
        });
    }
    return function (view, params, tabContent) {

        var self = this;
        var nextUpPromise;

        categorysyncbuttons.init(view);

        self.preRender = function () {
            nextUpPromise = getNextUpPromise();
        };

        self.renderTab = function () {

            Dashboard.showLoadingMsg();
            loadNextUp(view, nextUpPromise);
        };
    };

});
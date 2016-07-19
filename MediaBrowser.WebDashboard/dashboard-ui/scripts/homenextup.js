define(['components/categorysyncbuttons', 'emby-itemscontainer'], function (categorysyncbuttons) {

    function getNextUpPromise() {

        var query = {

            Limit: 24,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated,SyncInfo",
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

            html += LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "backdrop",
                showTitle: true,
                showParentTitle: true,
                overlayText: false,
                lazy: true,
                preferThumb: true,
                showDetailsMenu: true,
                centerText: true,
                overlayPlayButton: AppInfo.enableAppLayouts,
                context: 'home-nextup'
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
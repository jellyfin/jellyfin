define(['datetime', 'cardBuilder', 'imageLoader', 'apphost', 'paper-icon-button-light', 'emby-button'], function (datetime, cardBuilder, imageLoader, appHost) {

    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending"
    };

    function renderTimers(context, timers) {

        var html = '';

        var supportsImageAnalysis = appHost.supports('imageanalysis');
        var cardLayout = appHost.preferVisualCards || supportsImageAnalysis;

        html += cardBuilder.getCardsHtml({
            items: timers,
            shape: 'backdrop',
            showTitle: true,
            cardLayout: cardLayout,
            vibrant: supportsImageAnalysis,
            preferThumb: true,
            coverImage: true,
            overlayText: false,
            showSeriesTimerTime: true,
            showSeriesTimerChannel: true,
            centerText: !cardLayout,
            overlayMoreButton: !cardLayout
        });

        var elem = context.querySelector('#items');
        elem.innerHTML = html;

        imageLoader.lazyChildren(elem);

        Dashboard.hideLoadingMsg();
    }

    function reload(context, promise) {

        Dashboard.showLoadingMsg();

        promise.then(function (result) {

            renderTimers(context, result.Items);
        });
    }

    return function (view, params, tabContent) {

        var self = this;
        var timersPromise;        self.preRender = function () {
            timersPromise = ApiClient.getLiveTvSeriesTimers(query);
        };

        self.renderTab = function () {

            reload(tabContent, timersPromise);
        };
    };

});
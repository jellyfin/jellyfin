define(['datetime', 'cardBuilder', 'imageLoader', 'paper-icon-button-light', 'emby-button'], function (datetime, cardBuilder, imageLoader) {

    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending"
    };

    function renderTimers(context, timers) {

        var html = '';

        html += cardBuilder.getCardsHtml({
            items: timers,
            shape: 'backdrop',
            showTitle: true,
            cardLayout: true,
            vibrant: true,
            preferThumb: true,
            coverImage: true,
            overlayText: false,
            showSeriesTimerTime: true,
            showSeriesTimerChannel: true
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
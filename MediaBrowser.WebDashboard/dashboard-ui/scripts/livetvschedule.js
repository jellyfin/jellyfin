define(['scripts/livetvcomponents', 'emby-button', 'emby-itemscontainer'], function () {

    function renderActiveRecordings(context, promise) {

        promise.then(function (result) {

            // The IsActive param is new, so handle older servers that don't support it
            if (result.Items.length && result.Items[0].Status != 'InProgress') {
                result.Items = [];
            }

            renderTimers(context.querySelector('#activeRecordings'), result.Items, {
                indexByDate: false
            });
        });
    }

    function renderTimers(context, timers, options) {

        LiveTvHelpers.getTimersHtml(timers, options).then(function (html) {

            var elem = context;

            if (html) {
                elem.classList.remove('hide');
            } else {
                elem.classList.add('hide');
            }

            elem.querySelector('.recordingItems').innerHTML = html;

            ImageLoader.lazyChildren(elem);
        });
    }

    function renderUpcomingRecordings(context, promise) {

        promise.then(function (result) {

            renderTimers(context.querySelector('#upcomingRecordings'), result.Items);
            Dashboard.hideLoadingMsg();
        });
    }

    return function (view, params, tabContent) {

        var self = this;
        var activeRecordingsPromise;
        var upcomingRecordingsPromise;

        tabContent.querySelector('#upcomingRecordings .recordingItems').addEventListener('timercancelled', function () {
            self.preRender();
            self.renderTab();
        });

        self.preRender = function () {
            activeRecordingsPromise = ApiClient.getLiveTvTimers({
                IsActive: true
            });

            upcomingRecordingsPromise = ApiClient.getLiveTvTimers({
                IsActive: false,
                IsScheduled: true
            });
        };

        self.renderTab = function () {
            Dashboard.showLoadingMsg();

            renderActiveRecordings(tabContent, activeRecordingsPromise);
            renderUpcomingRecordings(tabContent, upcomingRecordingsPromise);
        };
    };

});
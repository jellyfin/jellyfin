define(['datetime', 'dom', 'seriesRecordingEditor', 'emby-itemscontainer'], function (datetime, dom, seriesRecordingEditor) {

    return function (view, params) {

        function renderTimer(page, item) {

            page.querySelector('.itemName').innerHTML = item.Name;

            Dashboard.hideLoadingMsg();
        }

        function renderSchedule(page, result) {

            var timers = result.Items;

            LiveTvHelpers.getTimersHtml(timers).then(function (html) {

                var scheduleTab = page.querySelector('.scheduleTab');
                scheduleTab.innerHTML = html;

                ImageLoader.lazyChildren(scheduleTab);
            });
        }

        function reload() {

            var id = params.id;
            Dashboard.showLoadingMsg();

            ApiClient.getLiveTvSeriesTimer(id).then(function (result) {

                renderTimer(view, result);

            });

            ApiClient.getLiveTvTimers({

                seriesTimerId: id

            }).then(function (timerResult) {

                renderSchedule(view, timerResult);

            });
        }

        seriesRecordingEditor.embed(params.id, ApiClient.serverId(), {
            context: view.querySelector('.recordingEditor')
        });

        view.querySelector('.scheduleTab').addEventListener('timercancelled', reload);
        view.addEventListener('viewbeforeshow', reload);
    };
});
define(['datetime', 'dom', 'seriesRecordingEditor', 'emby-itemscontainer'], function (datetime, dom, seriesRecordingEditor) {

    return function (view, params) {

        function renderTimer(page, item) {

            page.querySelector('.itemName').innerHTML = item.Name;

            Dashboard.hideLoadingMsg();
        }

        function renderSchedule(page) {

            ApiClient.getLiveTvPrograms({
                UserId: ApiClient.getCurrentUserId(),
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Thumb",
                SortBy: "StartDate",
                EnableTotalRecordCount: false,
                EnableUserData: false,
                SeriesTimerId: params.id,
                Fields: "ChannelInfo",
                limit: 100

            }).then(function (result) {

                if (result.Items.length && result.Items[0].SeriesTimerId != params.id) {
                    result.Items = [];
                }

                LiveTvHelpers.getProgramScheduleHtml(result.Items).then(function (html) {

                    var scheduleTab = page.querySelector('.scheduleTab');
                    scheduleTab.innerHTML = html;

                    ImageLoader.lazyChildren(scheduleTab);
                });
            });

            //var timers = result.Items;

            //LiveTvHelpers.getTimersHtml(timers).then(function (html) {

            //    var scheduleTab = page.querySelector('.scheduleTab');
            //    scheduleTab.innerHTML = html;

            //    ImageLoader.lazyChildren(scheduleTab);
            //});
        }

        function reload() {

            var id = params.id;
            Dashboard.showLoadingMsg();

            ApiClient.getLiveTvSeriesTimer(id).then(function (result) {

                renderTimer(view, result);

            });

            renderSchedule(view);
        }

        seriesRecordingEditor.embed(params.id, ApiClient.serverId(), {
            context: view.querySelector('.recordingEditor')
        });

        view.querySelector('.scheduleTab').addEventListener('timercancelled', reload);
        view.addEventListener('viewbeforeshow', reload);
    };
});
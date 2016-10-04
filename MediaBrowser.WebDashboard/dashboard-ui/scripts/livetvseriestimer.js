define(['datetime', 'dom', 'seriesRecordingEditor', 'listView', 'emby-itemscontainer'], function (datetime, dom, seriesRecordingEditor, listView) {

    return function (view, params) {

        function renderTimer(page, item) {

            page.querySelector('.itemName').innerHTML = item.Name;

            Dashboard.hideLoadingMsg();
        }

        function getProgramScheduleHtml(items, options) {

            options = options || {};

            var html = '';
            html += '<div is="emby-itemscontainer" class="itemsContainer vertical-list" data-contextmenu="false">';
            html += listView.getListViewHtml({
                items: items,
                enableUserDataButtons: false,
                image: false,
                showProgramDateTime: true,
                mediaInfo: false,
                action: 'none',
                moreButton: false,
                recordButton: false
            });

            html += '</div>';

            return html;
        }

        function renderSchedule(page) {

            ApiClient.getLiveTvTimers({
                UserId: ApiClient.getCurrentUserId(),
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Thumb",
                SortBy: "StartDate",
                EnableTotalRecordCount: false,
                EnableUserData: false,
                SeriesTimerId: params.id,
                Fields: "ChannelInfo"

            }).then(function (result) {

                if (result.Items.length && result.Items[0].SeriesTimerId != params.id) {
                    result.Items = [];
                }

                var html = getProgramScheduleHtml(result.Items);

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

            renderSchedule(view);
        }

        seriesRecordingEditor.embed(params.id, ApiClient.serverId(), {
            context: view.querySelector('.recordingEditor')
        });

        view.querySelector('.scheduleTab').addEventListener('timercancelled', reload);
        view.addEventListener('viewbeforeshow', reload);
    };
});
define(['components/categorysyncbuttons', 'scripts/livetvcomponents', 'emby-button', 'listViewStyle', 'emby-itemscontainer'], function (categorysyncbuttons) {

    function getRecordingGroupHtml(group) {

        var html = '';

        html += '<div class="listItem">';

        html += '<button type="button" is="emby-button" class="fab mini autoSize blue" item-icon><i class="md-icon">live_tv</i></button>';

        html += '<div class="listItemBody two-line">';
        html += '<a href="livetvrecordinglist.html?groupid=' + group.Id + '" class="clearLink">';

        html += '<div>';
        html += group.Name;
        html += '</div>';

        html += '<div class="secondary">';
        if (group.RecordingCount == 1) {
            html += Globalize.translate('ValueItemCount', group.RecordingCount);
        } else {
            html += Globalize.translate('ValueItemCountPlural', group.RecordingCount);
        }
        html += '</div>';

        html += '</a>';
        html += '</div>';
        html += '</div>';

        return html;
    }

    function renderRecordingGroups(context, groups) {

        if (groups.length) {
            context.querySelector('#recordingGroups').classList.remove('hide');
        } else {
            context.querySelector('#recordingGroups').classList.add('hide');
        }

        var html = '';

        html += '<div class="paperList">';

        for (var i = 0, length = groups.length; i < length; i++) {

            html += getRecordingGroupHtml(groups[i]);
        }

        html += '</div>';

        context.querySelector('#recordingGroupItems').innerHTML = html;

        Dashboard.hideLoadingMsg();
    }

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts;
    }

    function renderRecordings(elem, recordings) {

        if (recordings.length) {
            elem.classList.remove('hide');
        } else {
            elem.classList.add('hide');
        }

        var recordingItems = elem.querySelector('.recordingItems');

        if (enableScrollX()) {
            recordingItems.classList.add('hiddenScrollX');
        } else {
            recordingItems.classList.remove('hiddenScrollX');
        }

        recordingItems.innerHTML = LibraryBrowser.getPosterViewHtml({
            items: recordings,
            shape: (enableScrollX() ? 'autooverflow' : 'auto'),
            showTitle: true,
            showParentTitle: true,
            coverImage: true,
            lazy: true,
            cardLayout: true
        });

        ImageLoader.lazyChildren(recordingItems);
    }

    function renderActiveRecordings(context) {

        ApiClient.getLiveTvTimers({

            IsActive: true

        }).then(function (result) {

            // The IsActive param is new, so handle older servers that don't support it
            if (result.Items.length && result.Items[0].Status != 'InProgress') {
                result.Items = [];
            }

            renderTimers(context.querySelector('#activeRecordings'), result.Items, {
                indexByDate: false
            });
        });

        //ApiClient.getLiveTvRecordings({

        //    userId: Dashboard.getCurrentUserId(),
        //    IsInProgress: true,
        //    Fields: 'CanDelete'

        //}).then(function (result) {

        //    renderRecordings(context.querySelector('#activeRecordings'), result.Items);

        //});
    }

    function renderLatestRecordings(context) {

        ApiClient.getLiveTvRecordings({

            userId: Dashboard.getCurrentUserId(),
            limit: enableScrollX() ? 12 : 4,
            IsInProgress: false,
            Fields: 'CanDelete,PrimaryImageAspectRatio',
            EnableTotalRecordCount: false

        }).then(function (result) {

            renderRecordings(context.querySelector('#latestRecordings'), result.Items);
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

    function renderUpcomingRecordings(context) {

        ApiClient.getLiveTvTimers({
            IsActive: false
        }).then(function (result) {

            renderTimers(context.querySelector('#upcomingRecordings'), result.Items);
        });
    }

    function reload(context) {

        Dashboard.showLoadingMsg();

        renderUpcomingRecordings(context);
        renderActiveRecordings(context);
        renderLatestRecordings(context);

        ApiClient.getLiveTvRecordingGroups({

            userId: Dashboard.getCurrentUserId()

        }).then(function (result) {

            renderRecordingGroups(context, result.Items);
        });
    }

    return function (view, params, tabContent) {

        var self = this;

        categorysyncbuttons.init(tabContent);
        tabContent.querySelector('#activeRecordings .recordingItems').addEventListener('timercancelled', function () {
            reload(tabContent);
        });
        tabContent.querySelector('#upcomingRecordings .recordingItems').addEventListener('timercancelled', function () {
            reload(tabContent);
        });

        self.renderTab = function () {
            reload(tabContent);
        };
    };

});
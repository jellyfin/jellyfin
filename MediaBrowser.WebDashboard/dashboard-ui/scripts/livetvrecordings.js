define(['jQuery', 'scripts/livetvcomponents'], function ($) {

    function getRecordingGroupHtml(group) {

        var html = '';

        html += '<paper-icon-item>';

        html += '<paper-fab mini class="blue" icon="live-tv" item-icon></paper-fab>';

        html += '<paper-item-body two-line>';
        html += '<a href="livetvrecordinglist.html?groupid=' + group.Id + '" class="clearLink">';

        html += '<div>';
        html += group.Name;
        html += '</div>';

        html += '<div secondary>';
        if (group.RecordingCount == 1) {
            html += Globalize.translate('ValueItemCount', group.RecordingCount);
        } else {
            html += Globalize.translate('ValueItemCountPlural', group.RecordingCount);
        }
        html += '</div>';

        html += '</a>';
        html += '</paper-item-body>';
        html += '</paper-icon-item>';

        return html;
    }

    function renderRecordingGroups(context, groups) {

        if (groups.length) {
            $('#recordingGroups', context).show();
        } else {
            $('#recordingGroups', context).hide();
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

        ApiClient.getLiveTvRecordings({

            userId: Dashboard.getCurrentUserId(),
            IsInProgress: true,
            Fields: 'CanDelete'

        }).then(function (result) {

            renderRecordings(context.querySelector('#activeRecordings'), result.Items);

        });
    }

    function renderLatestRecordings(context) {

        ApiClient.getLiveTvRecordings({

            userId: Dashboard.getCurrentUserId(),
            limit: enableScrollX() ? 12 : 4,
            IsInProgress: false,
            Fields: 'CanDelete,PrimaryImageAspectRatio'

        }).then(function (result) {

            renderRecordings(context.querySelector('#latestRecordings'), result.Items);
        });
    }

    function renderTimers(context, timers) {

        LiveTvHelpers.getTimersHtml(timers).then(function (html) {

            var elem = context.querySelector('#upcomingRecordings');

            if (html) {
                elem.classList.remove('hide');
            } else {
                elem.classList.add('hide');
            }

            elem.querySelector('.recordingItems').innerHTML = html;

            ImageLoader.lazyChildren(elem);
            $(elem).createCardMenus();
        });
    }

    function renderUpcomingRecordings(context) {

        ApiClient.getLiveTvTimers().then(function (result) {

            renderTimers(context, result.Items);
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

            require(['paper-fab', 'paper-item-body', 'paper-icon-item'], function () {
                renderRecordingGroups(context, result.Items);
            });
        });
    }

    return function (view, params, tabContent) {

        var self = this;
        tabContent.querySelector('#upcomingRecordings .recordingItems').addEventListener('timercancelled', function () {
            reload(tabContent);
        });

        self.renderTab = function () {
            reload(tabContent);
        };
    };

});
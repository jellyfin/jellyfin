define(['jQuery'], function ($) {

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

    function renderRecordingGroups(page, groups) {

        if (groups.length) {
            $('#recordingGroups', page).show();
        } else {
            $('#recordingGroups', page).hide();
        }

        var html = '';

        html += '<div class="paperList">';

        for (var i = 0, length = groups.length; i < length; i++) {

            html += getRecordingGroupHtml(groups[i]);
        }

        html += '</div>';

        page.querySelector('#recordingGroupItems').innerHTML = html;

        Dashboard.hideLoadingMsg();
    }

    function renderRecordings(elem, recordings) {

        if (recordings.length) {
            elem.classList.remove('hide');
        } else {
            elem.classList.add('hide');
        }

        var recordingItems = elem.querySelector('.recordingItems');
        recordingItems.innerHTML = LibraryBrowser.getPosterViewHtml({
            items: recordings,
            shape: "auto",
            showTitle: true,
            showParentTitle: true,
            centerText: true,
            coverImage: true,
            lazy: true,
            overlayPlayButton: true

        });

        ImageLoader.lazyChildren(recordingItems);
    }

    function renderActiveRecordings(page) {

        ApiClient.getLiveTvRecordings({

            userId: Dashboard.getCurrentUserId(),
            IsInProgress: true,
            Fields: 'CanDelete'

        }).then(function (result) {

            renderRecordings(page.querySelector('#activeRecordings'), result.Items);

        });
    }

    function renderLatestRecordings(page) {

        ApiClient.getLiveTvRecordings({

            userId: Dashboard.getCurrentUserId(),
            limit: 12,
            IsInProgress: false,
            Fields: 'CanDelete,PrimaryImageAspectRatio'

        }).then(function (result) {

            renderRecordings(page.querySelector('#latestRecordings'), result.Items);
        });
    }

    function renderTimers(page, timers) {

        LiveTvHelpers.getTimersHtml(timers).then(function (html) {

            var elem = page.querySelector('#upcomingRecordings');

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

    function renderUpcomingRecordings(page) {

        ApiClient.getLiveTvTimers().then(function (result) {

            renderTimers(page, result.Items);
        });
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        renderUpcomingRecordings(page);
        renderActiveRecordings(page);
        renderLatestRecordings(page);

        ApiClient.getLiveTvRecordingGroups({

            userId: Dashboard.getCurrentUserId()

        }).then(function (result) {

            require(['paper-fab', 'paper-item-body', 'paper-icon-item'], function () {
                renderRecordingGroups(page, result.Items);
            });
        });
    }

    window.LiveTvPage.initRecordingsTab = function (page, tabContent) {

        tabContent.querySelector('#upcomingRecordings .recordingItems').addEventListener('timercancelled', function () {
            reload(tabContent);
        });
    };

    window.LiveTvPage.renderRecordingsTab = function (page, tabContent) {

        reload(tabContent);
    };

});
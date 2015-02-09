(function ($, document) {

    function getRecordingGroupHtml(group) {

        var html = '';

        html += '<li><a href="livetvrecordinglist.html?groupid=' + group.Id + '">';

        html += '<h3>';
        html += group.Name;
        html += '</h3>';

        html += '<span class="ui-li-count">' + group.RecordingCount + '</span>';

        html += '</li>';

        return html;
    }

    function renderRecordingGroups(page, groups) {

        if (groups.length) {
            $('#recordingGroups', page).show();
        } else {
            $('#recordingGroups', page).hide();
        }

        var html = '';

        html += '<ul data-role="listview" data-inset="true">';

        for (var i = 0, length = groups.length; i < length; i++) {

            html += getRecordingGroupHtml(groups[i]);
        }

        html += '</ul>';

        $('#recordingGroupItems', page).html(html).trigger('create');

        Dashboard.hideLoadingMsg();
    }

    function renderRecordings(elem, recordings) {

        var screenWidth = $(window).width();

        if (recordings.length) {
            elem.show();
        } else {
            elem.hide();
        }

        $('.recordingItems', elem).html(LibraryBrowser.getPosterViewHtml({
            
            items: recordings,
            shape: "auto",
            showTitle: true,
            showParentTitle: true,
            overlayText: screenWidth >= 600,
            coverImage: true

        }));
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvRecordings({

            userId: Dashboard.getCurrentUserId(),
            IsInProgress: true

        }).done(function (result) {

            renderRecordings($('#activeRecordings', page), result.Items);

        });

        ApiClient.getLiveTvRecordings({

            userId: Dashboard.getCurrentUserId(),
            limit: 12,
            IsInProgress: false

        }).done(function (result) {

            renderRecordings($('#latestRecordings', page), result.Items);

        });

        ApiClient.getLiveTvRecordingGroups({

            userId: Dashboard.getCurrentUserId()

        }).done(function (result) {

            renderRecordingGroups(page, result.Items);

        });
    }

    $(document).on('pagebeforeshow', "#liveTvRecordingsPage", function () {

        var page = this;

        reload(page);

    });

})(jQuery, document);
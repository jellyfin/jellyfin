(function ($, document, apiClient) {

    function deleteRecording(page, id) {

        Dashboard.confirm("Are you sure you wish to delete this recording?", "Confirm Recording Deletion", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.deleteLiveTvRecording(id).done(function () {

                    Dashboard.alert('Recording deleted');

                    reload(page);
                });
            }

        });
    }
    
    function loadRecordings(page, elem, groupId) {

        var contentElem = $('.recordingList', elem).html('<div class="circle"></div><div class="circle1"></div>');

        apiClient.getLiveTvRecordings({

            userId: Dashboard.getCurrentUserId(),
            groupId: groupId

        }).done(function (result) {

            renderRecordings(page, contentElem, result.Items);

        });
    }

    function getRecordingGroupHtml(group) {

        var html = '';

        html += '<div data-role="collapsible" class="recordingGroupCollapsible" data-recordinggroupid="' + group.Id + '" style="margin-top:1em" data-mini="true">';

        html += '<h3>' + group.Name + '</h3>';

        html += '<div class="recordingList">';
        html += '</div>';

        html += '</div>';

        return html;
    }

    function renderRecordingGroups(page, groups) {

        var html = '';

        for (var i = 0, length = groups.length; i < length; i++) {

            html += getRecordingGroupHtml(groups[i]);
        }

        var elem = $('#items', page).html(html).trigger('create');

        $('.recordingGroupCollapsible', elem).on('collapsibleexpand.lazyload', function () {

            $(this).off('collapsibleexpand.lazyload');

            var groupId = this.getAttribute('data-recordinggroupid');

            loadRecordings(page, this, groupId);
        });

        Dashboard.hideLoadingMsg();
    }

    function renderRecordings(page, elem, recordings) {

        var html = '';

        html += '<ul data-role="listview" data-split-icon="delete" data-inset="false">';

        for (var i = 0, length = recordings.length; i < length; i++) {

            var recording = recordings[i];

            html += '<li><a href="livetvrecording.html?id=' + recording.Id + '">';

            html += '<h3>';
            html += recording.EpisodeTitle || recording.Name;
            html += '</h3>';

            var startDate = recording.StartDate;

            try {

                startDate = parseISO8601Date(startDate, { toLocal: true });

            } catch (err) {

            }

            var minutes = recording.RunTimeTicks / 600000000;

            minutes = minutes || 1;

            html += '<p>';
            html += startDate.toLocaleDateString();
            html += '&nbsp; &#8226; &nbsp;' + Math.round(minutes) + 'min';
            html += '</p>';

            if (recording.Status !== 'Completed') {
                html += '<p class="ui-li-aside"><span style="color:red;">' + recording.StatusName + '</span></p>';
            }

            html += '</a>';

            html += '<a href="#" class="btnDeleteRecording" data-recordingid="' + recording.Id + '">Delete</a>';

            html += '</li>';
        }

        html += '</ul>';

        elem.html(html).trigger('create');

        $('.btnDeleteRecording', elem).on('click', function () {

            var recordingId = this.getAttribute('data-recordingid');

            deleteRecording(page, recordingId);
        });

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        apiClient.getLiveTvRecordingGroups({

            userId: Dashboard.getCurrentUserId()

        }).done(function (result) {

            renderRecordingGroups(page, result.Items);

        });
    }

    $(document).on('pagebeforeshow', "#liveTvRecordingsPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvRecordingsPage", function () {

        var page = this;

        $('.recordingGroupCollapsible', page).off('collapsibleexpand.lazyload');
    });

})(jQuery, document, ApiClient);
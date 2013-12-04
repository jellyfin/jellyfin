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

    function renderRecordings(page, recordings) {

        var html = '';

        for (var i = 0, length = recordings.length; i < length; i++) {

            var recording = recordings[i];

            html += '<tr>';

            html += '<td class="desktopColumn">';
            html += '<button data-recordingid="' + recording.Id + '" class="btnDeleteRecording" type="button" data-icon="delete" data-inline="true" data-mini="true" data-iconpos="notext">Delete</button>';
            html += '</td>';

            html += '<td>';
            html += '<a href="livetvrecording.html?id=' + recording.Id + '">';
            html += recording.Name;
            
            if (recording.EpisodeTitle) {
                html += "<br/>" + recording.EpisodeTitle;
            }
            html += '</a>';
            html += '</td>';

            html += '<td class="desktopColumn">';
            if (recording.ChannelId) {
                html += '<a href="livetvchannel.html?id=' + recording.ChannelId + '">' + recording.ChannelName + '</a>';
            }
            html += '</td>';

            var startDate = recording.StartDate;

            try {

                startDate = parseISO8601Date(startDate, { toLocal: true });

            } catch (err) {

            }

            html += '<td>' + startDate.toLocaleDateString() + '</td>';

            html += '<td>' + LiveTvHelpers.getDisplayTime(recording.StartDate) + '</td>';

            var minutes = recording.DurationMs / 60000;

            html += '<td class="tabletColumn">' + minutes.toFixed(0) + ' mins</td>';

            html += '<td class="tabletColumn">' + (recording.Status || '') + '</td>';

            html += '</tr>';
        }

        var elem = $('#table-column-toggle tbody', page).html(html).trigger('create');

        $('.btnDeleteRecording', elem).on('click', function () {

            var recordingId = this.getAttribute('data-recordingid');

            deleteRecording(page, recordingId);
        });

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        apiClient.getLiveTvRecordings().done(function (result) {

            renderRecordings(page, result.Items);

        });
    }

    $(document).on('pagebeforeshow', "#liveTvRecordingsPage", function () {

        var page = this;

        reload(page);
    });

})(jQuery, document, ApiClient);
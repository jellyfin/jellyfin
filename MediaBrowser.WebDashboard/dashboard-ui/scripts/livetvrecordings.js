(function ($, document, apiClient) {

    function playRecording(page, id) {

    }

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

        var cssClass = "detailTable";

        html += '<div class="detailTableContainer"><table class="' + cssClass + '">';

        html += '<tr>';

        html += '<th>&nbsp;</th>';
        html += '<th>Name</th>';
        html += '<th>Channel</th>';
        html += '<th>Date</th>';
        html += '<th>Start</th>';
        html += '<th>End</th>';
        html += '<th>Status</th>';

        html += '</tr>';

        for (var i = 0, length = recordings.length; i < length; i++) {

            var recording = recordings[i];

            html += '<tr>';

            html += '<td>';
            html += '<button data-recordingid="' + recording.Id + '" class="btnPlayRecording" type="button" data-icon="play" data-inline="true" data-mini="true" data-iconpos="notext">Play</button>';
            html += '<button data-recordingid="' + recording.Id + '" class="btnDeleteRecording" type="button" data-icon="delete" data-inline="true" data-mini="true" data-iconpos="notext">Delete</button>';
            html += '</td>';

            html += '<td>' + (recording.Name || '') + '</td>';

            html += '<td>';
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

            html += '<td>' + LiveTvHelpers.getDisplayTime(recording.EndDate) + '</td>';

            html += '<td>' + (recording.Status || '') + '</td>';

            html += '</tr>';
        }

        html += '</table></div>';

        var elem = $('#items', page).html(html).trigger('create');

        $('.btnPlayRecording', elem).on('click', function () {

            var recordingId = this.getAttribute('data-recordingid');

            playRecording(page, recordingId);
        });

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
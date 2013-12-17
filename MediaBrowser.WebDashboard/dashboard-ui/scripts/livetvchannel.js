(function ($, document, apiClient) {

    var currentItem;
    var programs;

    function cancelRecording(page, id) {

    }

    function scheduleRecording(page, id) {

    }

    function renderPrograms(page, result) {

        var html = '';

        var cssClass = "detailTable";

        html += '<div class="detailTableContainer"><table class="' + cssClass + '">';

        html += '<tr>';

        html += '<th class="tabletColumn">&nbsp;</th>';
        html += '<th>Date</th>';
        html += '<th>Start</th>';
        html += '<th class="tabletColumn">End</th>';
        html += '<th>Name</th>';
        html += '<th class="desktopColumn">Genre</th>';

        html += '</tr>';

        for (var i = 0, length = result.Items.length; i < length; i++) {

            var program = result.Items[i];

            html += '<tr>';

            html += '<td class="tabletColumn">';

            if (program.RecordingId) {
                html += '<button data-recordingid="' + program.RecordingId + '" class="btnCancelRecording" type="button" data-icon="delete" data-inline="true" data-mini="true" data-iconpos="notext">Cancel</button>';
            } else {
                html += '<button data-recordingid="' + program.RecordingId + '" class="btnScheduleRecording" type="button" data-icon="facetime-video" data-inline="true" data-mini="true" data-theme="b" data-iconpos="notext">Record</button>';
            }

            html += '</td>';

            var startDate = program.StartDate;

            try {

                startDate = parseISO8601Date(startDate, { toLocal: true });

            } catch (err) {

            }

            html += '<td>' + startDate.toLocaleDateString() + '</td>';

            html += '<td>' + LiveTvHelpers.getDisplayTime(program.StartDate) + '</td>';

            html += '<td class="tabletColumn">' + LiveTvHelpers.getDisplayTime(program.EndDate) + '</td>';

            html += '<td>';

            if (program.Name) {
                //html += '<a href="livetvprogram.html?id=' + program.Id + '">';
                html += program.Name;
                //html += '</a>';
            }

            html += '</td>';

            html += '<td class="desktopColumn">' + program.Genres.join(' / ') + '</td>';

            html += '</tr>';
        }

        html += '</table></div>';

        var elem = $('#programList', page).html(html).trigger('create');

        $('.btnCancelRecording', elem).on('click', function () {

            var recordingId = this.getAttribute('data-recordingid');

            cancelRecording(page, recordingId);
        });

        $('.btnScheduleRecording', elem).on('click', function () {

            var recordingId = this.getAttribute('data-recordingid');

            scheduleRecording(page, recordingId);
        });
    }

    function loadPrograms(page) {

        ApiClient.getLiveTvPrograms({

            ChannelIds: currentItem.Id,
            UserId: Dashboard.getCurrentUserId()

        }).done(function (result) {

            renderPrograms(page, result);
            programs = result.Items;

            Dashboard.hideLoadingMsg();
        });
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvChannel(getParameterByName('id')).done(function (item) {

            currentItem = item;

            var name = item.Name;

            $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

            Dashboard.setPageTitle(name);

            $('.itemName', page).html(name);
            $('.itemChannelNumber', page).html(item.Number);

            $('.userDataIcons', page).html(LibraryBrowser.getUserDataIconsHtml(item));

            if (ApiClient.isWebSocketOpen()) {

                var vals = [item.Type, item.Id, item.Name];

                vals.push('livetv');

                ApiClient.sendWebSocketMessage("Context", vals.join('|'));
            }

            if (MediaPlayer.canPlay(item)) {
                $('#playButtonContainer', page).show();
            } else {
                $('#playButtonContainer', page).hide();
            }

            Dashboard.getCurrentUser().done(function (user) {

                if (user.Configuration.IsAdministrator && item.LocationType !== "Offline") {
                    $('#editButtonContainer', page).show();
                } else {
                    $('#editButtonContainer', page).hide();
                }

            });

            loadPrograms(page);

        });
    }

    window.LiveTvHelpers = {

        getDisplayTime: function (date) {

            try {

                date = parseISO8601Date(date, { toLocal: true });

            } catch (err) {
                return date;
            }

            date = date.toLocaleTimeString();

            date = date.replace('0:00', '0');

            return date;
        }

    };

    $(document).on('pageinit', "#liveTvChannelPage", function () {

        var page = this;

        $('#btnPlay', page).on('click', function () {
            var userdata = currentItem.UserData || {};
            LibraryBrowser.showPlayMenu(this, currentItem.Name, currentItem.Type, currentItem.MediaType, userdata.PlaybackPositionTicks);
        });

        $('#btnRemote', page).on('click', function () {

            RemoteControl.showMenuForItem({ item: currentItem, context: 'livetv' });
        });

        $('#btnEdit', page).on('click', function () {

            Dashboard.navigate("edititemmetadata.html?channelid=" + currentItem.Id);
        });

    }).on('pageshow', "#liveTvChannelPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvChannelPage", function () {

        currentItem = null;
        programs = null;
    });

})(jQuery, document, ApiClient);
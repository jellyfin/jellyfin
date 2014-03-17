(function ($, document, apiClient) {

    var currentItem;

    function deleteRecording() {

        Dashboard.confirm("Are you sure you wish to delete this recording?", "Confirm Recording Deletion", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.deleteLiveTvRecording(currentItem.Id).done(function () {

                    Dashboard.alert('Recording deleted');

                    Dashboard.navigate('livetvrecordings.html');
                });
            }

        });
    }
    
    function play() {
        
        var userdata = currentItem.UserData || {};

        var mediaType = currentItem.MediaType;

        LibraryBrowser.showPlayMenu(this, currentItem.Id, currentItem.Type, mediaType, userdata.PlaybackPositionTicks);
    }

    function renderRecording(page, item) {

        currentItem = item;
        var context = 'livetv';

        var name = item.Name;

        $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

        Dashboard.setPageTitle(name);

        $('.itemName', page).html(name);

        $('.itemEpisodeName', page).html(item.EpisodeTitle || '');

        $('.itemCommunityRating', page).html(LibraryBrowser.getRatingHtml(item));

        $('.userDataIcons', page).html(LibraryBrowser.getUserDataIconsHtml(item));

        LibraryBrowser.renderGenres($('.itemGenres', page), item, context);
        LibraryBrowser.renderOverview($('.itemOverview', page), item);
        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        LiveTvHelpers.renderMiscProgramInfo($('.miscTvProgramInfo', page), item);

        if (ApiClient.isWebSocketOpen()) {

            var vals = [item.Type, item.Id, item.Name];

            vals.push('livetv');

            ApiClient.sendWebSocketMessage("Context", vals.join('|'));
        }

        $('.recordingStatus', page).html('Status:&nbsp;&nbsp;&nbsp;' + item.Status);

        Dashboard.getCurrentUser().done(function (user) {

            if (MediaPlayer.canPlay(item, user)) {
                $('#playButtonContainer', page).show();
            } else {
                $('#playButtonContainer', page).hide();
            }

            if (user.Configuration.IsAdministrator && item.LocationType !== "Offline") {
                $('#deleteButtonContainer', page).show();
            } else {
                $('#deleteButtonContainer', page).hide();
            }

        });

        LiveTvHelpers.renderOriginalAirDate($('.airDate', page), item);

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        apiClient.getLiveTvRecording(id, Dashboard.getCurrentUserId()).done(function (result) {

            renderRecording(page, result);

        });
    }

    $(document).on('pageinit', "#liveTvRecordingPage", function () {

        var page = this;

        $('#btnDelete', page).on('click', deleteRecording);
        $('#btnPlay', page).on('click', play);

        $('#btnRemote', page).on('click', function () {

            RemoteControl.showMenuForItem({

                item: currentItem,
                context: 'livetv'
            });
        });

    }).on('pagebeforeshow', "#liveTvRecordingPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvRecordingPage", function () {

        currentItem = null;
    });

})(jQuery, document, ApiClient);
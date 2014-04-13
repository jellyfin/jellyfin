(function ($, document, apiClient) {

    var currentItem;

    function deleteTimer(page, id) {

        Dashboard.confirm("Are you sure you wish to cancel this recording?", "Confirm Recording Cancellation", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).done(function () {

                    Dashboard.alert('Recording cancelled.');

                    reload(page);
                });
            }

        });
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvProgram(getParameterByName('id'), Dashboard.getCurrentUserId()).done(function (item) {

            var context = 'livetv';
            currentItem = item;

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

            $(page).trigger('displayingitem', [{

                item: item,
                context: 'livetv'
            }]);

            if (item.TimerId) {
                $('#cancelRecordingButtonContainer', page).show();
            } else {
                $('#cancelRecordingButtonContainer', page).hide();
            }

            if (!item.TimerId && !item.SeriesTimerId) {
                $('#recordButtonContainer', page).show();
            } else {
                $('#recordButtonContainer', page).hide();
            }

            var startDateLocal = parseISO8601Date(item.StartDate, { toLocal: true });
            var endDateLocal = parseISO8601Date(item.EndDate, { toLocal: true });
            var now = new Date();
            
            if (now >= startDateLocal && now < endDateLocal) {
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

            LiveTvHelpers.renderOriginalAirDate($('.airDate', page), item);

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageinit', "#liveTvProgramPage", function () {

        var page = this;

        $('#btnRecord', page).on('click', function() {

            var id = getParameterByName('id');
            
            Dashboard.navigate('livetvnewrecording.html?programid=' + id);

        });

        $('#btnPlay', page).on('click', function () {

            ApiClient.getLiveTvChannel(currentItem.ChannelId, Dashboard.getCurrentUserId()).done(function (channel) {
                
                var userdata = channel.UserData || {};

                LibraryBrowser.showPlayMenu(this, channel.Id, channel.Type, false, channel.MediaType, userdata.PlaybackPositionTicks);
            });
        });

        $('#btnCancelRecording', page).on('click', function () {

            deleteTimer(page, currentItem.TimerId);
        });

    }).on('pageshow', "#liveTvProgramPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvProgramPage", function () {

        currentItem = null;
    });

})(jQuery, document, ApiClient);
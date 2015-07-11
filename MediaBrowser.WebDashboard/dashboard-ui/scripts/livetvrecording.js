(function ($, document) {

    var currentItem;

    function deleteRecording() {

        Dashboard.confirm(Globalize.translate('MessageConfirmRecordingDeletion'), Globalize.translate('HeaderConfirmRecordingDeletion'), function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.deleteLiveTvRecording(currentItem.Id).done(function () {

                    Dashboard.alert(Globalize.translate('MessageRecordingDeleted'));

                    Dashboard.navigate('livetvrecordings.html');
                });
            }

        });
    }
    
    function play() {
        
        var userdata = currentItem.UserData || {};

        var mediaType = currentItem.MediaType;

        LibraryBrowser.showPlayMenu(null, currentItem.Id, currentItem.Type, false, mediaType, userdata.PlaybackPositionTicks);
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
        LibraryBrowser.renderOverview(page.querySelectorAll('.itemOverview'), item);
        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        LiveTvHelpers.renderMiscProgramInfo($('.miscTvProgramInfo', page), item);

        $(page).trigger('displayingitem', [{

            item: item,
            context: 'livetv'
        }]);

        $('.recordingStatus', page).html('Status:&nbsp;&nbsp;&nbsp;' + item.Status);

        if (MediaController.canPlay(item)) {
            $('#playButtonContainer', page).show();
        } else {
            $('#playButtonContainer', page).hide();
        }

        if (item.CanDelete) {
            $('#deleteButtonContainer', page).show();
        } else {
            $('#deleteButtonContainer', page).hide();
        }

        LiveTvHelpers.renderOriginalAirDate($('.airDate', page), item);

        Dashboard.hideLoadingMsg();

        Dashboard.getCurrentUser().done(function(user) {

            if (SyncManager.isAvailable(item, user)) {
                $('.btnSync', page).removeClass('hide');
            } else {
                $('.btnSync', page).addClass('hide');
            }
        });
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        ApiClient.getLiveTvRecording(id, Dashboard.getCurrentUserId()).done(function (result) {

            renderRecording(page, result);

        });
    }

    $(document).on('pageinitdepends', "#liveTvRecordingPage", function () {

        var page = this;

        $('.btnDelete', page).on('click', deleteRecording);
        $('.btnPlay', page).on('click', play);

        $('.btnSync', page).on('click', function () {

            SyncManager.showMenu({
                items: [currentItem]
            });
        });

    }).on('pagebeforeshowready', "#liveTvRecordingPage", function () {

        var page = this;

        reload(page);

    }).on('pagebeforehide', "#liveTvRecordingPage", function () {

        currentItem = null;
    });

})(jQuery, document);
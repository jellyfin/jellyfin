(function ($, document, apiClient) {

    var currentItem;

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

            if (item.CommunityRating) {
                $('.itemCommunityRating', page).html(LibraryBrowser.getRatingHtml(item)).show();
            } else {
                $('.itemCommunityRating', page).hide();
            }

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

            $('#recordButtonContainer', page).show();

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

    }).on('pageshow', "#liveTvProgramPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvProgramPage", function () {

        currentItem = null;
    });

})(jQuery, document, ApiClient);
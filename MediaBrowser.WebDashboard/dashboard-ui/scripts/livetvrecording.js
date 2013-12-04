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

    function renderRecording(page, item) {

        currentItem = item;

        var name = item.Name;

        $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

        Dashboard.setPageTitle(name);

        $('.itemName', page).html(name);
        $('.itemChannelNumber', page).html(item.Number);

        $('.userDataIcons', page).html(LibraryBrowser.getUserDataIconsHtml(item));

        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

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
                $('#deleteButtonContainer', page).show();
            } else {
                $('#deleteButtonContainer', page).hide();
            }

        });

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        apiClient.getLiveTvRecording(id).done(function (result) {

            renderRecording(page, result);

        });
    }

    $(document).on('pageinit', "#liveTvRecordingPage", function () {

        var page = this;

        $('#btnDelete', page).on('click', deleteRecording);

    }).on('pagebeforeshow', "#liveTvRecordingPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvRecordingPage", function () {

        currentItem = null;
    });

})(jQuery, document, ApiClient);
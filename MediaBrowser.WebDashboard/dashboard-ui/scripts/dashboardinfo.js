(function ($, document, window) {

    function loadPage(page, systemInfo) {

        $('#cachePath', page).html(systemInfo.CachePath);
        $('#logPath', page).html(systemInfo.LogPath);
        $('#imagesByNamePath', page).html(systemInfo.ItemsByNamePath);
        $('#transcodingTemporaryPath', page).html(systemInfo.TranscodingTempPath);

        if (systemInfo.WanAddress) {

            var externalUrl = systemInfo.WanAddress + "/mediabrowser";

            $('.externalUrl', page).html('Remote access: <a href="' + externalUrl + '" target="_blank">' + externalUrl + '</a>').show().trigger('create');
        } else {
            $('.externalUrl', page).hide();
        }

        Dashboard.hideLoadingMsg();
    }

    $(document).on('pageshow', "#dashboardInfoPage", function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getSystemInfo().done(function (systemInfo) {

            loadPage(page, systemInfo);

        });

    });

})(jQuery, document, window);

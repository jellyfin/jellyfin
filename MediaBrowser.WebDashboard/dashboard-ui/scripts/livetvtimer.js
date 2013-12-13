(function ($, document, apiClient) {

    var currentItem;

    function renderTimer(page, item) {

        currentItem = item;

        $('.program', page).html(item.Name);
        $('.channel', page).html(item.ChannelName);
        $('.overview', page).html(item.Overview || '');

        $('#txtRequestedPrePaddingSeconds', page).val(item.RequestedPrePaddingSeconds);
        $('#txtRequestedPostPaddingSeconds', page).val(item.RequestedPostPaddingSeconds);
        $('#txtRequiredPrePaddingSeconds', page).val(item.RequiredPrePaddingSeconds);
        $('#txtRequiredPostPaddingSeconds', page).val(item.RequiredPostPaddingSeconds);

        Dashboard.hideLoadingMsg();
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        apiClient.getLiveTvTimer(id).done(function (result) {

            renderTimer(page, result);

        });
    }

    $(document).on('pageinit', "#liveTvTimerPage", function () {

        var page = this;

    }).on('pagebeforeshow', "#liveTvTimerPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvTimerPage", function () {

        currentItem = null;
    });

})(jQuery, document, ApiClient);
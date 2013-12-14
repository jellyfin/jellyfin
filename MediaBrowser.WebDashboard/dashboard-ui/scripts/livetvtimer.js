(function ($, document, apiClient) {

    var currentItem;

    function deleteTimer(page, id) {

        Dashboard.confirm("Are you sure you wish to cancel this timer?", "Confirm Timer Cancellation", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).done(function () {

                    Dashboard.alert('Timer deleted');

                    reload(page);
                });
            }

        });
    }

    function renderTimer(page, item) {

        currentItem = item;

        $('.program', page).html(item.Name);
        $('.channel', page).html(item.ChannelName);
        $('.overview', page).html(item.Overview || '');

        $('#txtRequestedPrePaddingSeconds', page).val(item.RequestedPrePaddingSeconds);
        $('#txtRequestedPostPaddingSeconds', page).val(item.RequestedPostPaddingSeconds);
        $('#txtRequiredPrePaddingSeconds', page).val(item.RequiredPrePaddingSeconds);
        $('#txtRequiredPostPaddingSeconds', page).val(item.RequiredPostPaddingSeconds);

        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));
        $('.status', page).html('Status:&nbsp;&nbsp;&nbsp;' + item.Status);

        if (item.SeriesTimerId) {

            $('.seriesTimerLink', page).html('<a href="livetvseriestimer.html?id=' + item.SeriesTimerId + '">View Series</a>').show().trigger('create');

        } else {
            $('.seriesTimerLink', page).hide();
        }

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

        $('#btnCancelTimer', page).on('click', function () {

            deleteTimer(page, currentItem.Id);

        });

    }).on('pagebeforeshow', "#liveTvTimerPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvTimerPage", function () {

        currentItem = null;
    });

})(jQuery, document, ApiClient);
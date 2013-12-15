(function (window, $, document, apiClient) {

    var currentItem;

    function deleteTimer(page, id) {

        Dashboard.confirm("Are you sure you wish to cancel this series?", "Confirm Series Timer Cancellation", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvSeriesTimer(id).done(function () {

                    Dashboard.alert('Timer cancelled.');

                    reload(page);
                });
            }

        });
    }

    function renderTimer(page, item) {

        currentItem = item;

        $('.itemName', page).html(item.Name);
        $('.channel', page).html('<a href="livetvchannel.html?id=' + item.ChannelId + '">' + item.ChannelName + '</a>').trigger('create');
        $('.overview', page).html(item.Overview || '');

        $('#txtRequestedPrePaddingSeconds', page).val(item.RequestedPrePaddingSeconds);
        $('#txtRequestedPostPaddingSeconds', page).val(item.RequestedPostPaddingSeconds);
        $('#txtRequiredPrePaddingSeconds', page).val(item.RequiredPrePaddingSeconds);
        $('#txtRequiredPostPaddingSeconds', page).val(item.RequiredPostPaddingSeconds);
        $('#selectPriority', page).val(item.Priority);

        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        apiClient.getLiveTvSeriesTimer(currentItem.Id).done(function (item) {

            item.RequestedPrePaddingSeconds = $('#txtRequestedPrePaddingSeconds', form).val();
            item.RequestedPostPaddingSeconds = $('#txtRequestedPostPaddingSeconds', form).val();
            item.RequiredPrePaddingSeconds = $('#txtRequiredPrePaddingSeconds', form).val();
            item.RequiredPostPaddingSeconds = $('#txtRequiredPostPaddingSeconds', form).val();
            item.Priority = $('#selectPriority', form).val();

            ApiClient.updateLiveTvSeriesTimer(item).done(function () {
                Dashboard.alert('Timer Saved');
            });
        });

        // Disable default form submission
        return false;

    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        apiClient.getLiveTvTimer(id).done(function (result) {

            renderTimer(page, result);

        });
    }

    $(document).on('pageinit', "#liveTvSeriesTimerPage", function () {

        var page = this;

        $('#btnCancelTimer', page).on('click', function () {

            deleteTimer(page, currentItem.Id);

        });

    }).on('pagebeforeshow', "#liveTvSeriesTimerPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#liveTvSeriesTimerPage", function () {

        currentItem = null;
    });

    function liveTvSeriesTimerPage() {

        var self = this;

        self.onSubmit = onSubmit;
    }

    window.LiveTvSeriesTimerPage = new liveTvSeriesTimerPage();

})(window, jQuery, document, ApiClient);
(function (window, $, document, apiClient) {

    var currentItem;

    function deleteTimer(page, id) {

        Dashboard.confirm("Are you sure you wish to cancel this timer?", "Confirm Timer Cancellation", function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).done(function () {

                    Dashboard.alert('Timer cancelled.');

                    reload(page);
                });
            }

        });
    }

    function renderTimer(page, item) {

        currentItem = item;

        $('.program', page).html(item.Name);
        $('.channel', page).html('<a href="livetvchannel.html?id=' + item.ChannelId + '">' + item.ChannelName + '</a>').trigger('create');
        $('.overview', page).html(item.Overview || '');

        $('#txtPrePaddingSeconds', page).val(item.PrePaddingSeconds);
        $('#txtPostPaddingSeconds', page).val(item.PostPaddingSeconds);
        $('#chkPrePaddingRequired', page).checked(item.IsPrePaddingRequired).checkboxradio('refresh');
        $('#chkPostPaddingRequired', page).checked(item.IsPostPaddingRequired).checkboxradio('refresh');

        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));
        $('.status', page).html('Status:&nbsp;&nbsp;&nbsp;' + item.Status);

        if (item.SeriesTimerId) {

            $('.seriesTimerLink', page).html('<a href="livetvseriestimer.html?id=' + item.SeriesTimerId + '">View Series</a>').show().trigger('create');

        } else {
            $('.seriesTimerLink', page).hide();
        }

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        apiClient.getLiveTvTimer(currentItem.Id).done(function (item) {

            item.PrePaddingSeconds = $('#txtPrePaddingSeconds', form).val();
            item.PostPaddingSeconds = $('#txtPostPaddingSeconds', form).val();
            item.IsPrePaddingRequired = $('#chkPrePaddingRequired', form).checked();
            item.IsPostPaddingRequired = $('#chkPostPaddingRequired', form).checked();

            ApiClient.updateLiveTvTimer(item).done(function () {
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

    function liveTvTimerPage() {

        var self = this;

        self.onSubmit = onSubmit;
    }

    window.LiveTvTimerPage = new liveTvTimerPage();

})(window, jQuery, document, ApiClient);
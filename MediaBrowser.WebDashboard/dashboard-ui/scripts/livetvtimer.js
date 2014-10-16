(function (window, $, document) {

    var currentItem;

    function deleteTimer(page, id) {

        Dashboard.confirm(Globalize.translate('MessageConfirmRecordingCancellation'), Globalize.translate('HeaderConfirmRecordingCancellation'), function (result) {

            if (result) {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).done(function () {

                    Dashboard.alert(Globalize.translate('MessageRecordingCancelled'));

                    Dashboard.navigate('livetvtimers.html');
                });
            }

        });
    }

    function renderTimer(page, item) {

        var context = 'livetv';
        currentItem = item;

        var programInfo = item.ProgramInfo || {};

        $('.itemName', page).html(item.Name);

        $('.itemEpisodeName', page).html(programInfo.EpisodeTitle || '');

        $('.itemCommunityRating', page).html(LibraryBrowser.getRatingHtml(programInfo));

        LibraryBrowser.renderGenres($('.itemGenres', page), programInfo, context);
        LibraryBrowser.renderOverview($('.itemOverview', page), programInfo);

        if (programInfo.ImageTags && programInfo.ImageTags.Primary) {

            var imgUrl = ApiClient.getScaledImageUrl(programInfo.Id, {
                maxWidth: 200,
                maxHeight: 200,
                tag: programInfo.ImageTags.Primary,
                type: "Primary"
            });

            $('.timerPageImageContainer', page).css("display", "inline-block")
                .html('<img src="' + imgUrl + '" style="max-width:200px;max-height:200px;" />');

        } else {
            $('.timerPageImageContainer', page).hide();
        }

        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        LiveTvHelpers.renderMiscProgramInfo($('.miscTvProgramInfo', page), programInfo);

        $('#txtPrePaddingSeconds', page).val(item.PrePaddingSeconds / 60);
        $('#txtPostPaddingSeconds', page).val(item.PostPaddingSeconds / 60);
        $('#chkPrePaddingRequired', page).checked(item.IsPrePaddingRequired).checkboxradio('refresh');
        $('#chkPostPaddingRequired', page).checked(item.IsPostPaddingRequired).checkboxradio('refresh');

        $('.timerStatus', page).html('Status:&nbsp;&nbsp;&nbsp;' + item.Status);

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getLiveTvTimer(currentItem.Id).done(function (item) {

            item.PrePaddingSeconds = $('#txtPrePaddingSeconds', form).val() * 60;
            item.PostPaddingSeconds = $('#txtPostPaddingSeconds', form).val() * 60;
            item.IsPrePaddingRequired = $('#chkPrePaddingRequired', form).checked();
            item.IsPostPaddingRequired = $('#chkPostPaddingRequired', form).checked();

            ApiClient.updateLiveTvTimer(item).done(function () {
                Dashboard.alert(Globalize.translate('MessageRecordingSaved'));
            });
        });

        // Disable default form submission
        return false;

    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        ApiClient.getLiveTvTimer(id).done(function (result) {

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

})(window, jQuery, document);
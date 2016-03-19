define(['jQuery'], function ($) {

    var currentItem;

    function deleteTimer(page, id) {

        require(['confirm'], function (confirm) {

            confirm(Globalize.translate('MessageConfirmRecordingCancellation'), Globalize.translate('HeaderConfirmRecordingCancellation')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).then(function () {

                    require(['toast'], function (toast) {
                        toast(Globalize.translate('MessageRecordingCancelled'));
                    });

                    Dashboard.navigate('livetv.html');
                });
            });
        });
    }

    function renderTimer(page, item) {

        currentItem = item;

        var programInfo = item.ProgramInfo || {};

        $('.itemName', page).html(item.Name);

        $('.itemEpisodeName', page).html(programInfo.EpisodeTitle || '');

        $('.itemCommunityRating', page).html(LibraryBrowser.getRatingHtml(programInfo));

        LibraryBrowser.renderGenres($('.itemGenres', page), programInfo);
        LibraryBrowser.renderOverview(page.querySelectorAll('.itemOverview'), programInfo);

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

        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(programInfo));

        $('#txtPrePaddingMinutes', page).val(item.PrePaddingSeconds / 60);
        $('#txtPostPaddingMinutes', page).val(item.PostPaddingSeconds / 60);

        if (item.Status == 'New') {
            $('.timerStatus', page).hide();
        } else {
            $('.timerStatus', page).show().html('Status:&nbsp;&nbsp;&nbsp;' + item.Status);
        }

        Dashboard.hideLoadingMsg();
    }

    function onSubmit() {

        Dashboard.showLoadingMsg();

        var form = this;

        ApiClient.getLiveTvTimer(currentItem.Id).then(function (item) {

            item.PrePaddingSeconds = $('#txtPrePaddingMinutes', form).val() * 60;
            item.PostPaddingSeconds = $('#txtPostPaddingMinutes', form).val() * 60;

            ApiClient.updateLiveTvTimer(item).then(function () {
                Dashboard.hideLoadingMsg();
                require(['toast'], function (toast) {
                    toast(Globalize.translate('MessageRecordingSaved'));
                });
            });
        });

        // Disable default form submission
        return false;

    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        var id = getParameterByName('id');

        ApiClient.getLiveTvTimer(id).then(function (result) {

            renderTimer(page, result);

        });
    }

    $(document).on('pageinit', "#liveTvTimerPage", function () {

        var page = this;

        $('#btnCancelTimer', page).on('click', function () {

            deleteTimer(page, currentItem.Id);

        });

        $('.liveTvTimerForm').off('submit', onSubmit).on('submit', onSubmit);

    }).on('pagebeforeshow', "#liveTvTimerPage", function () {

        var page = this;

        reload(page);

    }).on('pagebeforehide', "#liveTvTimerPage", function () {

        currentItem = null;
    });

});
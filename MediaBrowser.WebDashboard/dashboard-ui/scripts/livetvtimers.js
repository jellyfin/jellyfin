define(['jQuery'], function ($) {

    function deleteTimer(page, id) {

        require(['confirm'], function (confirm) {

            confirm(Globalize.translate('MessageConfirmRecordingCancellation'), Globalize.translate('HeaderConfirmRecordingCancellation')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).then(function () {

                    require(['toast'], function (toast) {
                        toast(Globalize.translate('MessageRecordingCancelled'));
                    });

                    reload(page);
                });
            });
        });
    }

    function renderTimers(page, timers) {

        LiveTvHelpers.getTimersHtml(timers).then(function (html) {
            var elem = $('#items', page).html(html)[0];

            ImageLoader.lazyChildren(elem);

            $('.btnDeleteTimer', elem).on('click', function () {

                var id = this.getAttribute('data-timerid');

                deleteTimer(page, id);
            });

            Dashboard.hideLoadingMsg();
        });
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvTimers().then(function (result) {

            renderTimers(page, result.Items);
        });
    }

    window.LiveTvPage.renderTimersTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            reload(tabContent);
        }
    };

});
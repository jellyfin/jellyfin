(function ($, document) {

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        Sections.loadLatestChannelItems(page.querySelector('.latestItems'), Dashboard.getCurrentUserId()).always(function() {
            Dashboard.hideLoadingMsg();
        });
    }

    function loadTab(page, index) {

        switch (index) {

            case 0:
                reloadItems(page);
                break;
            default:
                break;
        }
    }

    $(document).on('pageinit', "#channelsPage", function () {

        var page = this;
        var pages = page.querySelector('neon-animated-pages');

        $(pages).on('tabchange', function () {
            loadTab(page, parseInt(this.selected));
        });

    });

})(jQuery, document);
(function ($, document) {

    function reloadItems(page) {

        Sections.loadLatestChannelItems(page.querySelector('.latestItems'), Dashboard.getCurrentUserId());
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

    $(document).on('pageinitdepends', "#channelsPage", function () {

        var page = this;
        var pages = page.querySelector('neon-animated-pages');

        $(pages).on('tabchange', function () {
            loadTab(page, parseInt(this.selected));
        });

    });

})(jQuery, document);
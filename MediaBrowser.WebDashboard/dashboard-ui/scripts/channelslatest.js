(function ($, document) {

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        Sections.loadLatestChannelItems(page.querySelector('.latestItems'), Dashboard.getCurrentUserId()).then(function() {
            Dashboard.hideLoadingMsg();
        }, function () {
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

    pageIdOn('pageinit', "channelsPage", function () {

        var page = this;
        var pages = page.querySelector('neon-animated-pages');

        pages.addEventListener('tabchange', function (e) {
            loadTab(page, parseInt(e.target.selected));
        });

    });

})(jQuery, document);
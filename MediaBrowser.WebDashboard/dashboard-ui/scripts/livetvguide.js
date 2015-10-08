(function ($, document) {

    window.LiveTvPage.initGuideTab = function (page, tabContent) {

    };

    window.LiveTvPage.renderGuideTab = function (page, tabContent) {

        if (page.guideInstance) {
            if (LibraryBrowser.needsRefresh(tabContent)) {
                page.guideInstance.refresh();
            }
        } else {
            require(['tvguide'], function (tvguide) {

                page.guideInstance = new tvguide({
                    element: tabContent,
                    enableHeadRoom: true,
                    enableHoverMenu: true
                });
            });
        }
    };

})(jQuery, document);
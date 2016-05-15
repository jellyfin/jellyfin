define(['tvguide'], function (tvguide) {

    window.LiveTvPage.initGuideTab = function (page, tabContent) {

    };

    window.LiveTvPage.renderGuideTab = function (page, tabContent) {

        if (!page.guideInstance) {

            page.guideInstance = new tvguide({
                element: tabContent
            });
        }
    };

});
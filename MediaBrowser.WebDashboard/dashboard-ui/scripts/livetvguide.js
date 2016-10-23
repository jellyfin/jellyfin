define(['tvguide'], function (tvguide) {
    'use strict';

    return function (view, params, tabContent) {

        var self = this;
        var guideInstance;
        self.renderTab = function () {
            if (!guideInstance) {
                guideInstance = new tvguide({
                    element: tabContent
                });
            }
        };
    };
});
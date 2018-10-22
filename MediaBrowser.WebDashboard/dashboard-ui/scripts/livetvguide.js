define(["tvguide"], function(tvguide) {
    "use strict";
    return function(view, params, tabContent) {
        var guideInstance, self = this;
        self.renderTab = function() {
            guideInstance || (guideInstance = new tvguide({
                element: tabContent,
                serverId: ApiClient.serverId()
            }))
        }, self.onShow = function() {
            guideInstance && guideInstance.resume()
        }, self.onHide = function() {
            guideInstance && guideInstance.pause()
        }
    }
});
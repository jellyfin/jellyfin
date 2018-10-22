define(["components/remotecontrol", "libraryMenu", "emby-button"], function(remotecontrolFactory, libraryMenu) {
    "use strict";
    return function(view, params) {
        var remoteControl = new remotecontrolFactory;
        remoteControl.init(view, view.querySelector(".remoteControlContent")), view.addEventListener("viewshow", function(e) {
            libraryMenu.setTransparentMenu(!0), remoteControl && remoteControl.onShow()
        }), view.addEventListener("viewbeforehide", function(e) {
            libraryMenu.setTransparentMenu(!1), remoteControl && remoteControl.destroy()
        })
    }
});
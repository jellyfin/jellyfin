define(['jQuery'], function ($) {

    pageIdOn('pageinit', "nowPlayingPage", function () {

        var page = this;

        require(['components/remotecontrol'], function (remotecontrolFactory) {
            page.remoteControl = new remotecontrolFactory();
            page.remoteControl.init(page.querySelector('.remoteControlContent'));
            page.remoteControl.onShow();
            page.remoteControlInitComplete = true;
        });
    });

    pageIdOn('pagebeforeshow', "nowPlayingPage", function () {

        var page = this;

        document.body.classList.add('hiddenViewMenuBar');
        document.body.classList.add('hiddenNowPlayingBar');

        if (page.remoteControl) {

            if (!page.remoteControlInitComplete) {
                page.remoteControlInitComplete = true;
            } else {
                page.remoteControl.onShow();
            }
        }
    });

    pageIdOn('pagebeforehide', "nowPlayingPage", function () {

        var page = this;

        if (page.remoteControl) {
            page.remoteControl.destroy();
        }
        document.body.classList.remove('hiddenViewMenuBar');
        document.body.classList.remove('hiddenNowPlayingBar');
    });

});
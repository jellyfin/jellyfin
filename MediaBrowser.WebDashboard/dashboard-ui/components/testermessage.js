(function () {

    function onPageShow() {

        if (!browserInfo.android) {
            return;
        }

        var msg;

        var settingsKey = "betatester";

        var expectedValue = new Date().toDateString() + "6";
        if (appStorage.getItem(settingsKey) == expectedValue) {
            return;
        }

        msg = 'At your convenience, please take a moment to visit the Emby Community and leave testing feedback related to this beta build. Your feedback will help us improve the release before it goes public. Thank you for being a part of the Emby beta test team.';

        msg += "<br/><br/>";
        msg += '<a href="http://emby.media/community/index.php?/topic/28144-android-mobile-25" target="_blank">Visit Emby community</a>';

        Dashboard.alert({
            message: msg,
            title: 'Hello Emby Beta Tester!',
            callback: function () {
                appStorage.setItem(settingsKey, expectedValue);
            }
        });

    }

    pageClassOn('pageshow', "homePage", onPageShow);

})();
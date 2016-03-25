define(['browser', 'appStorage'], function (browser, appStorage) {

    require(['css!devices/ie/ie.css']);
    var browserSwitchKey = "ieswitchbrowser";

    function getWeek(date) {

        var onejan = new Date(date.getFullYear(), 0, 1);
        return Math.ceil((((date - onejan) / 86400000) + onejan.getDay() + 1) / 4);
    }

    function onPageShow() {

        var expectedValue;
        var msg;

        if (navigator.userAgent.toLowerCase().indexOf('windows nt 10.') != -1) {

            expectedValue = new Date().toDateString() + "1";
            if (appStorage.getItem(browserSwitchKey) == expectedValue) {
                return;
            }

            msg = Globalize.translate('MessageTryMicrosoftEdge');

            msg += "<br/><br/>";
            msg += '<a href="https://www.microsoft.com/en-us/windows/microsoft-edge" target="_blank">' + Globalize.translate('ButtonLearnMore') + '</a>';

            Dashboard.alert({
                message: msg,
                title: Globalize.translate('HeaderTryMicrosoftEdge')
            });

        } else if (!browser.mobile) {

            expectedValue = getWeek(new Date()) + "_7";

            if (appStorage.getItem(browserSwitchKey) == expectedValue) {
                return;
            }

            if (!appStorage.getItem(browserSwitchKey)) {
                appStorage.setItem(browserSwitchKey, expectedValue);
                return;
            }

            msg = Globalize.translate('MessageTryModernBrowser');

            msg += "<br/><br/>";
            msg += '<a href="https://www.google.com/chrome" target="_blank">' + Globalize.translate('ButtonLearnMore') + '</a>';

            Dashboard.alert({
                message: msg,
                title: Globalize.translate('HeaderTryModernBrowser')
            });
        }

        appStorage.setItem(browserSwitchKey, expectedValue);
    }

    pageClassOn('pageshow', "libraryPage", onPageShow);
    pageClassOn('pageshow', "type-interior", onPageShow);
});
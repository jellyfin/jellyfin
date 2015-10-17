(function () {

    Dashboard.importCss('devices/ie/ie.css');

    function onPageShow() {

        var page = this;

        if (navigator.userAgent.toLowerCase().indexOf('Windows NT 10.') != -1) {

            var expectedValue = new Date().toDateString();
            if (appStorage.getItem("ieswitchtoedge") == expectedValue) {
                return;
            }

            appStorage.setItem("ieswitchtoedge", expectedValue);

            var msg = Globalize.translate('MessageTryMicrosoftEdge');

            msg += "<br/><br/>";
            msg += '<a href="https://www.microsoft.com/en-us/windows/microsoft-edge" target="_blank">' + Globalize.translate('ButtonLearnMore') + '</a>';

            Dashboard.alert({
                message: msg,
                title: Globalize.translate('HeaderTryMicrosoftEdge')
            });
        }
    }

    pageClassOn('pageshow', "libraryPage", onPageShow);
    pageClassOn('pageshow', "type-interior", onPageShow);

})();
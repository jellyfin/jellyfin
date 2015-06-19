(function () {

    function show(options) {

        // items
        // positionTo
        // showCancel
        // title

        var innerOptions = {
            'title': options.title,
            'buttonLabels': options.items.map(function (i) {
                return i.name;
            })
        };

        if (options.showCancel) {
            innerOptions.addCancelButtonWithLabel = Globalize.translate('ButtonCancel');
        }

        // Depending on the buttonIndex, you can now call shareViaFacebook or shareViaTwitter
        // of the SocialSharing plugin (https://github.com/EddyVerbruggen/SocialSharing-PhoneGap-Plugin)
        window.plugins.actionsheet.show(innerOptions, function (index) {

            if (options.callback) {

                if (index >= 1) {
                    options.callback(options.items[index - 1].id);
                }
            }
        });
    }

    window.ActionSheetElement = {
        show: show
    };
})();
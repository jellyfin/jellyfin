(function () {

    function show(options) {

        // items
        // positionTo
        // showCancel
        // title

        // If any items have an icon, give them all an icon just to make sure they're all lined up evenly
        var renderIcon = options.items.filter(function (o) {
            return o.ironIcon;
        }).length;

        if (renderIcon) {

            // iOS supports unicode icons
            if ($.browser.safari) {

                for (var i = 0, length = options.items.length; i < length; i++) {

                    var option = options.items[i];

                    switch (option.ironIcon) {

                        case 'check':
                            option.name = '\\u2713 ' + option.name;
                            break;
                        default:
                            option.name = '\\u2001 ' + option.name;
                            break;
                    }
                }
            }
        }

        var innerOptions = {
            'title': options.title,
            'buttonLabels': options.items.map(function (i) {
                return i.name;
            })
        };

        // Show cancel unless the caller explicitly set it to false
        if (options.showCancel !== false) {
            innerOptions.addCancelButtonWithLabel = Globalize.translate('ButtonCancel');
        }

        // Depending on the buttonIndex, you can now call shareViaFacebook or shareViaTwitter
        // of the SocialSharing plugin (https://github.com/EddyVerbruggen/SocialSharing-PhoneGap-Plugin)
        window.plugins.actionsheet.show(innerOptions, function (index) {

            if (options.callback) {

                // Results are 1-based
                if (index >= 1 && options.items.length >= index) {

                    options.callback(options.items[index - 1].id);
                }
            }
        });
    }

    window.ActionSheetElement = {
        show: show
    };
})();
(function () {

    function showMenu(options, successCallback, cancelCallback) {

        Dashboard.confirm(Globalize.translate('ButtonShareHelp'), Globalize.translate('HeaderConfirm'), function (confirmed) {

            if (!confirmed) {
                cancelCallback(options);
                return;
            }

            var shareInfo = options.share;

            window.plugins.socialsharing.share(shareInfo.Overview, shareInfo.Name, shareInfo.ImageUrl, shareInfo.Url, function () {

                successCallback(options);

            }, function () {

                cancelCallback(options);
            });
        });

    }

    window.SharingWidget = {
        showMenu: showMenu
    };


})();
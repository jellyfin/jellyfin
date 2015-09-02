(function () {

    function showMenu(options, successCallback, cancelCallback) {

        var shareInfo = options.share;

        window.plugins.socialsharing.share(shareInfo.Overview, shareInfo.Name, shareInfo.ImageUrl, shareInfo.Url, function () {

            successCallback(options);

        }, function () {

            cancelCallback(options);
        });
    }

    window.SharingWidget = {
        showMenu: showMenu
    };


})();
define([], function () {

    function onSharingSuccess(options) {

        console.log('share success. shareId: ' + options.share.Id);

    }

    function onSharingCancel(options) {

        var shareId = options.share.Id;

        console.log('share cancelled. shareId: ' + shareId);

        // Delete the share since it was cancelled
        ApiClient.ajax({

            type: 'DELETE',
            url: ApiClient.getUrl('Social/Shares/' + shareId)

        });
    }

    function showMenu(userId, itemId) {

        Dashboard.showLoadingMsg();

        require(['sharingwidget'], function (SharingWidget) {

            ApiClient.ajax({
                type: 'POST',
                url: ApiClient.getUrl('Social/Shares', {

                    ItemId: itemId,
                    UserId: userId
                }),
                dataType: "json"

            }).then(function (share) {

                var options = {
                    share: share
                };

                Dashboard.hideLoadingMsg();
                SharingWidget.showMenu(options, onSharingSuccess, onSharingCancel);

            }, function () {

                Dashboard.hideLoadingMsg();
            });
        });
    }

    window.SharingManager = {
        showMenu: showMenu
    };

});
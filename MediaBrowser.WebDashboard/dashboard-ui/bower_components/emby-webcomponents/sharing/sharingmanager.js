define(['connectionManager', 'sharingMenu', 'loading'], function (connectionManager, sharingMenu, loading) {

    function onSharingSuccess(options) {

        console.log('share success. shareId: ' + options.share.Id);
    }

    function onSharingCancel(options, apiClient) {

        var shareId = options.share.Id;

        console.log('share cancelled. shareId: ' + shareId);

        // Delete the share since it was cancelled
        apiClient.ajax({

            type: 'DELETE',
            url: apiClient.getUrl('Social/Shares/' + shareId)

        });
    }

    function showMenu(options) {

        loading.show();
        var itemId = options.itemId;
        var apiClient = options.apiClient || connectionManager.getApiClient(options.serverId);
        var userId = apiClient.getCurrentUserId();

        return apiClient.getItem(userId, itemId).then(function () {

            return apiClient.ajax({
                type: 'POST',
                url: apiClient.getUrl('Social/Shares', {

                    ItemId: itemId,
                    UserId: userId
                }),
                dataType: "json"

            }).then(function (share) {

                var options = {
                    share: share
                };

                loading.hide();
                sharingMenu.showMenu(options, onSharingSuccess, function (options) {
                    onSharingCancel(options, apiClient);
                });

            }, function () {

                loading.hide();
            });
        });
    }

    return {
        showMenu: showMenu
    };
});
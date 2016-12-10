define(['connectionManager', 'confirm', 'embyRouter', 'globalize'], function (connectionManager, confirm, embyRouter, globalize) {
    'use strict';

    function deleteItem(options) {

        var item = options.item;
        var itemId = item.Id;
        var parentId = item.SeasonId || item.SeriesId || item.ParentId;
        var serverId = item.ServerId;

        var msg = globalize.translate('sharedcomponents#ConfirmDeleteItem');
        var title = globalize.translate('sharedcomponents#HeaderDeleteItem');
        var apiClient = connectionManager.getApiClient(item.ServerId);

        return confirm({

            title: title,
            text: msg,
            confirmText: globalize.translate('sharedcomponents#Delete'),
            primary: 'cancel'

        }).then(function () {

            return apiClient.deleteItem(itemId).then(function () {

                if (options.navigate) {
                    if (parentId) {
                        embyRouter.showItem(parentId, serverId);
                    } else {
                        embyRouter.goHome();
                    }
                }
            });
        });
    }

    return {
        deleteItem: deleteItem
    };
});
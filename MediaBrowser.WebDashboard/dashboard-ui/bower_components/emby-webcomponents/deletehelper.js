define(['connectionManager', 'confirm', 'appRouter', 'globalize'], function (connectionManager, confirm, appRouter, globalize) {
    'use strict';

    function alertText(options) {

        return new Promise(function (resolve, reject) {

            require(['alert'], function (alert) {
                alert(options).then(resolve, resolve);
            });
        });
    }

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
                        appRouter.showItem(parentId, serverId);
                    } else {
                        appRouter.goHome();
                    }
                }
            }, function (err) {

                var result = function () {
                    return Promise.reject(err);
                };

                return alertText(globalize.translate('sharedcomponents#ErrorDeletingItem')).then(result, result);
            });
        });
    }

    return {
        deleteItem: deleteItem
    };
});
define(['components/favoriteitems'], function (favoriteItems) {

    return function (view, params, tabContent) {

        var self = this;

        self.renderTab = function () {

            var parentId = null;
            favoriteItems.render(tabContent, Dashboard.getCurrentUserId(), parentId);
        };
    };

});
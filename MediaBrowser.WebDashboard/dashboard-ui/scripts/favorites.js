define(['components/favoriteitems'], function (favoriteItems) {

    return function (view, params) {

        var self = this;

        view.addEventListener('viewshow', function (e) {
            
            var isRestored = e.detail.isRestored;

            if (!isRestored) {

                var parentId = null;
                favoriteItems.render(view, Dashboard.getCurrentUserId(), parentId);
            }
        });
    };

});
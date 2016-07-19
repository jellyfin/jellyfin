define(['itemHelper'], function (itemHelper) {

    function initSyncButtons(view) {

        var apiClient = window.ApiClient;

        if (!apiClient || !apiClient.getCurrentUserId()) {
            return;
        }

        apiClient.getCurrentUser().then(function (user) {

            var item = {
                SupportsSync: true
            };

            var categorySyncButtons = view.querySelectorAll('.categorySyncButton');
            for (var i = 0, length = categorySyncButtons.length; i < length; i++) {
                categorySyncButtons[i].addEventListener('click', onCategorySyncButtonClick);
                if (itemHelper.canSync(user, item)) {
                    categorySyncButtons[i].classList.remove('hide');
                } else {
                    categorySyncButtons[i].classList.add('hide');
                }
            }
        });
    }

    function onCategorySyncButtonClick(e) {

        var button = this;
        var category = button.getAttribute('data-category');
        var parentId = LibraryMenu.getTopParentId();

        require(['syncDialog'], function (syncDialog) {
            syncDialog.showMenu({
                ParentId: parentId,
                Category: category
            });
        });
    }

    return {
        init: function (view) {
            initSyncButtons(view);
        }
    };
});
define(['libraryBrowser', 'itemHelper'], function (libraryBrowser, itemHelper) {

    function isClickable(target) {

        while (target != null) {
            var tagName = target.tagName || '';
            if (tagName == 'A' || tagName.indexOf('BUTTON') != -1 || tagName.indexOf('INPUT') != -1) {
                return true;
            }

            return false;
            //target = target.parentNode;
        }

        return false;
    }

    function onGroupedCardClick(e, card) {

        var itemId = card.getAttribute('data-id');
        var context = card.getAttribute('data-context');

        var userId = Dashboard.getCurrentUserId();

        var playedIndicator = card.querySelector('.playedIndicator');
        var playedIndicatorHtml = playedIndicator ? playedIndicator.innerHTML : null;
        var options = {

            Limit: parseInt(playedIndicatorHtml || '10'),
            Fields: "PrimaryImageAspectRatio,DateCreated",
            ParentId: itemId,
            GroupItems: false
        };

        var target = e.target;
        if (isClickable(target)) {
            return;
        }

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).then(function (items) {

            if (items.length == 1) {
                Dashboard.navigate(libraryBrowser.getHref(items[0], context));
                return;
            }

            var url = 'itemdetails.html?id=' + itemId;
            if (context) {
                url += '&context=' + context;
            }

            Dashboard.navigate(url);
        });

        e.stopPropagation();
        e.preventDefault();
        return false;
    }

    function showSyncButtonsPerUser(page) {

        var apiClient = window.ApiClient;

        if (!apiClient || !apiClient.getCurrentUserId()) {
            return;
        }

        Dashboard.getCurrentUser().then(function (user) {

            var item = {
                SupportsSync: true
            };

            var categorySyncButtons = page.querySelectorAll('.categorySyncButton');
            for (var i = 0, length = categorySyncButtons.length; i < length; i++) {
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

    pageClassOn('pageinit', "libraryPage", function () {

        var page = this;

        var categorySyncButtons = page.querySelectorAll('.categorySyncButton');
        for (var i = 0, length = categorySyncButtons.length; i < length; i++) {
            categorySyncButtons[i].addEventListener('click', onCategorySyncButtonClick);
        }
    });

    pageClassOn('pageshow', "libraryPage", function () {

        var page = this;

        if (!Dashboard.isServerlessPage()) {
            showSyncButtonsPerUser(page);
        }
    });
});
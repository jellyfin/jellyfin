define(['jQuery', 'libraryBrowser'], function ($, libraryBrowser) {

    var currentItem;

    function reload(page) {

        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getJSON(ApiClient.getUrl('Social/Shares/Public/' + id + '/Item')).then(function (item) {

            reloadFromItem(page, item);
        });
    }

    function reloadFromItem(page, item) {

        currentItem = item;

        libraryBrowser.renderName(item, $('.itemName', page)[0], false);
        libraryBrowser.renderParentName(item, $('.parentName', page)[0]);
        libraryBrowser.renderDetailPageBackdrop(page, item);

        renderImage(page, item);

        setInitialCollapsibleState(page, item);
        ItemDetailPage.renderDetails(page, item, null, true);

        Dashboard.hideLoadingMsg();
    }

    function setInitialCollapsibleState(page, item) {

        $('.collectionItems', page).empty();

        if (item.MediaSources && item.MediaSources.length) {
            ItemDetailPage.renderMediaSources(page, item);
        }

        var chapters = item.Chapters || [];

        if (!chapters.length) {
            $('#scenesCollapsible', page).hide();
        } else {
            $('#scenesCollapsible', page).show();
            ItemDetailPage.renderScenes(page, item, null, 3, true);
        }

        if (!item.People || !item.People.length) {
            $('#castCollapsible', page).hide();
        } else {
            $('#castCollapsible', page).show();
            ItemDetailPage.renderCast(page, item, null, 6, true);
        }

        ItemDetailPage.renderCriticReviews(page, item, 1);
    }

    function renderImage(page, item) {
        libraryBrowser.renderDetailImage(page.querySelector('.detailImageContainer'), item, false);
    }

    $(document).on('pageinit', "#publicSharedItemPage", function () {

        var page = this;

        $(page).on("click", ".moreScenes", function () {

            ItemDetailPage.renderScenes(page, currentItem, null, null, true);

        }).on("click", ".morePeople", function () {

            ItemDetailPage.renderCast(page, currentItem, null, null, true);

        }).on("click", ".moreCriticReviews", function () {

            ItemDetailPage.renderCriticReviews(page, currentItem);

        });

    }).on('pageshow', "#publicSharedItemPage", function () {

        var page = this;

        reload(page);

    });

});
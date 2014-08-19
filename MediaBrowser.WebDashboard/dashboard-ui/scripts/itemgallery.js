(function ($, document) {

    var currentItem;

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), getParameterByName('id')).done(function (item) {

            currentItem = item;

            LibraryBrowser.renderName(item, $('.itemName', page), true);
            LibraryBrowser.renderParentName(item, $('.parentName', page));

            $('#galleryContent', page).html(LibraryBrowser.getGalleryHtml(currentItem)).trigger('create');

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageshow', "#itemGalleryPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#itemGalleryPage", function () {

        var page = this;

        currentItem = null;
    });

})(jQuery, document);
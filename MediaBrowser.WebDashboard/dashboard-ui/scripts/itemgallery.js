(function ($, document) {

    var currentItem;

    function getPromise() {

        var name = getParameterByName('person');

        if (name) {
            return ApiClient.getPerson(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('studio');

        if (name) {

            return ApiClient.getStudio(name, Dashboard.getCurrentUserId());

        }

        name = getParameterByName('genre');

        if (name) {
            return ApiClient.getGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('musicartist');

        if (name) {
            return ApiClient.getArtist(name, Dashboard.getCurrentUserId());
        }

        return ApiClient.getItem(Dashboard.getCurrentUserId(), getParameterByName('id'));
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        getPromise().done(function (item) {

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
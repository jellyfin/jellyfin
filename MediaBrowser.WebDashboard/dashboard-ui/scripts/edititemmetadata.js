(function ($, document, window) {

    var currentItemId;

    function reload(page) {

        page = $(page)[0];

        Dashboard.showLoadingMsg();

        var itemId = MetadataEditor.getCurrentItemId();
        currentItemId = itemId;

        if (itemId) {
            require(['components/metadataeditor/metadataeditor'], function (metadataeditor) {

                metadataeditor.embed(page.querySelector('.editPageInnerContent'), itemId);
            });
        } else {
            page.querySelector('.editPageInnerContent').innerHTML = '';
            Dashboard.hideLoadingMsg();
        }
    }

    $(document).on('pageinit', "#editItemMetadataPage", function () {

        var page = this;

        $('.libraryTree', page).on('itemclicked', function (event, data) {

            if (data.id != currentItemId) {

                //$.mobile.urlHistory.ignoreNextHashChange = true;
                window.location.hash = 'editItemMetadataPage?id=' + data.id;
                reload(page);
            }
        });

    }).on('pageshow', "#editItemMetadataPage", function () {

        var page = this;

        reload(page);

    }).on('pagebeforehide', "#editItemMetadataPage", function () {

        var page = this;
    });

})(jQuery, document, window);


define(['historyManager', 'jQuery'], function (historyManager, $) {

    var currentItemId;

    function reload(page) {

        page = $(page)[0];

        Dashboard.showLoadingMsg();

        var itemId = MetadataEditor.getCurrentItemId();
        currentItemId = itemId;

        if (itemId) {
            require(['metadataEditor'], function (metadataEditor) {

                metadataEditor.embed(page.querySelector('.editPageInnerContent'), itemId, ApiClient.serverInfo().Id);
            });
        } else {
            page.querySelector('.editPageInnerContent').innerHTML = '';
            Dashboard.hideLoadingMsg();
        }
    }

    $(document).on('pageinit', "#editItemMetadataPage", function () {

        var page = this;

        MetadataEditor.setCurrentItemId(null);

        $('.libraryTree', page).on('itemclicked', function (event, data) {

            if (data.id != currentItemId) {

                MetadataEditor.setCurrentItemId(data.id);
                reload(page);
            }
        });

    }).on('pageshow', "#editItemMetadataPage", function () {

        var page = this;

        reload(page);

    }).on('pagebeforehide', "#editItemMetadataPage", function () {

        var page = this;
    });

});

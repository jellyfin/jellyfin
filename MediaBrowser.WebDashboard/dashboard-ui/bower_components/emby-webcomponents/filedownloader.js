define(['multi-download'], function (multiDownload) {

    return {
        download: function (items) {

            multiDownload(items.map(function (item) {
                return item.url;
            }));
        }
    };
});
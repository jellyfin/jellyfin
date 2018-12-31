define(['multi-download'], function (multiDownload) {
    'use strict';

    return {
        download: function (items) {

            multiDownload(items.map(function (item) {
                return item.url;
            }));
        }
    };
});
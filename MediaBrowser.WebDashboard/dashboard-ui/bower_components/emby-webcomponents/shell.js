define([], function () {
    'use strict';

    return {
        openUrl: function (url) {
            window.open(url, '_blank');
        },
        canExec: false,
        exec: function (options) {
            // options.path
            // options.arguments
            return Promise.reject();
        }
    };
});
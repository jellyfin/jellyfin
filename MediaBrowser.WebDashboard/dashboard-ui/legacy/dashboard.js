Dashboard.confirm = function (message, title, callback) {
    'use strict';

    require(['confirm'], function (confirm) {

        confirm(message, title).then(function () {
            callback(true);
        }, function () {
            callback(false);
        });
    });
};
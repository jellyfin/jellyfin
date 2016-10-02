define([], function () {
    'use strict';

    function replaceAll(str, find, replace) {

        return str.split(find).join(replace);
    }

    return function (options) {

        if (typeof options === 'string') {
            options = {
                text: options
            };
        }

        var text = replaceAll(options.text || '', '<br/>', '\n');

        alert(text);

        return Promise.resolve();
    };
});
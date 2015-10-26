define([], function () {
    return function (options) {

        var result = prompt(options.text, options.defaultText || '');

        if (options.callback) {
            options.callback(result);
        }
    };
});
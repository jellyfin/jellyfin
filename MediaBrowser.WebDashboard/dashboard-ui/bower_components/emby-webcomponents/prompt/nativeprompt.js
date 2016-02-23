define([], function () {

    function replaceAll(str, find, replace) {

        return str.split(find).join(replace);
    }

    return function (options) {

        if (typeof options === 'string') {
            options = {
                label: '',
                text: options
            };
        }

        var label = replaceAll(options.label || '', '<br/>', '\n');

        var result = prompt(label, options.text || '');

        if (result) {
            return Promise.resolve(result);
        } else {
            return Promise.reject(result);
        }
    };
});
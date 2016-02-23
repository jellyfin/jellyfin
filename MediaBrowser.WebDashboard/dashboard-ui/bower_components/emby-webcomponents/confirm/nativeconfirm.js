define([], function () {

    function replaceAll(str, find, replace) {

        return str.split(find).join(replace);
    }

    return function (options) {

        if (typeof options === 'string') {
            options = {
                title: '',
                text: options
            };
        }

        var text = replaceAll(options.text || '', '<br/>', '\n');
        var result = confirm(text);

        if (result) {
            return Promise.resolve();
        } else {
            return Promise.reject();
        }
    };
});
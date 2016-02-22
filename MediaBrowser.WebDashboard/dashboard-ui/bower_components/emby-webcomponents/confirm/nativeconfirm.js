define([], function () {

    return function (options) {

        if (typeof options === 'string') {
            options = {
                title: '',
                text: options
            };
        }

        var result = confirm(options.text);

        if (result) {
            return Promise.resolve();
        } else {
            return Promise.reject();
        }
    };
});
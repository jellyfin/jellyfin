define([], function () {

    return function (options) {

        return new Promise(function (resolve, reject) {

            if (typeof options === 'string') {
                options = {
                    label: '',
                    text: options
                };
            }

            var result = prompt(options.label || '', options.text || '');

            if (result) {
                resolve(result);
            } else {
                reject(result);
            }
        });

    };
});
define([], function () {

    return function (options) {

        return new Promise(function (resolve, reject) {

            if (typeof options === 'string') {
                options = {
                    title: '',
                    text: options
                };
            }

            var result = prompt(options.title || '', options.text || '');

            if (result) {
                resolve(result);
            } else {
                reject(result);
            }
        });

    };
});